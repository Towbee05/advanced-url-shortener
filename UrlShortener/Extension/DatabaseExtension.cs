using DbUp;
using System.Data;
using System.Reflection;

namespace UrlShortener.Extenstion;

public static class DatabaseExtension
{
    public static IHost MigrateDatabase<TContext>(this IHost host)
    {
        using (var scope = host.Services.CreateScope())
        {
            var services = scope.ServiceProvider();
            var configuration = services.GetRequiredService<IConfiguration>();
            var logger = services.GetRequiredService<ILogger<TContext>>();

            logger.LogInformation("Migrating Database ...");
            string connectionString = configuration.GetConnectionString("Default");

            EnsureDatabase.For.PostgresqlDatabase(connectionString);
            
            var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogToConsole()
            .Build();

            var result = upgrader.PerformUpgrade();
            if (!result.Successful)
            {
                logger.LogError(result.Error, "An error occurred while migrating the postresql database");
                return host;
            }

            logger.LogInformation("Migrated postresql database.");
        }
        return host;
    }
}