using Database;
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
        Task<bool> WorkUser(User user, AnalyzerQuality requestedQuality, bool skipLastKnownMatch);
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
        /// Gathers all matches of this user and inserts them into database and rabbit queue, and also updates the user's LastKnownSharingCode in database.
        /// </summary>
        /// <param name="steamId"></param>
        /// <returns name="matchFound">bool, whether at least one new match was found</returns>
        public async Task<bool> WorkUser(User user, AnalyzerQuality requestedQuality, bool skipLastKnownMatch)
        {
            _logger.LogInformation($"Working user with SteamId [ {user.SteamId} ], requestedQuality [ {requestedQuality} ] and skipLastKnownMatch [ {skipLastKnownMatch} ]");

            // Perform work on this or the next match
            bool matchFound;

            if (skipLastKnownMatch)
            {
                try
                {
                    matchFound = await WorkNextSharingCode(user, requestedQuality);
                }
                catch (ValveApiCommunicator.InvalidUserAuthException)
                {
                    // Set user's authentication as invalid
                    user.Invalidated = true;
                    _context.Users.Update(user);
                    await _context.SaveChangesAsync();
                    throw;
                }
            }
            else
            {
                matchFound = await WorkSharingCode(user.LastKnownSharingCode, user.SteamId, requestedQuality);
            }


            // Update user
            _context.SaveChangesAsync();

            // Work on all other matches of this user without awaiting the result
            WorkAllNewSharingCodesAndUpdateUser(user, requestedQuality);

            _logger.LogInformation($"Finished working user with SteamId [ {user.SteamId} ]");

            return matchFound;
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
            if (!_context.Matches.Any(x => (x.SharingCode == match.SharingCode) && x.AnalyzedQuality >= requestedQuality))
            {
                _logger.LogInformation($"Publishing model with SharingCode [ {match.SharingCode} ] from uploader with SteamId [ {uploaderId} ] to queue.");

                // Put match into rabbit queue with random correlationId
                _rabbitProducer.PublishMessage(match.ToTransferModel());

                // put match into database
                var dbMatch = match.ToDatabaseModel();
                _logger.LogInformation($"Inserting match with SharingCode [ {match.SharingCode} ] into database.");
                await _context.Matches.AddAsync(dbMatch);
                await _context.SaveChangesAsync();
                return true;
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
            bool foundNextSharingCode;
            try
            {
                // attempt to get next sharingcode
                user.LastKnownSharingCode = await _apiCommunicator.QueryNextSharingCode(user);
                foundNextSharingCode = true;

                // try to insert match into database if this code was never seen before
                var matchPublished = await WorkSharingCode(user.LastKnownSharingCode, user.SteamId, requestedQuality);
            }
            catch (NoMatchesFoundException e)
            {
                foundNextSharingCode = false;
            }
            return foundNextSharingCode;
        }

        /// <summary>
        /// Runs WorkNextSharingCode as long as new sharingcodes are found.
        /// Also writes the newest user.LastKnownSharingCode to database.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task WorkAllNewSharingCodesAndUpdateUser(User user, AnalyzerQuality requestedQuality)
        {
            // Work next sharingcode until we've reached the newest one of this user
            while (await WorkNextSharingCode(user, requestedQuality));

            // call saveChanges to write newest value for user.LastKnownSharingCode to database
            await _context.SaveChangesAsync();
        }
    }
}
