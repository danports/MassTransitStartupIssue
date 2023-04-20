using MassTransit;
using MassTransitStartupIssue;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Quartz;

var host = new HostBuilder()
    .ConfigureHostConfiguration(configHost => configHost.AddCommandLine(args))
    .ConfigureAppConfiguration((hostContext, configApp) => configApp.AddJsonFile("appsettings.json", true, true))
    .ConfigureLogging((context, logging) =>
    {
        logging.AddConfiguration(context.Configuration.GetSection("Logging"));
        logging.AddConsole();
        logging.AddDebug();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddQuartz(q =>
        {
            q.SchedulerName = "MassTransit-Scheduler";
            q.UseMicrosoftDependencyInjectionJobFactory();
            q.UseTimeZoneConverter();
            q.MisfireThreshold = TimeSpan.FromSeconds(60);
            q.UseDefaultThreadPool(p => p.MaxConcurrency = 32);
        });

        services.Configure<MassTransitHostOptions>(options => options.WaitUntilStarted = true);
        services.AddMassTransit(x =>
        {
            x.AddQuartzConsumers(quartz =>
            {
                quartz.QueueName = "Development_Quartz";
                quartz.PrefetchCount = 4;
            });

            x.UsingAmazonSqs((registration, sqs) =>
            {
                sqs.Host("us-east-1", h =>
                {
                    h.AccessKey("access-key");
                    h.SecretKey("secret-key");
                    h.Scope("Development");
                    h.EnableScopedTopics();
                });
                sqs.ConfigureEndpoints(registration);
            });
        });

        services.AddSingleton<IHostedService, SampleHostedService>();
    })
    .UseConsoleLifetime()
    .Build();

Console.WriteLine("Starting host...");
var run = host.RunAsync();

var bus = host.Services.GetRequiredService<IBusControl>();
await bus.WaitForHealthStatus(BusHealthStatus.Healthy, TimeSpan.FromMinutes(1));
Console.WriteLine("Bus ready!");

while (!(await host.Services.GetRequiredService<ISchedulerFactory>().GetScheduler()).IsStarted)
    await Task.Delay(1000);
Console.WriteLine("Scheduler started!");

await run;