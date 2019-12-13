using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using System.Linq;
using System;
using Moq;
using System.ComponentModel;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Database;
using SharingCodeGatherer.Controllers;
using SharingCodeGatherer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace SharingCodeGathererTests
{
    [TestClass]
    public class UsersControllerTests
    {
        private readonly ServiceProvider serviceProvider;

        public UsersControllerTests()
        {
            var services = new ServiceCollection();
            services.AddLogging(x => x.AddConsole().AddDebug());

            serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Tests POST: api/Users/<steamId> with a valid User and verifies that the user was stored in database correctly and WorkUser() was called on him.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task CreateUserTest()
        {
            var options = TestHelper.GetDatabaseOptions("CreateUserTest");
            var user = TestHelper.GetRandomUser();

            // mock api communicator to return true on validation
            var mockApiComm = new Mock<IValveApiCommunicator>();
            mockApiComm
                .Setup(x => x.ValidateAuthData(It.Is<User>(x => x.SteamId == user.SteamId)))
                .Returns(Task.FromResult<bool>(true));

            var mockScWorker = new Mock<ISharingCodeWorker>();

            // Call PostUser to create valid user
            using (var context = new SharingCodeContext(options))
            {
                var usersController = new UsersController(context, mockScWorker.Object, mockApiComm.Object);

                var result = await usersController.PostUser(user.SteamId, user.SteamAuthToken, user.LastKnownSharingCode);
                Assert.IsInstanceOfType(result, typeof(Microsoft.AspNetCore.Mvc.OkResult));
            }

            // Verify that the user was stored in database correctly and WorkUser was called on him
            using (var context = new SharingCodeContext(options))
            {
                var userFromDb = context.Users.Single();
                Assert.IsTrue(userFromDb.SteamId == user.SteamId);
                Assert.IsTrue(userFromDb.LastKnownSharingCode == user.LastKnownSharingCode);
                Assert.IsTrue(userFromDb.SteamAuthToken == user.SteamAuthToken);
                Assert.IsTrue(userFromDb.Invalidated == false);

                mockScWorker.Verify(x => x.WorkUser(It.Is<User>(x=>x.SteamId == user.SteamId), false), Times.Once());
            }
        }

        /// <summary>
        /// Tests POST: api/Users/<steamId> with a invalid User and verifies that the user was not stored in database and WorkUser() was never called.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task CreateInvalidUserTest()
        {
            var options = TestHelper.GetDatabaseOptions("CreateInvalidUserTest");
            var user = TestHelper.GetRandomUser();

            // mock api communicator to return false on validation
            var mockApiComm = new Mock<IValveApiCommunicator>();
            mockApiComm
                .Setup(x => x.ValidateAuthData(It.Is<User>(x => x.SteamId == user.SteamId)))
                .Returns(Task.FromResult<bool>(false));

            var mockScWorker = new Mock<ISharingCodeWorker>();

            // Call PostUser in attempt to create invalid user
            using (var context = new SharingCodeContext(options))
            {
                var usersController = new UsersController(context, mockScWorker.Object, mockApiComm.Object);

                var result = await usersController.PostUser(user.SteamId, user.SteamAuthToken, user.LastKnownSharingCode);
                Assert.IsInstanceOfType(result, typeof(Microsoft.AspNetCore.Mvc.BadRequestResult));
            }

            // Verify that the user was not stored in database correctly and WorkUser was not called on him
            using (var context = new SharingCodeContext(options))
            {
                var userIsInDb = context.Users.Any();
                Assert.IsFalse(userIsInDb);
                mockScWorker.Verify(x => x.WorkUser(It.IsAny<User>(), It.IsAny<bool>()), Times.Never());
            }
        }

        /// <summary>
        /// Tests POST: api/Users/<steamId> to update an invalidated user and verifies that the 
        /// updated user, and only the updated user was stored in database correctly and WorkUser was called on him.
        /// <returns></returns>
        [TestMethod]
        public async Task UpdateInvalidUserTest()
        {
            var options = TestHelper.GetDatabaseOptions("UpdateInvalidUserTest");

            // Create invalid user
            var invalidUser = TestHelper.GetRandomUser();
            invalidUser.Invalidated = true;
            using (var context = new SharingCodeContext(options))
            {
                context.Users.Add(invalidUser);
                await context.SaveChangesAsync();
            }

            // mock api communicator to return true on validation
            var mockApiComm = new Mock<IValveApiCommunicator>();
            mockApiComm
                .Setup(x => x.ValidateAuthData(It.Is<User>(x => x.SteamId == invalidUser.SteamId)))
                .Returns(Task.FromResult<bool>(true));

            var mockScWorker = new Mock<ISharingCodeWorker>();

            // Call PostUser to update user with new authToken and SharingCode
            var updatedUser = new User
            {
                SteamId = invalidUser.SteamId,
                LastKnownSharingCode = "updatedsharingcode",
                SteamAuthToken = "updatedauthToken"
            };
            using (var context = new SharingCodeContext(options))
            {
                var usersController = new UsersController(context, mockScWorker.Object, mockApiComm.Object);
                var result = await usersController.PostUser(updatedUser.SteamId, updatedUser.SteamAuthToken, updatedUser.LastKnownSharingCode);
                Assert.IsInstanceOfType(result, typeof(Microsoft.AspNetCore.Mvc.OkResult));
            }

            // Verify that the updated user, and only the updated user was stored in database correctly and WorkUser was called on him
            using (var context = new SharingCodeContext(options))
            {
                var userFromDb = context.Users.Single();
                Assert.IsTrue(userFromDb.SteamId == updatedUser.SteamId);
                Assert.IsTrue(userFromDb.LastKnownSharingCode == updatedUser.LastKnownSharingCode);
                Assert.IsTrue(userFromDb.SteamAuthToken == updatedUser.SteamAuthToken);
                Assert.IsTrue(userFromDb.Invalidated == false);

                mockScWorker.Verify(x => x.WorkUser(It.Is<User>(x=>x.SteamAuthToken == updatedUser.SteamAuthToken), false), Times.Once()); 
            }
        }

        /// <summary>
        /// Tests DELETE: api/Users/<steamId> and verifies that the user was deleted
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task DeleteUserTest()
        {
            var options = TestHelper.GetDatabaseOptions("DeleteUserTest");
            var user = TestHelper.GetRandomUser();


            // Create valid user
            using (var context = new SharingCodeContext(options))
            {
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }

            // Call DeleteUser
            using (var context = new SharingCodeContext(options))
            {
                var mockApiComm = new Mock<IValveApiCommunicator>();
                var mockScWorker = new Mock<ISharingCodeWorker>();

                var usersController = new UsersController(context, mockScWorker.Object, mockApiComm.Object);
                var result = await usersController.DeleteUser(user.SteamId);
                Assert.IsInstanceOfType(result, typeof(Microsoft.AspNetCore.Mvc.OkResult));
            }

            // Verify that the user was deleted
            using (var context = new SharingCodeContext(options))
            {
                var userInDb = context.Users.Any();
                Assert.IsFalse(userInDb);
            }
        }

        /// <summary>
        /// Tests GET: api/Users/<steamId>/LookForMatches
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task LookForMatchesTest()
        {
            var options = TestHelper.GetDatabaseOptions("LookForMatchesTest");
            var user = TestHelper.GetRandomUser();

            // Create valid user
            using (var context = new SharingCodeContext(options))
            {
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }

            // Create valid user, call LookForMatches on him and verify that WorkUser was called
            using (var context = new SharingCodeContext(options))
            {
                var mockApiComm = new Mock<IValveApiCommunicator>();
                var mockScWorker = new Mock<ISharingCodeWorker>();
                var usersController = new UsersController(context, mockScWorker.Object, mockApiComm.Object);

                // Call LookForMatches
                var lfmResponse = await usersController.PostLookForMatches(user.SteamId);
                // Verify that WorkUser was called
                mockScWorker.Verify(x => x.WorkUser(It.Is<User>(x => x.SteamId == user.SteamId), true), Times.Once);
            }
        }
    }
}
