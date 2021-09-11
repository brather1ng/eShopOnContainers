using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using System;
using System.Diagnostics.Metrics;
using System.Threading;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOpenTelemetryMetrics(this IServiceCollection services, string meterName)
        {
            services.AddOptions<PrometheusExporterOptions>()
                .Configure(options => options.Url = "/metrics");
            return services
                .AddSingleton(p => new PrometheusExporter(p.GetRequiredService<IOptions<PrometheusExporterOptions>>().Value))
                .AddSingleton(p => new PullMetricProcessor(p.GetRequiredService<PrometheusExporter>(), false))
                .AddSingleton<IHostedService, MetricsHostedService>()
                .AddSingleton(CreateMeterProvider)
                .AddSingleton(_ => new Meter(meterName));

            MeterProvider CreateMeterProvider(IServiceProvider p) =>
                Sdk.CreateMeterProviderBuilder()
                    .AddSource(meterName)
                    .AddMetricProcessor(p.GetRequiredService<PullMetricProcessor>())
                    .Build();
        }

        public static IApplicationBuilder UseMetrics(this IApplicationBuilder app)
        {
            var options = app.ApplicationServices.GetRequiredService<IOptions<PrometheusExporterOptions>>().Value;
            app.Map(new PathString(options.Url), a => a.Run(ExportPrometheusAsync));
            return app;

            async Task ExportPrometheusAsync(HttpContext context)
            {
                var processor = app.ApplicationServices.GetRequiredService<PullMetricProcessor>();
                var exporter = app.ApplicationServices.GetRequiredService<PrometheusExporter>();

                processor.PullRequest();
                var result = exporter.GetMetricsCollection();
                await context.Response.WriteAsync(result);
            }
        }
    }

    internal sealed class MetricsHostedService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private Counter<long> _counter;
        private Timer _timer;

        public MetricsHostedService(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _serviceProvider.GetRequiredService<MeterProvider>();
            _counter = _serviceProvider.GetRequiredService<Meter>()
                .CreateCounter<long>("uptime", "incremented by 1 each second");
            _timer = new Timer(DoWork, null, TimeSpan.Zero, TimeSpan.FromSeconds(1));
            return Task.CompletedTask;
        }

        private void DoWork(object state)
        {
            _counter.Add(1);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }
    }
}
