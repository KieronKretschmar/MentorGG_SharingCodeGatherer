using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;

namespace SharingCodeGatherer.Controllers
{
    [Route("trusted/maintenance")]
    [ApiController]
    public class MaintenanceController : ControllerBase
    {
        private readonly ILogger<MaintenanceController> _logger;
        private readonly SharingCodeContext _context;
        private readonly IProducer<SharingCodeInstruction> _producer;

        public MaintenanceController(
            ILogger<MaintenanceController> logger,
            SharingCodeContext context,
            IProducer<SharingCodeInstruction> producer
            )
        {
            _logger = logger;
            _context = context;
            _producer = producer;
        }

        /// <summary>
        /// Publishes messages for the first up to <paramref name="count"/> matches that have an InternalMatchId <paramref name="internalMatchId"/> or higher.
        /// </summary>
        /// <param name="internalMatchId"></param>
        /// <param name="count"></param>
        /// <returns></returns>
        [HttpPost("resend/following-internal-matchid/{internalMatchId}")]
        public ActionResult ResendFromInternalMatchId(long internalMatchId, int count)
        {
            _logger.LogInformation($"ResendFromInternalMatchId called with internalMatchId [ {internalMatchId} ] and count [ {count} ].");

            // Load matches with last known uploader
            var matches = _context.Matches
                .Where(x => x.Id >= internalMatchId)
                .Take(count)
                .ToList();

            foreach (var match in matches)
            {
                // Try to use the last uploaders steamId or default to -1 of not available (as Uploads wasn't added until 2020-06-05)
                var lastUpload = _context.Uploads
                    .Include(x=>x.Uploader)
                    .Where(x => x.InternalMatchId == match.Id)
                    .OrderBy(y => y.UploadTime)
                    .LastOrDefault();

                var lastUploaderSteamId = lastUpload != null ? lastUpload.Uploader.SteamId : -1;

                var message = new SharingCodeInstruction
                {
                    SharingCode = match.SharingCode,
                    UploaderId = lastUploaderSteamId,
                    UploadType = RabbitCommunicationLib.Enums.UploadType.SharingCodeGatherer
                };

                _producer.PublishMessage(message);
            }

            var msg = $"Resent [ {matches.Count} ] matches with InternalMatchId between [ {matches.FirstOrDefault()?.Id} ] and [ {matches.LastOrDefault()?.Id} ].";
            _logger.LogInformation(msg);
            return Ok(msg);
        }
    }
}