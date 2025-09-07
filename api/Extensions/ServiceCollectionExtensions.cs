using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RevloDB.Configuration;
using RevloDB.Data;
using RevloDB.Repositories;
using RevloDB.Repositories.Interfaces;
using RevloDB.Services;
using RevloDB.Services.Interfaces;


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

            services
                .AddOptions<AuthOptions>()
                .Bind(configuration.GetSection(AuthOptions.SectionName))
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

            // Register repositories
            services.AddScoped<IKeyRepository, KeyRepository>();
            services.AddScoped<IVersionRepository, VersionRepository>();
            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<INamespaceRepository, NamespaceRepository>();
            services.AddScoped<IAPIKeyRepository, APIKeyRepository>();
            services.AddScoped<IUserNamespaceRepository, UserNamespaceRepository>();
            services.AddScoped<ICleanupRepository, CleanupRepository>();

            // Register services
            services.AddScoped<ICleanupService, CleanupService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IAPIKeyService, APIKeyService>();
            services.AddScoped<INamespaceService, NamespaceService>();
            services.AddScoped<IUserNamespaceService, UserNamespaceService>();
            services.AddScoped<IKeyValueService, KeyValueService>();
            services.AddScoped<IUserAuthService, UserAuthService>();

            services.AddScoped<IDatabaseInitializerService, DatabaseInitializerService>();

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
            services.AddAutoMapper(typeof(Program));
            return services;
        }
    }
}