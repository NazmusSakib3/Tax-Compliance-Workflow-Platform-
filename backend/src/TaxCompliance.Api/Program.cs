using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using TaxCompliance.Api.Configuration;
using TaxCompliance.Api.Middleware;
using TaxCompliance.Application.Auth;
using TaxCompliance.Infrastructure;
using TaxCompliance.Infrastructure.Authentication;
using TaxCompliance.Infrastructure.Configuration;
using TaxCompliance.Infrastructure.Identity;
using RabbitMQ.Client;
using TaxCompliance.Infrastructure.Messaging;
using TaxCompliance.Infrastructure.Persistence;
using TaxCompliance.Infrastructure.Seeding;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, services, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .ReadFrom.Services(services)
    .Enrich.FromLogContext()
    .WriteTo.Console());

var jwtOptions = builder.Configuration.GetSection("Jwt").Get<JwtOptions>() ?? new JwtOptions();
var apiXmlDocumentationPath = Path.Combine(AppContext.BaseDirectory, $"{typeof(Program).Assembly.GetName().Name}.xml");

ProductionConfigurationValidator.Validate(builder.Configuration, jwtOptions, builder.Environment.EnvironmentName);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("Login", limiterOptions =>
    {
        limiterOptions.PermitLimit = builder.Configuration.GetValue("Security:LoginRateLimit:PermitLimit", 60);
        limiterOptions.Window = TimeSpan.FromMinutes(builder.Configuration.GetValue("Security:LoginRateLimit:WindowMinutes", 1));
        limiterOptions.QueueLimit = 0;
    });
});
builder.Services.AddCors(options =>
{
    options.AddPolicy("AngularSpa", policy =>
    {
        policy
            .AllowAnyHeader()
            .AllowAnyMethod()
            .SetIsOriginAllowed(origin => GetConfiguredCorsOrigins(builder.Configuration, builder.Environment)
                .Contains(origin.TrimEnd('/'), StringComparer.OrdinalIgnoreCase));
    });
});
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Tax Compliance Workflow Platform API",
        Version = "v1"
    });

    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
        Scheme = "bearer",
        BearerFormat = "JWT",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Description = "Paste only the JWT bearer token in this field.",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };

    options.AddSecurityDefinition(JwtBearerDefaults.AuthenticationScheme, jwtSecurityScheme);
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [jwtSecurityScheme] = Array.Empty<string>()
    });

    if (File.Exists(apiXmlDocumentationPath))
    {
        options.IncludeXmlComments(apiXmlDocumentationPath);
    }
});
builder.Services.AddInfrastructure(builder.Configuration, builder.Environment);
builder.Services.Configure<SecurityHeadersOptions>(builder.Configuration.GetSection(SecurityHeadersOptions.SectionName));

var openTelemetryOptions = builder.Configuration.GetSection(OpenTelemetryOptions.SectionName).Get<OpenTelemetryOptions>()
    ?? new OpenTelemetryOptions();
var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"]
    ?? builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(openTelemetryOptions.ServiceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation();

        if (!string.IsNullOrWhiteSpace(otlpEndpoint))
        {
            tracing.AddOtlpExporter(options => options.Endpoint = new Uri(otlpEndpoint));
        }
        else if (builder.Environment.IsDevelopment())
        {
            tracing.AddConsoleExporter();
        }
    });

var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
var redisConnection = builder.Configuration.GetConnectionString("Redis");
var useInMemoryInfrastructure = builder.Configuration.GetValue<bool>("Development:UseInMemoryInfrastructure");
var healthChecks = builder.Services.AddHealthChecks()
    .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy());
if (!string.IsNullOrWhiteSpace(defaultConnection))
{
    healthChecks.AddNpgSql(defaultConnection, name: "postgres");
}

if (!string.IsNullOrWhiteSpace(redisConnection))
{
    healthChecks.AddRedis(redisConnection, name: "redis");
}

if (!useInMemoryInfrastructure)
{
    var rabbitMqOptions = builder.Configuration.GetSection("RabbitMq").Get<RabbitMqOptions>() ?? new RabbitMqOptions();
    var rabbitMqConnection = RabbitMqConnectionStringBuilder.BuildAmqpUri(rabbitMqOptions);
    if (!string.IsNullOrWhiteSpace(rabbitMqConnection))
    {
        builder.Services.AddSingleton<IConnection>(_ =>
        {
            var factory = RabbitMqConnectionFactory.Create(rabbitMqOptions);
            return factory.CreateConnectionAsync().GetAwaiter().GetResult();
        });
        healthChecks.AddRabbitMQ(name: "rabbitmq");
    }
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AuthorizationPolicies.AdminOnly, policy =>
        policy.RequireRole(RoleNames.Admin));

    options.AddPolicy(AuthorizationPolicies.RuleManagement, policy =>
        policy.RequireRole(RoleNames.Admin, RoleNames.ComplianceManager));

    options.AddPolicy(AuthorizationPolicies.ContributorAccess, policy =>
        policy.RequireRole(RoleNames.Admin, RoleNames.ComplianceManager, RoleNames.Contributor));

    options.AddPolicy(AuthorizationPolicies.ReaderAccess, policy =>
        policy.RequireRole(RoleNames.Admin, RoleNames.ComplianceManager, RoleNames.Contributor, RoleNames.Viewer));
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
else
{
    app.UseForwardedHeaders(new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto
    });
}

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<SecurityHeadersMiddleware>();
app.UseCors("AngularSpa");
app.UseHttpsRedirection();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health");

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    if (dbContext.Database.IsRelational())
    {
        await dbContext.Database.MigrateAsync();
    }
    else
    {
        await dbContext.Database.EnsureCreatedAsync();
    }

    await IdentitySeeder.SeedAsync(dbContext, userManager, roleManager, app.Configuration, app.Environment);
}

app.Run();

static string[] GetConfiguredCorsOrigins(IConfiguration configuration, IHostEnvironment environment)
{
    var origins = configuration
        .GetSection("Cors:AllowedOrigins")
        .Get<string[]>() ?? Array.Empty<string>();

    var configuredValue = configuration["Cors:AllowedOrigins"];
    if (!string.IsNullOrWhiteSpace(configuredValue))
    {
        origins = origins
            .Concat(configuredValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .ToArray();
    }

    if (origins.Length == 0 && environment.IsDevelopment())
    {
        origins = new[] { "http://localhost:4200" };
    }

    return origins
        .Where(origin => !string.IsNullOrWhiteSpace(origin))
        .Select(origin => origin.TrimEnd('/'))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .ToArray();
}

public partial class Program
{
}
