using Database;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitCommunicationLib.Enums;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using SharingCodeGatherer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharingCodeGathererTests
{
    [TestClass]
    public class SharingCodeWorkerTests
    {
        private readonly ServiceProvider serviceProvider;

        public SharingCodeWorkerTests()
        {
            var services = new ServiceCollection();
            services.AddLogging(x => x.AddConsole().AddDebug());

            serviceProvider = services.BuildServiceProvider();
        }

        /// <summary>
        /// Tests WorkUser() with invalid api key, expects InvalidApiKeyException and asserts that the user was not invalidated.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task InvalidApiKeyTest()
        {
            var options = TestHelper.GetDatabaseOptions("InvalidApiKeyTest");
            var user = TestHelper.GetRandomUser();

            using (var context = new SharingCodeContext(options))
            {
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }

            // Mock IValveApiCommunicator such that it mimicks behaviour of invalid apiKey
            var mockApiComm = new Mock<IValveApiCommunicator>();
            mockApiComm
                .Setup(x => x.QueryNextSharingCode(It.Is<User>(x => x.SteamId == user.SteamId)))
                .Throws(new ValveApiCommunicator.InvalidApiKeyException(""));

            var mockRabbit = new Mock<IProducer<SharingCodeInstruction>>();

            // Work user and check for InvalidApiKeyException exception
            using (var context = new SharingCodeContext(options))
            {
                var scWorker = new SharingCodeWorker(
                    serviceProvider.GetService<ILogger<ISharingCodeWorker>>(),
                    context,
                    mockApiComm.Object,
                    mockRabbit.Object
                    );

               await Assert.ThrowsExceptionAsync<ValveApiCommunicator.InvalidApiKeyException>(async () =>
               {
                   await scWorker.WorkUser(user, AnalyzerQuality.Low);
               });
            }

            // Check that the user was not invalidated
            using (var context = new SharingCodeContext(options))
            {
                var invalidatedUser = context.Users.Single();
                Assert.IsFalse(invalidatedUser.Invalidated);
            }
        }

        /// <summary>
        /// Tests WorkUser() with mocked dependencies and tests whether entries for Match and Upload are inserted.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task UploadMatchTest()
        {
            var options = TestHelper.GetDatabaseOptions("UploadMatchTest");

            // Test constants
            const string testSharingCode = "ABCDEFG123";
            const AnalyzerQuality testQuality = AnalyzerQuality.Low;



            // Add user to database
            var user = TestHelper.GetRandomUser();
            using (var context = new SharingCodeContext(options))
            {
                context.Users.Add(user);
                await context.SaveChangesAsync();
            }

            // Mock IValveApiCommunicator to return a sharingCode once and then no more
            var mockApiComm = new Mock<IValveApiCommunicator>();
            mockApiComm
                .SetupSequence(x => x.QueryNextSharingCode(It.Is<User>(x => x.SteamId == user.SteamId)))
                .Returns(Task.FromResult(testSharingCode))
                .Throws(new ValveApiCommunicator.NoMatchesFoundException(""));

            var mockRabbit = new Mock<IProducer<SharingCodeInstruction>>();

            // Work user
            using (var context = new SharingCodeContext(options))
            {
                var scWorker = new SharingCodeWorker(
                    serviceProvider.GetService<ILogger<ISharingCodeWorker>>(),
                    context,
                    mockApiComm.Object,
                    mockRabbit.Object
                    );
                await scWorker.WorkUser(user, testQuality);
            }

            // Check that the Match and Upload entries were inserted to database
            using (var context = new SharingCodeContext(options))
            {
                // load match from db including navigational properties
                var match = context.Matches
                    .Include(x => x.Uploads)
                        .ThenInclude(upload => upload.Uploader)
                    .Single();
                Assert.AreEqual(testSharingCode, match.SharingCode);
                Assert.AreEqual(testQuality, match.AnalyzedQuality);

                // Access Upload via ForeignKey to test navigational property
                var upload = match.Uploads.Single();

                Assert.AreEqual(testQuality, upload.Quality);

                // Access User via ForeignKey to test navigational property
                var uploader = upload.Uploader;

                Assert.AreEqual(user.SteamId, uploader.SteamId);
            }
        }
    }
}
