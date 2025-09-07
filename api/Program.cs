using RevloDB.Extensions;
using RevloDB.Jobs;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add RevloDB services
builder.Services.AddRevloDbServices(builder.Configuration);
builder.Services.AddRevloDbCors();

// Register the background job
builder.Services.AddHostedService<CleanupBackgroundJob>();

// Register AutoMapper profiles
builder.Services.AddMappers();

var app = builder.Build();

// Initialize database
await app.InitializeDatabaseAsync();

// Configure pipeline
app.ConfigureRevloDbPipeline();

app.Run();