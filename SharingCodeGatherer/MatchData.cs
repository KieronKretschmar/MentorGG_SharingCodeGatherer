using Entities.Models;
using RabbitTransfer.Enums;
using RabbitTransfer.TransferModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SharingCodeGatherer
{
    /// <summary>
    /// 
    /// </summary>
    public class MatchData
    {
        public string SharingCode { get; set; }
        public long UploaderId { get; set; }

        public SCG_SWS_Model ToTransferModel()
        {
            return new SCG_SWS_Model
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
