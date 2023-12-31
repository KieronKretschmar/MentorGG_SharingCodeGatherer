﻿using Database;
using Entities.Models;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using RabbitCommunicationLib.Enums;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.Producer;
using RabbitCommunicationLib.TransferModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static SharingCodeGatherer.ValveApiCommunicator;

namespace SharingCodeGatherer
{
    public interface ISharingCodeWorker
    {
        Task<bool> WorkSharingCode(string currentSharingCode, long uploaderId, AnalyzerQuality requestedQuality);
        Task WorkUser(User user, AnalyzerQuality requestedQuality);
    }

    /// <summary>
    /// Class for orchestrating all work related to a user's request of gathering matches.
    /// </summary>
    public class SharingCodeWorker : ISharingCodeWorker
    {
        private ILogger<ISharingCodeWorker> _logger;
        private readonly SharingCodeContext _context;
        private readonly IValveApiCommunicator _apiCommunicator;
        private readonly IProducer<SharingCodeInstruction> _rabbitProducer;

        public SharingCodeWorker(ILogger<ISharingCodeWorker> logger, SharingCodeContext context, IValveApiCommunicator apiCommunicator, IProducer<SharingCodeInstruction> rabbitProducer)
        {
            _logger = logger;
            _context = context;
            _apiCommunicator = apiCommunicator;
            _rabbitProducer = rabbitProducer;
        }

        /// <summary>
        /// Determines whether the match belonging to the given sharingCode needs to be (re-)analyzed. In that case it is stored in the database and published to rabbit.
        /// </summary>
        /// <returns>bool, whether the sharingcode was published to be (re-)analyzed.</returns>
        public async Task<bool> WorkSharingCode(string currentSharingCode, long uploaderId, AnalyzerQuality requestedQuality)
        {
            _logger.LogInformation($"Starting to work current SharingCode [ {currentSharingCode} ] of uploader with SteamId [ {uploaderId} ], requestedQuality [ {requestedQuality} ].");
            var match = new MatchData
            {
                SharingCode = currentSharingCode,
                UploaderId = uploaderId,
                AnalyzedQuality = requestedQuality,
            };

            // Put match into database and rabbit queue if it's new
            var existingMatch = _context.Matches.FirstOrDefault(x => (x.SharingCode == match.SharingCode));
            if (existingMatch == null)
            {
                // Put match into rabbit queue
                _logger.LogInformation($"Publishing model with SharingCode [ {match.SharingCode} ] from uploader with SteamId [ {uploaderId} ] to queue.");
                _rabbitProducer.PublishMessage(match.ToTransferModel());

                // put match into database
                _logger.LogInformation($"Inserting Match including Upload with SharingCode [ {match.SharingCode} ] into database.");
                var dbMatch = match.ToDatabaseModel();
                var upload = new Upload(dbMatch, uploaderId, requestedQuality);
                dbMatch.Uploads = new List<Upload> { upload };
                _context.Matches.Add(dbMatch);

                await _context.SaveChangesAsync();
                return true;
            }
            // Trigger re-analysis if higher quality is requested
            else if(existingMatch != null && existingMatch.AnalyzedQuality < requestedQuality)
            {
                // Put match into rabbit queue
                _logger.LogInformation($"Publishing model with SharingCode [ {match.SharingCode} ] from uploader with SteamId [ {uploaderId} ] to queue for re-analysis. RequestedQuality: [ {requestedQuality} ].");
                _rabbitProducer.PublishMessage(match.ToTransferModel());

                _logger.LogInformation($"Inserting new Upload for match with SharingCode [ {match.SharingCode} ] into database.");
                var upload = new Upload(existingMatch, uploaderId, requestedQuality);
                existingMatch.Uploads.Add(upload);
                await _context.SaveChangesAsync();
            }
            else
            {
                _logger.LogInformation($"SharingCode [ {match.SharingCode} ] from uploader with SteamId [ {match.UploaderId} ] already exists and does not need to be re-published.");
            }
            return false;
        }

        /// <summary>
        /// Attempts to get the next sharingcode and, if found, updates user.LastKnownSharingCode (without writing to db) and runs WorkSharingCode on it. 
        /// </summary>
        /// <param name="user"></param>
        /// <returns>bool, whether a next sharingcode was found for this user</returns>
        public async Task<bool> WorkNextSharingCode(User user, AnalyzerQuality requestedQuality)
        {
            _logger.LogInformation($"Start WorkNextSharingCode for user with SteamId [ {user.SteamId} ]");
            bool foundNextSharingCode;
            try
            {
                // attempt to get next sharingcode
                user.LastKnownSharingCode = await _apiCommunicator.QueryNextSharingCode(user);
                foundNextSharingCode = true;
                await _context.SaveChangesAsync();

                // try to insert match into database if this code was never seen before
                var matchPublished = await WorkSharingCode(user.LastKnownSharingCode, user.SteamId, requestedQuality);
            }
            catch (NoMatchesFoundException e)
            {
                foundNextSharingCode = false;
            }
            _logger.LogInformation($"End WorkNextSharingCode for user with SteamId [ {user.SteamId} ]. Found next code: [ {foundNextSharingCode} ]");
            return foundNextSharingCode;
        }

        /// <summary>
        /// Runs WorkNextSharingCode as long as new sharingcodes are found.
        /// Also writes the newest user.LastKnownSharingCode to database.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task WorkUser(User user, AnalyzerQuality requestedQuality)
        {
            _logger.LogInformation($"Start WorkUser for user with SteamId [ {user.SteamId} ], requestedQuality [ {requestedQuality} ]");

            // Work next sharingcode until we've reached the newest one of this user
            while (await WorkNextSharingCode(user, requestedQuality));

            // call saveChanges to write newest value for user.LastKnownSharingCode to database
            await _context.SaveChangesAsync();

            _logger.LogInformation($"End WorkUser for user with SteamId [ {user.SteamId} ]");
        }
    }
}
