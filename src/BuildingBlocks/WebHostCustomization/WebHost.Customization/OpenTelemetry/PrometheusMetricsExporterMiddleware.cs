using Microsoft.AspNetCore.Http;
using OpenTelemetry.Exporter;
using OpenTelemetry.Metrics;
using System.Threading.Tasks;

namespace Microsoft.eShopOnContainers.OpenTelemetry
{
    internal class PrometheusMetricsExporterMiddleware
    {
        private readonly PullMetricProcessor _pullMetricProcessor;
        private readonly PrometheusExporter _exporter;

        public PrometheusMetricsExporterMiddleware(
            RequestDelegate _, PullMetricProcessor pullMetricProcessor, PrometheusExporter exporter)
        {
            _pullMetricProcessor = pullMetricProcessor;
            _exporter = exporter;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            _pullMetricProcessor.PullRequest();
            var result = _exporter.GetMetricsCollection();
            await httpContext.Response.WriteAsync(result);
        }
    }
}
