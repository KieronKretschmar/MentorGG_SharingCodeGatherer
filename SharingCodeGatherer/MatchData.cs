using Entities.Models;
using RabbitCommunicationLib.Enums;
using RabbitCommunicationLib.TransferModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SharingCodeGatherer
{
    /// <summary>
    /// Holds data regarding a match.
    /// </summary>
    public class MatchData
    {
        public string SharingCode { get; set; }
        public long UploaderId { get; set; }

        public SteamInfoInstructions ToTransferModel()
        {
            return new SteamInfoInstructions
            {
                UploaderId = UploaderId,
                SharingCode = SharingCode,
                // UploadType is always UploadType.SharingCodeGatherer, as this is the SharingCodeGatherer project
                UploadType = UploadType.SharingCodeGatherer, 
            };
        }
        public Match ToDatabaseModel()
        {
            return new Match { SharingCode = SharingCode };
        }
    }
}
