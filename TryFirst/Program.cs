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

namespace TryFirst
{
    class Program
    {
        private static void Main(string[] args)
        {
            EnsureDB();
            LogManager.ThrowExceptions = true;
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
            catch (Exception exception)
            {
                logger.Error(exception, "Stopped program because of exception");
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
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();

            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
        private static void EnsureDB()
        {
            string s = AppDomain.CurrentDomain.BaseDirectory;

            if (File.Exists(@$"{s}/Logs/Log.db"))
                return;
            using (var f = File.Create(@$"{s}/Logs/Log.db")) { }

            using (SQLiteConnection connection = new SQLiteConnection(@$"Data Source={s}/Logs/Log.db;"))
            using (SQLiteCommand command = new SQLiteCommand(
                "CREATE TABLE Log (" +
                "CreatedOn TEXT," +
                "Message TEXT," +
                "Level TEXT," +
                "Exception TEXT," +
                "StackTrace TEXT," +
                "Logger TEXT," +
                "Url TEXT);",
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
