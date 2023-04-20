using Microsoft.Extensions.Hosting;

namespace MassTransitStartupIssue
{
    public class SampleHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Sample service started!");
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            Console.WriteLine("Sample service stopped!");
            return Task.CompletedTask;
        }
    }
}
