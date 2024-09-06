using System.Text;
using Carhub.Service.Users.Core.Contexts;
using Carhub.Service.Users.Core.DAL;
using Carhub.Service.Users.Core.DAL.Repositories;
using Carhub.Service.Users.Core.ErrorHandling;
using Carhub.Service.Users.Core.Options;
using Carhub.Service.Users.Core.Repositories;
using Carhub.Service.Users.Core.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using Serilog;

namespace Carhub.Service.Users.Core;

public static class Extensions
{
    public static IServiceCollection AddCore(this IServiceCollection services, IConfiguration configuration)
    {
        AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
        return services
            .Configure<AuthOptions>(configuration.GetSection(AuthOptions.OptionsName))
            .AddPostgres<UsersDbContext>()
            .AddTransient<IIdentityService, IdentityService>()
            .AddSingleton<IPasswordManager, PasswordManager>()
            .AddSingleton<ITokenManager, TokenManager>()
            .AddScoped<IUserRepository, UserRepository>()
            .AddSingleton(TimeProvider.System)
            .AddSingleton<IHttpContextAccessor, HttpContextAccessor>()
            .AddTransient<IContextFactory, ContextFactory>()
            .AddTransient<IContext>(sp => sp.GetRequiredService<IContextFactory>().Create())
            .AddAuth()
            .AddErrorHandling();
    }

    public static void AddLogging(this WebApplicationBuilder builder)
    {
        var logFilePath = $"logs/log_users_{DateTime.UtcNow:yyyy-MM-dd}.txt";

        builder.Host.UseSerilog((ctx, lc) => lc
            .ReadFrom.Configuration(ctx.Configuration)
            .WriteTo.Console()
            .WriteTo.File(logFilePath, rollingInterval: RollingInterval.Infinite));
    }

    public static WebApplication UseCore(this WebApplication app)
    {
        app.UseErrorHandling();

        app.UseAuthentication();

        app.UseAuthorization();

        return app;
    }

    private static IServiceCollection AddAuth(this IServiceCollection services)
    {
        var options = services.GetOptions<AuthOptions>(AuthOptions.OptionsName);

        var tokenValidationParameters = new TokenValidationParameters
        {
            RequireAudience = options.RequireAudience,
            ValidIssuer = options.ValidIssuer,
            ValidIssuers = options.ValidIssuers,
            ValidateActor = options.ValidateActor,
            ValidAudience = options.ValidAudience,
            ValidAudiences = options.ValidAudiences,
            ValidateAudience = options.ValidateAudience,
            ValidateIssuer = options.ValidateIssuer,
            ValidateLifetime = options.ValidateLifetime,
            ValidateTokenReplay = options.ValidateTokenReplay,
            ValidateIssuerSigningKey = options.ValidateIssuerSigningKey,
            SaveSigninToken = options.SaveSigninToken,
            RequireExpirationTime = options.RequireExpirationTime,
            RequireSignedTokens = options.RequireSignedTokens,
            ClockSkew = TimeSpan.Zero
        };

        if (string.IsNullOrWhiteSpace(options.IssuerSigningKey))
            throw new ArgumentException("Missing issuer signing key.", nameof(options.IssuerSigningKey));

        if (!string.IsNullOrWhiteSpace(options.AuthenticationType))
            tokenValidationParameters.AuthenticationType = options.AuthenticationType;

        var rawKey = Encoding.UTF8.GetBytes(options.IssuerSigningKey);
        tokenValidationParameters.IssuerSigningKey = new SymmetricSecurityKey(rawKey);

        if (!string.IsNullOrWhiteSpace(options.NameClaimType))
            tokenValidationParameters.NameClaimType = options.NameClaimType;

        if (!string.IsNullOrWhiteSpace(options.RoleClaimType))
            tokenValidationParameters.RoleClaimType = options.RoleClaimType;

        services
            .AddAuthentication(o =>
            {
                o.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                o.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(o =>
            {
                o.Authority = options.Authority;
                o.Audience = options.Audience;
                o.MetadataAddress = options.MetadataAddress;
                o.SaveToken = o.SaveToken;
                o.RefreshOnIssuerKeyNotFound = options.RefreshOnIssuerKeyNotFound;
                o.RequireHttpsMetadata = options.RequireHttpsMetadata;
                o.IncludeErrorDetails = options.IncludeErrorDetails;
                o.TokenValidationParameters = tokenValidationParameters;
                if (!string.IsNullOrWhiteSpace(options.Challenge))
                    o.Challenge = options.Challenge;
            });

        services.AddSingleton(options);
        services.AddSingleton(tokenValidationParameters);

        foreach (var policy in options.Policies)
            services.AddAuthorizationBuilder()
                .AddPolicy(policy, y => y.RequireClaim("permissions", policy));

        return services;
    }

    private static IServiceCollection AddPostgres<T>(this IServiceCollection services)
        where T : DbContext
    {
        var options = services.GetOptions<PostgresOptions>(PostgresOptions.OptionsName);
        services.AddDbContext<T>(x => x.UseNpgsql(options.ConnectionString));
        return services;
    }

    private static T GetOptions<T>(this IServiceCollection services, string sectionName)
        where T : new()
    {
        using var serviceProvider = services.BuildServiceProvider();
        var configuration = serviceProvider.GetRequiredService<IConfiguration>();
        return configuration.GetOptions<T>(sectionName);
    }

    private static T GetOptions<T>(this IConfiguration configuration, string sectionName)
        where T : new()
    {
        var options = new T();
        configuration.GetSection(sectionName).Bind(options);
        return options;
    }
}