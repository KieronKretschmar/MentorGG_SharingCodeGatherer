using System;
using Database;
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using RabbitCommunicationLib.Interfaces;
using RabbitCommunicationLib.TransferModels;
using SharingCodeGatherer;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Configuration;

namespace SharingCodeGathererTests
{
    [TestClass]
    public class ValveApiCommunicatorTests
    {
        private readonly IConfiguration config;
        private readonly ServiceProvider serviceProvider;

        public ValveApiCommunicatorTests()
        {
            var services = new ServiceCollection();
            services.AddLogging(x => x.AddConsole().AddDebug());
            serviceProvider = services.BuildServiceProvider();

            var builder = new ConfigurationBuilder()
                .AddEnvironmentVariables();
            config = builder.Build();
        }

        /// <summary>
        /// Tests QueryNextSharingCode() with valid data, expects a valid sharingCode or a NoMatchesFoundException.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task QueryNextSharingCodeTest()
        {
            var user = TestHelper.GetValidUser();

            // Create ValveApiCommunicator
            var apiComm = new ValveApiCommunicator(serviceProvider.GetService<ILogger<ValveApiCommunicator>>(), config);

            // Call WorkUser and expect either a valid sharingCode with length 34 or a NoMatchesFoundException
            try
            {
                var sharingCode = await apiComm.QueryNextSharingCode(user);
                Assert.IsTrue(sharingCode.Count() == 34);
            }
            catch (ValveApiCommunicator.NoMatchesFoundException)
            {

            }
            catch(Exception e)
            {
                Assert.Fail();
            }
        }

        /// <summary>
        /// Tests QueryNextSharingCode() with Invalid User Auth data, expects InvalidUserAuthException.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task InvalidUserAuthTest()
        {
            var user = TestHelper.GetRandomUser();

            // Create ValveApiCommunicator
            var apiComm = new ValveApiCommunicator(serviceProvider.GetService<ILogger<ValveApiCommunicator>>(), config);

            // Call WorkUser and expect InvalidUserAuthException
            await Assert.ThrowsExceptionAsync<ValveApiCommunicator.InvalidUserAuthException>(async () =>
            {
                await apiComm.QueryNextSharingCode(user);
            });
        }

        /// <summary>
        /// Tests QueryNextSharingCode() with Invalid API Key, expects InvalidApiKeyException.
        /// </summary>
        /// <returns></returns>
        [TestMethod]
        public async Task InvalidApiKeyTest()
        {
            var user = TestHelper.GetValidUser();

            // mock config to return invalid api key
            var mockConfigSection = new Mock<IConfigurationSection>();
            mockConfigSection.Setup(x => x.Value).Returns("ThisIsNotAnApiKey");
            Mock<IConfiguration> mockConfig = new Mock<IConfiguration>();
            mockConfig.Setup(c => c.GetSection(It.IsAny<String>())).Returns(mockConfigSection.Object);

            // Create ValveApiCommunicator
            var apiComm = new ValveApiCommunicator(serviceProvider.GetService<ILogger<ValveApiCommunicator>>(), mockConfig.Object);

            // Call WorkUser and expect InvalidApiKeyException
            await Assert.ThrowsExceptionAsync<ValveApiCommunicator.InvalidApiKeyException>(async () =>
            {
                await apiComm.QueryNextSharingCode(user);
            });
        }
    }
}