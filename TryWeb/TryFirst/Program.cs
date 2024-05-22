using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging.Configuration;
using NLog;
using NLog.Web;
using System;
using System.Collections;
using System.Data.SQLite;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using NLog;
using Microsoft.Extensions.Logging;
using Archivator;
using static Archivator.Dependecies;

namespace ServerApp
{
    class Program
    {
        private static void Main(string[] args)
        {
            EnsureDB();
            FileManager.Instance();
            BuildWithLog(args);
        }

        private static void BuildWithLog(string[] args)
        {
            var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            try
            {
                BuildAndRun(args);
            }
            catch (Exception ex)
            {
                logger.Error(ex.Message);
            }
            finally
            {
                NLog.LogManager.Shutdown();
            }
        }
        private static void BuildAndRun(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Logging.ClearProviders().SetMinimumLevel(Microsoft.Extensions.Logging.LogLevel.Trace);
            builder.Host.UseNLog();

            ConfigureServices(builder.Services);

            var app = builder.Build();
            ConfigureApp(app);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddControllers().AddNewtonsoftJson();
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen();
        }
        private static void ConfigureApp(WebApplication app)
        {
            IHostApplicationLifetime lifetime = app.Lifetime;
            lifetime.ApplicationStopped.Register(OnShutDown);
            lifetime.ApplicationStopping.Register(OnShutDown);
            lifetime.ApplicationStarted.Register(() =>
               Console.WriteLine("App started"));
            // Configure the HTTP request pipeline.
            app.UseSwagger();
            app.UseSwaggerUI();
            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
        private static void EnsureDB()
        {
            string s = AppDomain.CurrentDomain.BaseDirectory;
            if (!Directory.Exists(Path.Combine(s, "Logs")))
                Directory.CreateDirectory(Path.Combine(s, "Logs"));
            if (!Directory.Exists(Path.Combine(s, "AmazingCollections")))
                Directory.CreateDirectory(Path.Combine(s, "AmazingCollections"));
            if (!Directory.Exists(Path.Combine(s, "Archives")))
                Directory.CreateDirectory(Path.Combine(s, "Archives"));
            if (!Directory.Exists(Path.Combine(s, "Cache")))
                Directory.CreateDirectory(Path.Combine(s, "Cache"));

            if (File.Exists(Path.Combine(s, DATABASE_PATH)))
                return;
            using (var f = File.Create(Path.Combine(s, DATABASE_PATH))) { }

            using (SQLiteConnection connection = new SQLiteConnection(@$"Data Source={Path.Combine(s, DATABASE_PATH)};"))
            using (SQLiteCommand command = new SQLiteCommand(
                DATABASE_COMMAND,
                connection))
            {
                connection.Open();
                command.ExecuteNonQuery();
            }
        }
        private static void OnShutDown()
        {
            FileManager.Instance().OnShutDown();
        }
    }
}
