namespace Ordering.BackgroundTasks
{
    using HealthChecks.UI.Client;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Diagnostics.HealthChecks;
    using Microsoft.Extensions.Configuration;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using OpenTelemetry.Resources;
    using OpenTelemetry.Trace;
    using Ordering.BackgroundTasks.Extensions;
    using Ordering.BackgroundTasks.Services;
    using System;
    using System.Collections.Generic;

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public virtual void ConfigureServices(IServiceCollection services)
        {
            services.AddCustomHealthCheck(this.Configuration)
                .Configure<BackgroundTaskSettings>(this.Configuration)
                .AddOptions()
                .AddHostedService<GracePeriodManagerService>()
                .AddEventBus(this.Configuration)
                .AddOpenTelemetryTracing(builder => builder
                    .SetResourceBuilder(ResourceBuilder.CreateDefault()
                        .AddService(Configuration["ServiceName"])
                        .AddAttributes(new[] { new KeyValuePair<string, object>("service", Configuration["ServiceName"]), new KeyValuePair<string, object>("application", Program.AppName) }))
                    .AddAspNetCoreInstrumentation()
                    .AddGrpcClientInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddSqlClientInstrumentation()
                    .AddOtlpExporter(options => options.Endpoint = new Uri("http://tempo:55680"))); ;
        }


        public void Configure(IApplicationBuilder app, ILoggerFactory loggerFactory)
        {
            app.UseRouting();
            app.UseEndpoints(endpoints =>
            {
                endpoints.MapHealthChecks("/hc", new HealthCheckOptions()
                {
                    Predicate = _ => true,
                    ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                });
                endpoints.MapHealthChecks("/liveness", new HealthCheckOptions
                {
                    Predicate = r => r.Name.Contains("self")
                });
            });
        }
    }
}
