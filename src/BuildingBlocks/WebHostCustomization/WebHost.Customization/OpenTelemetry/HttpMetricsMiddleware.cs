using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Threading.Tasks;

namespace Microsoft.eShopOnContainers.OpenTelemetry
{
    internal class HttpMetricsMiddleware
    {
        private static bool _instrumentsCreated;
        private static Counter<long> _activeRequests = null!;
        private static Counter<long> _totalRequests = null!;
        private static Histogram<long> _duration = null!;

        private readonly RequestDelegate _next;
        private readonly Meter _meter;

        public HttpMetricsMiddleware(RequestDelegate next, Meter meter)
        {
            _next = next;
            _meter = meter;
        }

        public async Task InvokeAsync(HttpContext httpContext)
        {
            var stopwatch = Stopwatch.StartNew();
            EnsureInstrumentsCreated();

            var commonAttributes = new[]
            {
                new KeyValuePair<string, object?>("http.method", httpContext.Request.Method),
                new KeyValuePair<string, object?>("http.host", httpContext.Request.Host),
                new KeyValuePair<string, object?>("http.scheme", httpContext.Request.Scheme),
                // todo replace route/query values by their keys
                new KeyValuePair<string, object?>("http.target", httpContext.Features.Get<IHttpRequestFeature>().RawTarget),
            };

            try
            {
                _activeRequests.Add(1, commonAttributes);

                await _next(httpContext);
            }
            finally
            {
                _activeRequests.Add(-1, commonAttributes);

                var responseAttributes = new[]
                {
                    new KeyValuePair<string, object?>("http.status_code", httpContext.Response.StatusCode),
                };
                var fullAttributes = new KeyValuePair<string, object?>[commonAttributes.Length + responseAttributes.Length];
                Array.Copy(commonAttributes, fullAttributes, commonAttributes.Length);
                Array.Copy(responseAttributes, 0, fullAttributes, commonAttributes.Length, responseAttributes.Length);

                _totalRequests.Add(1, fullAttributes);
                _duration.Record(stopwatch.ElapsedMilliseconds, fullAttributes);
            }
        }

        private void EnsureInstrumentsCreated()
        {
            if (_instrumentsCreated)
                return;

            lock (_meter)
            {
                if (_instrumentsCreated)
                    return;

                _activeRequests = _meter.CreateCounter<long>("http.server.active_requests", "requests");
                _totalRequests = _meter.CreateCounter<long>("http.server.total_requests", "requests");
                _duration = _meter.CreateHistogram<long>("http.server.duration", "milliseconds");

                _instrumentsCreated = true;
            }
        }
    }
}
