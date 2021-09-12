using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenTelemetry;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;

namespace Microsoft.eShopOnContainers.OpenTelemetry
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddOpenTelemetryTracing(this IServiceCollection services, IConfiguration configuration, string application) =>
            services.AddOpenTelemetryTracing(builder => builder
                .SetResourceBuilder(CreateResourceBuilder(configuration, application))
                .AddAspNetCoreInstrumentation()
                .AddGrpcClientInstrumentation()
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
                .AddOtlpExporter(options => options.Endpoint = new Uri("http://tempo:55680")));

        public static IServiceCollection AddOpenTelemetryMetrics(this IServiceCollection services, IConfiguration configuration, string application)
        {
            services.AddOptions<PrometheusExporterOptions>()
                .Configure(options => options.Url = "/metrics");
            return services
                .AddSingleton(p => new PrometheusExporter(p.GetRequiredService<IOptions<PrometheusExporterOptions>>().Value))
                .AddSingleton(p => new PullMetricProcessor(p.GetRequiredService<PrometheusExporter>(), false))
                .AddSingleton<IHostedService, MetricsHostedService>()
                .AddSingleton(CreateMeterProvider)
                .AddSingleton(_ => new Meter(application));

            MeterProvider CreateMeterProvider(IServiceProvider p) =>
                Sdk.CreateMeterProviderBuilder()
                    .SetResourceBuilder(CreateResourceBuilder(configuration, application))
                    .AddSource(application)
                    .AddMetricProcessor(p.GetRequiredService<PullMetricProcessor>())
                    .Build();
        }

        private static ResourceBuilder CreateResourceBuilder(IConfiguration configuration, string application) =>
            ResourceBuilder.CreateDefault()
                .AddService(configuration["ServiceName"])
                .AddAttributes(new[] { new KeyValuePair<string, object>("service", configuration["ServiceName"]), new KeyValuePair<string, object>("application", application) });
    }
}
