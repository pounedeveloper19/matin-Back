using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using NLog.Extensions.Logging;
using NLog.Web;
using MatinPower.Infrastructure;
using System.Text;

var logger = NLogBuilder.ConfigureNLog("nlog.config").GetLogger("ExceptionLogDatabase");

try
{
    // Prevent thread pool starvation on low-spec hardware (Core i5 3rd gen)
    // Without this, sync DB calls block all threads and new requests queue up
    ThreadPool.SetMinThreads(100, 100);

    var builder = WebApplication.CreateBuilder(args);
    var configuration = builder.Configuration;

    // --------------------------------------------------------
    // Disable Server Header (equivalent of enableVersionHeader="false")
    // --------------------------------------------------------
    builder.WebHost.ConfigureKestrel(options =>
    {
        options.AddServerHeader = false;
    });
    // --------------------------------------------------------

    //builder.Services.AddDbContext<MatinPowerContext>(options =>
    //    options.UseSqlServer(
    //        configuration.GetConnectionString("SqlServerConnection"),
    //        sqlOptions =>
    //        {
    //            sqlOptions.CommandTimeout(60);
    //            sqlOptions.EnableRetryOnFailure(
    //                maxRetryCount: 3,
    //                maxRetryDelay: TimeSpan.FromSeconds(5),
    //                errorNumbersToAdd: null);
    //        }));

    // Response compression to reduce bandwidth
    builder.Services.AddResponseCompression(options =>
    {
        options.EnableForHttps = true;
    });

    // File storage database (separate from main DB)
    builder.Services.AddDbContext<MatinPower.Server.Models.FileStorage.FileDbContext>(options =>
        options.UseSqlServer(configuration.GetConnectionString("FileDbConnection")));

    // Allow multipart upload up to 50 MB
    builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o =>
    {
        o.MultipartBodyLengthLimit = 50 * 1024 * 1024;
    });
    builder.WebHost.ConfigureKestrel(o =>
    {
        o.Limits.MaxRequestBodySize = 50 * 1024 * 1024;
    });

    // Add services to the container.
    builder.Services.AddControllers(options =>
    {
        // EF entities are used directly as API models; navigation properties
        // are non-nullable reference types but arrive null in JSON payloads.
        // Without this, ModelState.IsValid returns false for every entity with
        // a required navigation property (e.g. Contract.Status, Contract.Subscription).
        options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
    });
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title = "MatinPower API",
            Version = "v1",
            Description = "API for Ticket Management System"
        });
        
        // Add JWT Bearer token support for Swagger UI
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "JWT Authorization header using the Bearer scheme. Enter 'Bearer' [space] and then your token in the text input below.",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey,
            Scheme = "Bearer"
        });

        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference
                    {
                        Type = ReferenceType.SecurityScheme,
                        Id = "Bearer"
                    }
                },
                Array.Empty<string>()
            }
        });
    });

    // Credit Sync Service
    builder.Services.AddHttpContextAccessor();

    builder.Services.AddLogging(loggingBuilder =>
    {
        loggingBuilder.ClearProviders();
        loggingBuilder.AddNLog();
    });

    builder.Host.UseNLog();

    // JWT secret must match TokenManager.security constant
    var jwtSecret = configuration["Jwt:Secret"]
        ?? "401b09eab3c013d4ca54922bb802bec8fd5318192b0a75f201d8b3727429090fb337591abd3e44453b954555b7a0812e1081c39b740293f765eae731f5a65ed1";
    var signingKey = new SymmetricSecurityKey(Encoding.Default.GetBytes(jwtSecret));

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = signingKey,
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateLifetime = false,
        };
    });

    var app = builder.Build();

    // Initialize DbContextProvider with connection string (cached for better performance)
    // Do this asynchronously to avoid blocking startup
    var connectionString = configuration.GetConnectionString("SqlServerConnection");
    try
    {
        DbContextProvider.Initialize(app.Services, connectionString!);
    }
    catch (Exception ex)
    {
        var startupLogger = app.Services.GetRequiredService<ILogger<Program>>();
        startupLogger.LogWarning(ex, "Failed to initialize DbContextProvider during startup.");
    }

    app.UseResponseCompression();
    app.UseStaticFiles();


    // Configure the HTTP request pipeline.
    // Swagger enabled in all environments for API testing
    {
        app.UseSwagger();
        app.UseSwaggerUI(c =>
        {
            c.SwaggerEndpoint("/swagger/v1/swagger.json", "MatinPower API V1");
            c.RoutePrefix = "swagger";
            c.DisplayRequestDuration();
        });
    }

    app.UseCors(options =>
    {
        options
        .AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod();
    });

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();
    app.MapFallbackToFile("index.html");
    app.Run();
}
catch (Exception exception)
{
    logger.Error(exception, "Stopped program because of exception");
    throw;
}
finally
{
    NLog.LogManager.Shutdown();
}
