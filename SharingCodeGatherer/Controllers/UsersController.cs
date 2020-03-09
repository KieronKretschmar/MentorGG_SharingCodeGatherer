using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Database;
using Entities.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RabbitCommunicationLib.Enums;

namespace SharingCodeGatherer.Controllers
{
    [Route("users")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly SharingCodeContext _context;
        private readonly ISharingCodeWorker _scWorker;
        private readonly IValveApiCommunicator _valveApiCommunicator;

        public UsersController(SharingCodeContext context, ISharingCodeWorker scWorker, IValveApiCommunicator valveApiCommunicator)
        {
            _context = context;
            _scWorker = scWorker;
            _valveApiCommunicator = valveApiCommunicator;
        }


        /// <summary>
        /// Gets the database entry of the user with the given steamId.
        /// </summary>
        /// <param name="steamId"></param>
        /// <returns></returns>
        [HttpGet("{steamId}")]
        public async Task<ActionResult<User>> GetUser(long steamId)
        {
            var user = await _context.Users.FindAsync(steamId);

            if (user == null)
            {
                return NotFound();
            }

            return user;
        }

        /// <summary>
        /// Adds user to database and thereby enables automatic-upload for him.
        /// </summary>
        /// <param name="steamId"></param>
        /// <param name="steamAuthToken"></param>
        /// <param name="lastKnownSharingCode"></param>
        /// <returns></returns>
        [HttpPost("{steamId}")]
        public async Task<ActionResult> CreateUser(long steamId, string steamAuthToken, string lastKnownSharingCode)
        {
            // Create or update user, assuming data is valid and without saving changes before
            var user = await _context.Users.FindAsync(steamId);
            if (user == null)
            {
                // Create user
                user = new User
                {
                    SteamId = steamId,
                    SteamAuthToken = steamAuthToken,
                    LastKnownSharingCode = lastKnownSharingCode,
                };
                await _context.Users.AddAsync(user);
            }
            else
            {
                // Update user
                user.SteamAuthToken = steamAuthToken;
                user.LastKnownSharingCode = lastKnownSharingCode;
                user.Invalidated = false;
                _context.Users.Update(user);
            }


            // Validate data
            if (await _valveApiCommunicator.ValidateAuthData(user) == false)
            {
                return BadRequest();
            }

            // Update UserDb
            await _context.SaveChangesAsync();
            return Ok();
        }

        /// <summary>
        /// Removes User from database and thereby disables automatic upload.
        /// </summary>
        /// <param name="steamId"></param>
        /// <returns></returns>
        [HttpDelete("{steamId}")]
        public async Task<ActionResult> DeleteUser(long steamId)
        {
            var user = await _context.Users.FindAsync(steamId);
            if (user == null)
            {
                return NotFound();
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();

            return Ok();
        }

        /// <summary>
        /// Triggers calls to the Steam API to find new matches of the specified user, and initiates the process of analyzing them.
        /// </summary>
        /// <param name="steamId"></param>
        /// <returns></returns>
        [HttpPost("{steamId}/look-for-matches")]
        public async Task<ActionResult<bool>> LookForMatches(long steamId, AnalyzerQuality requestedQuality)
        {
            // Get user
            var user = _context.Users.Single(x => x.SteamId == steamId);
            try
            {
                var foundMatch = await _scWorker.WorkUser(user, requestedQuality, true);
                return foundMatch;
            }
            catch (ValveApiCommunicator.InvalidUserAuthException e)
            {
                return Unauthorized();
            }
        }
    }
}