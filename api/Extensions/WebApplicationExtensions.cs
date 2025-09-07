using RevloDB.Middleware;
using RevloDB.Services;

namespace RevloDB.Extensions
{
    public static class WebApplicationExtensions
    {
        public static async Task<WebApplication> InitializeDatabaseAsync(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();
            var databaseInitializer = scope.ServiceProvider.GetRequiredService<IDatabaseInitializerService>();
            await databaseInitializer.InitializeAsync();
            return app;
        }

        public static WebApplication ConfigureRevloDbPipeline(this WebApplication app)
        {
            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.UseMiddleware<JwtAuthMiddleware>();
            app.UseMiddleware<RoleAuthMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseHttpsRedirection();
            app.UseCors();
            app.UseAuthorization();
            app.MapControllers();

            return app;
        }
    }
}