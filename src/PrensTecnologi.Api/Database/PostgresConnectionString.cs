using Npgsql;

namespace PrensTecnologi.Api.Database;

public static class PostgresConnectionString
{
    /// <summary>
    /// Acepta la cadena en formato Npgsql (Host=...;Database=...) o en formato URL
    /// (postgresql://usuario:clave@host:puerto/base), que es el que entregan Render y otros proveedores.
    /// </summary>
    public static string? Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) ||
            !(value.StartsWith("postgres://", StringComparison.OrdinalIgnoreCase) ||
              value.StartsWith("postgresql://", StringComparison.OrdinalIgnoreCase)))
        {
            return value;
        }

        var uri = new Uri(value);
        var credentials = uri.UserInfo.Split(':', 2);

        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.IsDefaultPort || uri.Port <= 0 ? 5432 : uri.Port,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : null,
        };

        // Parámetros de la URL, por ejemplo ?sslmode=require.
        foreach (var pair in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = pair.Split('=', 2);
            if (parts.Length == 2)
            {
                builder[Uri.UnescapeDataString(parts[0])] = Uri.UnescapeDataString(parts[1]);
            }
        }

        return builder.ConnectionString;
    }
}
