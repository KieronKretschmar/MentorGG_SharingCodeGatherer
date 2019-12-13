using Database;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace SharingCodeGathererTests
{
    public static class TestHelper
    {
        public static User GetRandomUser()
        {
            return new User
            {
                SteamId = (long)new Random().Next(1, 99999999),
                LastKnownSharingCode = "sharingcode",
                SteamAuthToken = "authToken"
            };
        }

        public static User GetValidUser()
        {
            return new User
            {
                SteamId = 76561198033880857,
                LastKnownSharingCode = "CSGO-NSsey-SWHK5-UVsDj-TtqvL-hJQBD",
                SteamAuthToken = "9K86-JTWHE-MWLE"
            };
            
        }

        public static DbContextOptions<SharingCodeContext> GetDatabaseOptions(string databaseName)
        {
            return new DbContextOptionsBuilder<SharingCodeContext>()
                .UseInMemoryDatabase(databaseName: databaseName)
                .Options;
        }
    }
}
