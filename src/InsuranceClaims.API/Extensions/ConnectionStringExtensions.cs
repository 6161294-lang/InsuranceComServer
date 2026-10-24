namespace InsuranceClaims.API.Extensions;

public static class ConnectionStringExtensions
{
    /// <summary>
    /// Npgsql understands "Host=...;Database=..." strings. Hosting platforms such as Render hand out a URL
    /// ("postgres://user:password@host:port/database"), so a URL is converted; anything else is returned unchanged.
    /// </summary>
    public static string ToNpgsqlConnectionString(this string connectionString)
    {
        if (!connectionString.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) &&
            !connectionString.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase))
        {
            return connectionString;
        }

        var uri = new Uri(connectionString);
        var userInfo = uri.UserInfo.Split(':', 2);
        var port = uri.Port > 0 ? uri.Port : 5432;

        return $"Host={uri.Host};Port={port};Database={uri.AbsolutePath.TrimStart('/')};" +
               $"Username={Uri.UnescapeDataString(userInfo[0])};Password={Uri.UnescapeDataString(userInfo.ElementAtOrDefault(1) ?? string.Empty)};" +
               "SSL Mode=Require;Trust Server Certificate=true";
    }
}
