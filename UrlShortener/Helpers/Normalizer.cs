namespace UrlShortener.Helpers;

public class EmailNormalizer
{
    public static string Normalize(string email) =>
        email?.Trim().ToLowerInvariant() ?? string.Empty;
}

public class UsernameNormalizer
{
    public static string Normalize(string username) =>
        username?.Trim().ToLowerInvariant() ?? string.Empty;
}