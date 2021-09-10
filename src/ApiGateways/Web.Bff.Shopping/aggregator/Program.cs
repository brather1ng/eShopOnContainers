using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.eShopOnContainers.Web.Shopping.HttpAggregator;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;

BuildWebHost(args).Run();

IWebHost BuildWebHost(string[] args) =>
    WebHost
        .CreateDefaultBuilder(args)
        .ConfigureAppConfiguration(cb =>
        {
            var sources = cb.Sources;
            sources.Insert(3, new Microsoft.Extensions.Configuration.Json.JsonConfigurationSource()
            {
                Optional = true,
                Path = "appsettings.localhost.json",
                ReloadOnChange = false
            });
        })
        .UseStartup<Startup>()
        .UseSerilog((builderContext, config) =>
        {
            config
                .MinimumLevel.Information()
                .Enrich.WithProperty("ApplicationContext", "Web.Shopping.HttpAggregator")
                .Enrich.FromLogContext()
                .Enrich.WithSpan()
                .WriteTo.Console(new RenderedCompactJsonFormatter());
        })
        .Build();