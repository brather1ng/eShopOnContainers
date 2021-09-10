using eShopConContainers.WebSPA;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Enrichers.Span;
using Serilog.Formatting.Compact;
using System.IO;

BuildWebHost(args).Run();

IWebHost BuildWebHost(string[] args) =>
    WebHost.CreateDefaultBuilder(args)
     .UseStartup<Startup>()
        .UseContentRoot(Directory.GetCurrentDirectory())
        .ConfigureAppConfiguration((builderContext, config) =>
        {
            config.AddEnvironmentVariables();
        })
        .ConfigureLogging((hostingContext, builder) =>
        {
            builder.AddConfiguration(hostingContext.Configuration.GetSection("Logging"));
            builder.AddConsole();
            builder.AddDebug();
            builder.AddAzureWebAppDiagnostics();
        })
        .UseSerilog((builderContext, config) =>
        {
            config
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .Enrich.WithSpan()
                .WriteTo.Seq("http://seq")
                .WriteTo.Console(new RenderedCompactJsonFormatter())
                .ReadFrom.Configuration(builderContext.Configuration);
        })
        .Build();
