using CloudinaryDotNet;
using ETS.Application;
using ETS.Application.Behaviours;
using ETS.Domain.AppConfig;
using ETS.Domain.Contracts;
using ETS.Infrastructure.Authentication;
using ETS.Infrastructure.Data;
using ETS.Infrastructure.Persistence;
using ETS.Infrastructure.Services;
using ETS.WebApi.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using System.Reflection;
using System.Text.Json.Serialization;

namespace ETS.Infrastructure
{
    public static class DependencyInjection
    {
        private static string CorsPolicyName = "ets-report-analytics-policy";

        public static void ConfigureDefaultSettings(this WebApplicationBuilder builder)
        {
            var serviceName = "ets-report-analytics-service";

            builder.Logging.ClearProviders();

            // configure serilog
            builder.Host.UseSerilog((ctx, sp, loggerConfiguration) =>
            {
                loggerConfiguration
                    .ReadFrom.Configuration(ctx.Configuration)
                    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
                    .MinimumLevel.Override("System", LogEventLevel.Warning)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("ServiceName", serviceName)
                    .Enrich.WithProperty("LogType", "Log")
                    .WriteTo.Console()
                    .WriteTo.File(
                    "logs/app.log",
                    rollingInterval: RollingInterval.Month,
                    outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss} [{Level:u3}] {TraceId} {Message:1j}{NewLine}{Exception}");

            });
        }

        public static IServiceCollection ConfigurePresentationSettings(this IServiceCollection services, Assembly assembly,
           IConfiguration configuration)
        {
            services.AddEndpointsApiExplorer()
                .AddSwaggerGenWithAuth(assembly, configuration);

            services.AddExceptionHandler<GlobalExceptionHandler>();
            services.AddProblemDetails();

            return services;
        }

        private static IServiceCollection AddSwaggerGenWithAuth(this IServiceCollection services, Assembly assembly,
            IConfiguration configuration)
        {
            var serviceName = "ets-report-and-analytics-service";

            services.AddSwaggerGen(o =>
            {
                o.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
                {
                    Title = serviceName,
                    Version = "v1",
                    Description = "Event Ticketing Event and Analytics API"
                });
                o.CustomSchemaIds(id => id.FullName!.Replace('+', '-'));

                var baseDir = AppContext.BaseDirectory;
                var webApiXml = Path.Combine(baseDir, $"{assembly.GetName().Name}.xml");
                if (File.Exists(webApiXml))
                    o.IncludeXmlComments(webApiXml);
                var appAssembly = Assembly.GetExecutingAssembly();
                var appXml = Path.Combine(baseDir, $"{appAssembly.GetName().Name}.xml");
                if (File.Exists(appXml))
                    o.IncludeXmlComments(appXml);
                o.TagActionsBy(api =>
                {
                    if (api.GroupName != null) return [api.GroupName];
                    var routeValues = api.ActionDescriptor.RouteValues;
                    var controller = routeValues.TryGetValue("controller", out var c) ? c : null;
                    return controller != null ? [controller] : null;
                });


                o.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Type = SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    Description = "Enter your JWT token. \n\nExample: \"eyJhbciosjdIEDJksk\"",
                    Name = "Authorization",
                    In = ParameterLocation.Header
                });

