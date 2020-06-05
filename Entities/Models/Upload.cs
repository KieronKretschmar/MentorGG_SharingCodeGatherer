using RabbitCommunicationLib.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.Models
{
    /// <summary>
    /// One Upload entry for each published rabbit message.
    /// </summary>
    public class Upload
    {
        public int UploadId { get; set; }
        public int InternalMatchId { get; set; }
        public long SteamId { get; set; }
        public DateTime UploadTime { get; set; }
        public AnalyzerQuality Quality { get; set; }

        public virtual Match Match { get; set; }
        public virtual User Uploader { get; set; }
    }
}
