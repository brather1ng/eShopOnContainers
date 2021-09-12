using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using OpenTelemetry.Metrics;
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.eShopOnContainers.OpenTelemetry
{
    internal sealed class MetricsHostedService : IHostedService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Stopwatch _upTimeWatch = new();

        public MetricsHostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _upTimeWatch.Start();

            _serviceProvider.GetRequiredService<MeterProvider>();
            var meter = _serviceProvider.GetRequiredService<Meter>();

            meter.CreateObservableCounter("http.server.uptime", () => _upTimeWatch.ElapsedMilliseconds, "milliseconds");

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _upTimeWatch.Stop();
            return Task.CompletedTask;
        }
    }
}
