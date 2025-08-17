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

            services.AddDbContext<RevloDbContext>((serviceProvider, options) =>
            {
                var connectionString = configuration.GetConnectionString("DefaultConnection");
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
    }
}