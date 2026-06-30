using ETS.Infrastructure;
using Serilog;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);
var assembly = Assembly.GetExecutingAssembly();

IWebHostEnvironment env = builder.Environment;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

builder.ConfigureDefaultSettings();

builder.Services
    .ConfigureInfrastructureServices(builder.Configuration)
    .ConfigureApplicationServices(builder.Configuration)
    .ConfigurePresentationSettings(assembly, builder.Configuration);

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.WebAppBuilderPipelineBuilder();
app.Run();
