using RevloDB.Extensions;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add RevloDB services
builder.Services.AddRevloDbServices(builder.Configuration);
builder.Services.AddRevloDbCors();

var app = builder.Build();

// Initialize database
await app.InitializeDatabaseAsync();

// Configure pipeline
app.ConfigureRevloDbPipeline();

app.Run();