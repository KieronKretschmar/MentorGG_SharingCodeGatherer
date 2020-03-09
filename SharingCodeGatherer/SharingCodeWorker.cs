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
                matchFound = await WorkCurrentSharingCode(user, requestedQuality);
            }


            // Update user
            _context.SaveChangesAsync();

            // Work on all other matches of this user without awaiting the result
            WorkSharingCodesRecursivelyAndUpdateUser(user, requestedQuality);

            return matchFound;
        }

        /// <summary>
        /// Does work on the match of the currently set LastKnownSharingCode.
        /// This implies: Getting the next sharingCode, updating the user object without saving changes to database, and, if a match is found, putting it into rabbit queue and database.
        /// </summary>
        /// <param name="user"></param>
        /// <returns>bool, whether a match was found</returns>
        public async Task<bool> WorkCurrentSharingCode(User user, AnalyzerQuality requestedQuality)
        {
            var match = new MatchData
            {
                SharingCode = user.LastKnownSharingCode,
                UploaderId = user.SteamId,
                AnalyzedQuality = requestedQuality,
            };

            // Put match into database and rabbit queue if it's new
            if (!_context.Matches.Any(x => (x.SharingCode == match.SharingCode) && x.AnalyzedQuality >= requestedQuality))
            {
                _logger.LogInformation($"Publishing model with SharingCode [ {match.SharingCode} ] from uploader#{match.UploaderId} to queue.");

                // Put match into rabbit queue with random correlationId
                _rabbitProducer.PublishMessage(match.ToTransferModel());

                // put match into database
                await _context.Matches.AddAsync(match.ToDatabaseModel());
                return true;
            }
            return false;
        }

        /// <summary>
        /// Attempts to get the next sharingCode and perform work on it.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task<bool> WorkNextSharingCode(User user, AnalyzerQuality requestedQuality)
        {
            bool matchFound;

            // Query next SC, throwing exception if none is found
            user.LastKnownSharingCode = await _apiCommunicator.QueryNextSharingCode(user);
            matchFound = await WorkCurrentSharingCode(user, requestedQuality);

            return matchFound;
        }

        /// <summary>
        /// Does work on all matches that happened after the match of the currently set LastKnownSharingCode recursively.
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        public async Task WorkSharingCodesRecursivelyAndUpdateUser(User user, AnalyzerQuality requestedQuality)
        {
            while (await WorkNextSharingCode(user, requestedQuality))
                ;

            await _context.SaveChangesAsync();
        }
    }
}
