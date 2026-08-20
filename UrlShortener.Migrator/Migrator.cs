using DbUp;
using System.Reflection;

namespace UrlShortener.Database;

public static class Migrator
{
    // connection string added via CLI by running export ConnectionStrings__Default=value
    // 
    // 
    public static bool Run(string connectionString)
    {
        EnsureDatabase.For.PostgresqlDatabase(connectionString);

        var upgrader = DeployChanges.To
            .PostgresqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(Assembly.GetExecutingAssembly())
            .LogToConsole()
            .Build();

        var result = upgrader.PerformUpgrade();

        if (!result.Successful)
        {
            Console.Error.WriteLine(result.Error);
            return false;
        }
        Console.WriteLine("Successfully migrated the database");
        return true;
    }
}