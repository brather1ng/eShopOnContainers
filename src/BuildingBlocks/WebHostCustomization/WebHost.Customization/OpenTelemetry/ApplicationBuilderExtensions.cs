using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using OpenTelemetry.Exporter;

namespace Microsoft.eShopOnContainers.OpenTelemetry
{
    public static class ApplicationBuilderExtensions
    {
        public static IApplicationBuilder UsePrometheusMetricsExporter(this IApplicationBuilder app)
        {
            var options = app.ApplicationServices.GetRequiredService<IOptions<PrometheusExporterOptions>>().Value;
            return app.Map(new PathString(options.Url), a => a.UseMiddleware<PrometheusMetricsExporterMiddleware>());
        }

        public static IApplicationBuilder UseMetricsMiddleware(this IApplicationBuilder app) =>
            app.UseMiddleware<HttpMetricsMiddleware>();
    }
}
