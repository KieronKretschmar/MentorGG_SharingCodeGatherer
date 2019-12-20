using Entities.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace SharingCodeGatherer
{
    public interface IValveApiCommunicator
    {
        Task<string> QueryNextSharingCode(User user);
        Task<bool> ValidateAuthData(User user);
    }

    /// <summary>
    /// Communicates with the steam API to gather the newest SharingCodes for users.
    /// For more info about the api, see: 
    /// https://developer.valvesoftware.com/wiki/Counter-Strike:_Global_Offensive_Access_Match_History
    /// 
    /// Throws: ExceededApiLimitException, BadUserAuthException, NoMatchesFoundException
    /// Requires environment variables: ["STEAM_API_KEY"]
    /// </summary>
    public class ValveApiCommunicator : IValveApiCommunicator
    {
        private HttpClient Client { get; set; }

        private ILogger<ValveApiCommunicator> _logger;

        private string ApiKey { get; set; }

        private static readonly string InvalidApiKeyErrorMessage = "Please verify your <pre>key=</pre> parameter.</body></html>";

        public ValveApiCommunicator(ILogger<ValveApiCommunicator> logger, IConfiguration configuration)
        {
            _logger = logger;
            ApiKey = configuration.GetValue<string>("STEAM_API_KEY");

            Client = new HttpClient();
        }

        public async Task<bool> ValidateAuthData(User user)
        {
            try
            {
                var nextCode = await QueryNextSharingCode(user);
            }
            catch (InvalidUserAuthException)
            {
                return false;
            }
            return true;
        }

        public async Task<string> QueryNextSharingCode(User user)
        {
            var queryString = "https://api.steampowered.com/ICSGOPlayers_730/GetNextMatchSharingCode/v1?";
            queryString += $"key={ApiKey}&steamidkey={user.SteamAuthToken}&steamid={user.SteamId}&knowncode={user.LastKnownSharingCode}";

            var result = await Client.GetAsync(queryString);
            //var result = Client.GetAsync(queryString).Result; // await doesn't work here, don't ask me why

            string content;
            switch (result.StatusCode)
            {
                case HttpStatusCode.OK:
                case HttpStatusCode.Accepted:
                    content = await result.Content.ReadAsStringAsync();
                    JObject jobject = JObject.Parse(content);
                    var nextCode = jobject["result"]["nextcode"].ToString();
                    if (nextCode == "n/a")
                    {
                        throw new NoMatchesFoundException($"No new matches found for user {user.SteamId}");
                    }
                    return nextCode;
                case HttpStatusCode.Forbidden:
                    content = await result.Content.ReadAsStringAsync();
                    if (content.Contains(InvalidApiKeyErrorMessage))
                    {
                        _logger.LogError("Invalid Valve API Key. Response from Valve API: " + content);
                        throw new InvalidApiKeyException("Invalid Valve API Key.");
                    }
                    else
                    {
                        throw new InvalidUserAuthException(
                            $"Bad auth data for user. " +
                            $"SteamId: {user.SteamId}. " +
                            $"SteamAuthToken: {user.SteamAuthToken}. " +
                            $"LastKnownSharingCode: {user.LastKnownSharingCode}");
                    }
                case HttpStatusCode.TooManyRequests:
                case HttpStatusCode.ServiceUnavailable:
                    throw new ExceededApiLimitException("Too many calls to SteamAPI. Consider heating up a new instance with a different API key.");
                default:
                    //_logger.LogError($"Unexpected response by Steam Api. Statuscode: {result.StatusCode}", e);
                    throw new HttpRequestException($"Unexpected response by Steam Api. Statuscode: {result.StatusCode}");
            }
        }

        public static bool IsNoMoreMatchesFound(string sharingCode)
        {
            return sharingCode == "n/a";
        }

        public class ExceededApiLimitException : Exception
        {
            public ExceededApiLimitException(string message) : base(message)
            {
            }

            public ExceededApiLimitException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }

        public class NoMatchesFoundException : Exception
        {
            public NoMatchesFoundException(string message) : base(message)
            {
            }

            public NoMatchesFoundException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }

        public class InvalidUserAuthException : Exception
        {
            public InvalidUserAuthException(string message) : base(message)
            {
            }

            public InvalidUserAuthException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }


        public class InvalidApiKeyException : Exception
        {
            public InvalidApiKeyException(string message) : base(message)
            {
            }

            public InvalidApiKeyException(string message, Exception innerException) : base(message, innerException)
            {
            }
        }
    }
}
