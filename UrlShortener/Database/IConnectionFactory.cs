using System.Data;

namespace UrlShortener.Database;

public interface IConnectionFactory
{
    IDbConnection CreateConnection();
}