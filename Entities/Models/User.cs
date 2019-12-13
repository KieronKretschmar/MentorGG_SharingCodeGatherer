using System;
using System.Collections.Generic;
using System.Text;

namespace Entities.Models
{
    public class User
    {
        public long SteamId { get; set; }
        public string SteamAuthToken { get; set; }
        public string LastKnownSharingCode { get; set; }
        public bool Invalidated { get; set; } = false;
    }
}
