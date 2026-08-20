using UrlShortener.Database;
using Microsoft.Extensions.Configuration;
using System.Reflection;

var configuration = new ConfigurationBuilder()
.AddUserSecrets(Assembly.GetExecutingAssembly())
.AddEnvironmentVariables()
.Build();

var connectionString = configuration.GetConnectionString("Default") 
?? throw new InvalidOperationException("Connection string 'Default' is not configured.");


var success = Migrator.Run(connectionString);
if (!success)
{
    Environment.Exit(1);
}