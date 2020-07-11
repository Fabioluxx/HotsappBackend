using Hotsapp.Data.Util;
using Hotsapp.PlaylistWorker.Service;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Exceptions;

namespace Hotsapp.PlaylistWorker
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureServices((hostContext, services) =>
                {
                    var sp = services.BuildServiceProvider();
                    var config = sp.GetRequiredService<IConfiguration>();
                    DataFactory.Initialize(sp, config.GetConnectionString("MySql"));

                    Log.Logger = new LoggerConfiguration()
                        .Enrich.FromLogContext()
                        .Enrich.WithExceptionDetails()
                        .WriteTo.Console()
                        .CreateLogger();

                    services.AddMessaging(config.GetConnectionString("RabbitMQ"));
                    services.AddSingleton<PlaylistWorkerMessagingService>();

                    services.AddSingleton<PlaylistRepository>();
                    services.AddTransient<ChannelWorker>();
                    services.AddSingleton<ChannelWorkerFactory>();
                    services.AddHostedService<WorkerManager>();

                    services.Configure<HostOptions>(option =>
                    {
                        option.ShutdownTimeout = System.TimeSpan.FromSeconds(20);
                    });
                }).UseSerilog();
    }
}
