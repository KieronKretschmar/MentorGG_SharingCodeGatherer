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
                   await scWorker.WorkUser(user, AnalyzerQuality.Low,true);
               });
            }

            // Check that the user was not invalidated
            using (var context = new SharingCodeContext(options))
            {
                var invalidatedUser = context.Users.Single();
                Assert.IsFalse(invalidatedUser.Invalidated);
            }
        }
    }
}
