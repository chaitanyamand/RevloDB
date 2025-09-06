using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RevloDB.Configuration;
using RevloDB.Data;
using RevloDB.Repositories;
using RevloDB.Repositories.Interfaces;
using RevloDB.Services;
using RevloDB.Services.Interfaces;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace RevloDB.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddRevloDbServices(this IServiceCollection services, IConfiguration configuration)
        {
            services
                .AddOptions<DatabaseOptions>()
                .Bind(configuration.GetSection(DatabaseOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services
                .AddOptions<CleanupJobOptions>()
                .Bind(configuration.GetSection(CleanupJobOptions.SectionName))
                .ValidateDataAnnotations()
                .ValidateOnStart();

            services.AddDbContext<RevloDbContext>((serviceProvider, options) =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection")
                    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
                var databaseOptions = serviceProvider.GetRequiredService<IOptions<DatabaseOptions>>().Value;

                options.UseNpgsql(connectionString, npgsqlOptions =>
                {
                    npgsqlOptions.CommandTimeout(databaseOptions.CommandTimeout);
                });

                var environment = serviceProvider.GetRequiredService<IWebHostEnvironment>();
                if (environment.IsDevelopment())
                {
                    options.EnableSensitiveDataLogging();
                    options.EnableDetailedErrors();
                }
            });

            services.AddScoped<IKeyRepository, KeyRepository>();
            services.AddScoped<IVersionRepository, VersionRepository>();

            services.AddScoped<IKeyValueService, KeyValueService>();

            services.AddScoped<IDatabaseInitializerService, DatabaseInitializerService>();
            services.AddScoped<ICleanupRepository, CleanupRepository>();
            services.AddScoped<ICleanupService, CleanupService>();

            return services;
        }

        public static IServiceCollection AddJwtAuth(this IServiceCollection services)
        {
            var secretKey = services.BuildServiceProvider().GetRequiredService<IConfiguration>()["Jwt:Key"] ?? throw new InvalidOperationException("JWT secret key not configured.");
            var key = Encoding.UTF8.GetBytes(secretKey);

            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            services.AddSingleton(tokenValidationParameters);

            return services;
        }

        public static IServiceCollection AddRevloDbCors(this IServiceCollection services)
        {
            services.AddCors(options =>
            {
                options.AddDefaultPolicy(builder =>
                {
                    builder.AllowAnyOrigin()
                           .AllowAnyMethod()
                           .AllowAnyHeader();
                });
            });

            return services;
        }

        public static IServiceCollection AddMappers(this IServiceCollection services)
        {
            services.AddAutoMapper(typeof(UserMappingProfile).Assembly);
            return services;
        }
    }
}