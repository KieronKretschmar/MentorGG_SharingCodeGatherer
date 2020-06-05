using RabbitCommunicationLib.Enums;
using System;
using System.Collections.Generic;

namespace Entities.Models
{
    public class Match
    {
        /// <summary>
        /// Internal Id for this service. Not the MatchId used throughout MentorEngine.
        /// </summary>
        public int Id { get; set; }
        public string SharingCode { get; set; }

        public AnalyzerQuality AnalyzedQuality { get; set; }

        public virtual ICollection<Upload> Uploads { get; set; }

    }
}
