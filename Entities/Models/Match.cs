using RabbitCommunicationLib.Enums;
using System;

namespace Entities.Models
{
    public class Match
    {
        public int Id { get; set; }
        public string SharingCode { get; set; }

        public AnalyzerQuality AnalyzedQuality { get; set; }
    }
}
