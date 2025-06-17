// See https://aka.ms/new-console-template for more information
using BACnet;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

Console.WriteLine("Hello, World!");
var builder = WebApplication.CreateSlimBuilder();

var console = LoggerFactory.Create(builder =>
{
    builder
        .AddConsole()
        .SetMinimumLevel(LogLevel.Trace);
});

WebApplication app = builder.Build();
using IServiceScope scope = app.Services.CreateScope();

var loggerFactory = scope.ServiceProvider.GetService<ILoggerFactory>() ?? NullLoggerFactory.Instance;
var bacnet = new Bacnet(new("bacnet"), loggerFactory);
//bacnet.Start();
//bacnet.WhoIs();