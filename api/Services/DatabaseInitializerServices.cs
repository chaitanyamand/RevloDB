using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Npgsql;
using RevloDB.Configuration;
using RevloDB.Data;

namespace RevloDB.Services
{

    public interface IDatabaseInitializerService
    {
        Task InitializeAsync();
    }

    public class DatabaseInitializerService : IDatabaseInitializerService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;
        private readonly ILogger<DatabaseInitializerService> _logger;
        private readonly DatabaseOptions _databaseOptions;

        public DatabaseInitializerService(
            IServiceProvider serviceProvider,
            IConfiguration configuration,
            ILogger<DatabaseInitializerService> logger,
            IOptions<DatabaseOptions> databaseOptions)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
            _logger = logger;
            _databaseOptions = databaseOptions.Value;
        }

        public async Task InitializeAsync()
        {
            if (!_databaseOptions.AutoMigrate)
            {
                _logger.LogInformation("Auto-migration is disabled. Skipping database initialization.");
                return;
            }

            var connectionString = _configuration.GetConnectionString("DefaultConnection")!;
            var retryCount = 0;
            var maxRetries = _databaseOptions.RetryOnFailure ? _databaseOptions.MaxRetryCount : 1;

            while (retryCount < maxRetries)
            {
                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    await InitializeDatabaseInternalAsync(connectionString, scope);
                    return; // Success, exit retry loop
                }
                catch (Exception ex) when (retryCount < maxRetries - 1 && _databaseOptions.RetryOnFailure)
                {
                    retryCount++;
                    _logger.LogWarning(ex,
                        "Database initialization failed (attempt {Attempt}/{MaxAttempts}). Retrying in {Delay} seconds...",
                        retryCount, maxRetries, _databaseOptions.RetryDelay);

                    await Task.Delay(TimeSpan.FromSeconds(_databaseOptions.RetryDelay));
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Database initialization failed after {Attempts} attempts", retryCount + 1);
                    throw;
                }
            }
        }

        private async Task InitializeDatabaseInternalAsync(string connectionString, IServiceScope scope)
        {
            var connectionStringBuilder = new NpgsqlConnectionStringBuilder(connectionString);
            var databaseName = string.IsNullOrWhiteSpace(connectionStringBuilder.Database) ? "revlodb" : connectionStringBuilder.Database;
            var masterBuilder = new NpgsqlConnectionStringBuilder(connectionStringBuilder.ConnectionString)
            {
                Database = "postgres"
            };
            var masterConnectionString = masterBuilder.ConnectionString;

            _logger.LogInformation("Checking if database '{DatabaseName}' exists...", databaseName);

            if (_databaseOptions.CreateDatabaseIfNotExists)
            {
                await EnsureDatabaseExistsAsync(masterConnectionString, databaseName);
            }

            var context = scope.ServiceProvider.GetRequiredService<RevloDbContext>();

            _logger.LogInformation("Starting database migration...");

            context.Database.SetCommandTimeout(TimeSpan.FromSeconds(_databaseOptions.CommandTimeout));

            await context.Database.MigrateAsync();

            _logger.LogInformation("Database migration completed successfully!");
        }

        private async Task EnsureDatabaseExistsAsync(string masterConnectionString, string databaseName)
        {
            if (string.IsNullOrWhiteSpace(databaseName) || databaseName.Contains(';') || databaseName.Contains('\''))
            {
                var ex = new ArgumentException($"Invalid database name: {databaseName}");
                _logger.LogError(ex, "Database name validation failed.");
                throw ex;
            }

            try
            {
                using var connection = new NpgsqlConnection(masterConnectionString);
                await connection.OpenAsync();

                using var command = new NpgsqlCommand("SELECT 1 FROM pg_database WHERE datname = @name", connection);
                command.Parameters.AddWithValue("name", databaseName);
                command.CommandTimeout = _databaseOptions.CommandTimeout;
                var exists = await command.ExecuteScalarAsync();

                if (exists == null)
                {
                    _logger.LogInformation("Database '{DatabaseName}' does not exist. Creating it...", databaseName);

                    var builder = new NpgsqlCommandBuilder();
                    string quotedDatabaseName = builder.QuoteIdentifier(databaseName);

                    using var createCommand = new NpgsqlCommand($"CREATE DATABASE {quotedDatabaseName}", connection);
                    createCommand.CommandTimeout = _databaseOptions.CommandTimeout;
                    await createCommand.ExecuteNonQueryAsync();

                    _logger.LogInformation("Database '{DatabaseName}' created successfully!", databaseName);
                }
                else
                {
                    _logger.LogInformation("Database '{DatabaseName}' already exists.", databaseName);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking/creating database '{DatabaseName}'", databaseName);
                throw;
            }
        }
    }
}