                o.AddSecurityRequirement(document => new OpenApiSecurityRequirement
                {
                    [new OpenApiSecuritySchemeReference("Bearer", document)] = []
                });

            });

            return services;
        }

        public static IApplicationBuilder WebAppBuilderPipelineBuilder(this WebApplication app)
        {
            app.UseExceptionHandler();
            app.UseHttpsRedirection();
            app.UseRouting();
            app.UseSerilogRequestLogging();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseSwagger();
            app.UseSwaggerUI();                            
            app.MapControllers();

            return app;
        }


        public static IServiceCollection ConfigureApplicationServices(this IServiceCollection services, IConfiguration configuration)
        {
            var assemblies = new[]
            {
                typeof(DependencyInjection).Assembly,
                typeof(IApplicationDependencyMarker).Assembly
            };

            services.AddApplicationDependency(configuration, assemblies);            
            services.Configure<PostmarkConfigOptions>(configuration.GetSection("PostmarkServiceConfig"));            

            return services;
        }

        private static IServiceCollection AddApplicationDependency(this IServiceCollection services,
           IConfiguration configuration, Assembly[] assemblies)
        {
            services.AddMediatR(config =>
            {
                config.RegisterServicesFromAssemblies(assemblies);                                
                config.AddOpenBehavior(typeof(UnhandledExceptionBehaviour<,>));                

            });

            foreach(var assembly in assemblies)
                services.AutoRegisterBackgroundJobs(assembly);

            return services;
        }

        private static void AutoRegisterBackgroundJobs(this IServiceCollection services, Assembly assembly)
        {
            var hostedServiceTypes = assembly
                .GetTypes()
                .Where(t => typeof(IHostedService).IsAssignableFrom(t) &&
                t is { IsAbstract: false, IsClass: true });

            foreach (var serviceType in hostedServiceTypes)
            {
                services.AddSingleton(typeof(IHostedService), serviceType);
            }
        }


        public static IServiceCollection ConfigureInfrastructureServices(this IServiceCollection services,
           IConfiguration configuration)
        {
            string? connString = configuration.GetConnectionString("AppDbConnection");
            services.AddDbContextFactory<ApplicationDbContext>(opt => opt
                        .UseSqlServer(connString));

            services.AddSingleton<ISqlConnectionFactory>(_ => new SqlConnectionFactory(connString!));

            var originStr = configuration.GetSection("AllowedOrigins").Get<string>();
            var allowedOrigins = string.IsNullOrEmpty(originStr)
                ? []
                : originStr.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            services.AddControllers()
                .AddJsonOptions(opt => opt.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()))
                .ConfigureApiBehaviorOptions(options =>
                {
                    options.SuppressModelStateInvalidFilter = false;

                    options.InvalidModelStateResponseFactory = context =>
                    {
                        var httpContext = context.HttpContext;
                        var path = httpContext.Request.Path;

                        var message = string.Join(", ", context.ModelState.Values.SelectMany(a => a.Errors)
                            .Select(e => e.ErrorMessage));

                        var problemDetails = new ValidationProblemDetails
                        {
                            Title = "Invalid Request",
                            Status = StatusCodes.Status400BadRequest,
                            Detail = message,
                            Instance = path,
                            Type = "InvalidRequest"
                        };

                        return new BadRequestObjectResult(problemDetails);
                    };
                });

            services.AddEndpointsApiExplorer();
            services.AddCors(options =>
            {
                options.AddPolicy(name: CorsPolicyName, builder =>
                {
                    builder.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
                });
            });
            services.AddHttpContextAccessor();
            services.AddAuthenticationSection(configuration);

            return services;
        }

        private static IServiceCollection AddAuthenticationSection(this IServiceCollection services,
           IConfiguration configuration)
        {
            services.Configure<AppSettings>(configuration.GetSection("AppSettings"));
            services.Configure<AuthenticationOptions>(configuration.GetSection("Authentication"));
            services.AddScoped<IUserContext, ETS.Infrastructure.Authentication.UserContext>();

            services.Configure<CloudinarySettings>(configuration.GetSection("Cloudinary"));
            services.AddSingleton(sp =>
            {
                var settings = sp.GetRequiredService<IOptions<CloudinarySettings>>().Value;
                var account = new Account(settings.CloudName, settings.ApiKey, settings.ApiSecret);
                return new Cloudinary(account);
            });

            services.AddSingleton<IDocumentService, DocumentService>();
            services.AddHttpClient<IMicroserviceHttpClient, MicroserviceHttpClient>();            
            services.AddSingleton<IEmailService, EmailService>();
            services.AddSingleton<IQRCodeService, QRCodeService>();

            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer();

            services.ConfigureOptions<JwtBearerOptionsSetup>();

            return services;
        }


    }
}
