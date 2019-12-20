using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitTransfer.Producer;
using RabbitTransfer.Queues;
using RabbitTransfer.TransferModels;

namespace SharingCodeGatherer
{
    /// <summary>
    /// 
    /// Requires environment variables: ["AMQP_URI", "AMQP_SHARINGCODE_QUEUE", "MYSQL_CONNECTION_STRING"]
    /// </summary>
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers();
            services.AddLogging(x => x.AddConsole().AddDebug());

            services.AddTransient<ISharingCodeWorker, SharingCodeWorker>();
            services.AddSingleton<IValveApiCommunicator, ValveApiCommunicator>();


            // Create producer
            var connection = new QueueConnection(
                Configuration.GetValue<string>("AMQP_URI"),
                Configuration.GetValue<string>("AMQP_SHARINGCODE_QUEUE"));

            services.AddSingleton<Producer<SCG_SWS_Model>>(sp =>
            {
                return new Producer<SCG_SWS_Model>(connection);
            });

            // if a connectionString is set use mysql, else use InMemory
            var connString = Configuration.GetValue<string>("MYSQL_CONNECTION_STRING");
            if (connString != null)
            {
                services.AddDbContext<Database.SharingCodeContext>(o => { o.UseMySql(connString); });
            }
            else
            {
                services.AddEntityFrameworkInMemoryDatabase()
                    .AddDbContext<Database.SharingCodeContext>((sp, options) =>
                    {
                        options.UseInMemoryDatabase(databaseName: "MyInMemoryDatabase").UseInternalServiceProvider(sp);
                    });
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
