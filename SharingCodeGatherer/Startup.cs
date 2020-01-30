using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
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

            #region Database

            // if a connectionString is set use mysql, else use InMemory
            var connString = Configuration.GetValue<string>("MYSQL_CONNECTION_STRING");
            if (connString != null)
            {
                services.AddDbContext<Database.SharingCodeContext>(o => { o.UseMySql(connString); });
            }
            else
            {
                Console.WriteLine("WARNING: Using in memory database! Is `MYSQL_CONNECTION_STRING` set?");
                services.AddEntityFrameworkInMemoryDatabase()
                    .AddDbContext<Database.SharingCodeContext>((sp, options) =>
                    {
                        options.UseInMemoryDatabase(databaseName: "MyInMemoryDatabase").UseInternalServiceProvider(sp);
                    });
            }

            if (Configuration.GetValue<bool>("IS_MIGRATING"))
            {
                Console.WriteLine("WARNING: Migrating!");
                return;
            }

            #endregion

            services.AddTransient<ISharingCodeWorker, SharingCodeWorker>();
            services.AddSingleton<IValveApiCommunicator, ValveApiCommunicator>();

            #region RabbitMQ

            // Create producer
            var connection = new QueueConnection(
                Configuration.GetValue<string>("AMQP_URI"),
                Configuration.GetValue<string>("AMQP_SHARINGCODE_QUEUE"));

            services.AddSingleton<Producer<SCG_SWS_Model>>(sp =>
            {
                return new Producer<SCG_SWS_Model>(connection);
            });

            #endregion

            #region Swagger
            services.AddSwaggerGen(options =>
            {
                OpenApiInfo interface_info = new OpenApiInfo { Title = "SharingCodeGatherer", Version = "v1", };
                options.SwaggerDoc("v1", interface_info);

                // Generate documentation based on the XML Comments provided.
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                options.IncludeXmlComments(xmlPath);

                // Optionally, if installed, enable annotations
                options.EnableAnnotations();
            });
            #endregion

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

            #region Swagger
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.RoutePrefix = "swagger";
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "SharingCodeGatherer");
            });
            #endregion

        }
    }
}
