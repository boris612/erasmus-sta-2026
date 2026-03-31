using Npgsql;
using Testcontainers.PostgreSql;

namespace Events.WebAPI.Benchmarks;

internal static class BenchmarkDatabase
{
  private static readonly SemaphoreSlim Sync = new(1, 1);
  private static PostgreSqlContainer? container;
  private static string? resolvedConnectionString;
  private static string? resolvedAdminConnectionString;
  private static bool shutdownHookRegistered;

  public static string GetConnectionString(DatabaseOptions options, string repositoryRoot)
  {
    return GetConnectionStringAsync(options, repositoryRoot).GetAwaiter().GetResult();
  }

  public static async Task<string> GetConnectionStringAsync(DatabaseOptions options, string repositoryRoot)
  {
    if (!string.IsNullOrWhiteSpace(resolvedConnectionString))
      return resolvedConnectionString;

    await Sync.WaitAsync();
    try
    {
      if (!string.IsNullOrWhiteSpace(resolvedConnectionString))
        return resolvedConnectionString;

      string initScriptsPath = ResolveInitScriptsPath(options.InitScriptsPath, repositoryRoot);
      var envValues = LoadEnvironmentFile(options.EnvFilePath, repositoryRoot);
      string adminUsername = GetRequiredValue(envValues, "POSTGRES_USER");
      string adminPassword = GetRequiredValue(envValues, "POSTGRES_PASSWORD");
      string databaseName = GetRequiredValue(envValues, "POSTGRES_DB");
      string appUsername = GetRequiredValue(envValues, "APP_DB_USER");
      string appPassword = GetRequiredValue(envValues, "APP_DB_PASSWORD");

      container = new PostgreSqlBuilder()
        .WithImage(options.PostgreSqlImage)
        .WithDatabase(databaseName)
        .WithUsername(adminUsername)
        .WithPassword(adminPassword)
        .WithBindMount(initScriptsPath, "/docker-entrypoint-initdb.d")
        .WithEnvironment("APP_DB_USER", appUsername)
        .WithEnvironment("APP_DB_PASSWORD", appPassword)
        .Build();

      await container.StartAsync();

      string containerConnectionString = container.GetConnectionString();
      resolvedAdminConnectionString = new NpgsqlConnectionStringBuilder(containerConnectionString)
      {
        Username = adminUsername,
        Password = adminPassword,
        Database = databaseName
      }.ConnectionString;

      var connectionStringBuilder = new NpgsqlConnectionStringBuilder(containerConnectionString)
      {
        Username = appUsername,
        Password = appPassword,
        Database = databaseName
      };

      resolvedConnectionString = connectionStringBuilder.ConnectionString;
      RegisterShutdownHook();
      return resolvedConnectionString;
    }
    finally
    {
      Sync.Release();
    }
  }

  public static string GetAdminConnectionString(DatabaseOptions options, string repositoryRoot)
  {
    return GetAdminConnectionStringAsync(options, repositoryRoot).GetAwaiter().GetResult();
  }

  public static async Task<string> GetAdminConnectionStringAsync(DatabaseOptions options, string repositoryRoot)
  {
    if (!string.IsNullOrWhiteSpace(resolvedAdminConnectionString))
      return resolvedAdminConnectionString;

    await GetConnectionStringAsync(options, repositoryRoot);
    return resolvedAdminConnectionString
      ?? throw new InvalidOperationException("Admin connection string was not initialized.");
  }

  private static string ResolveInitScriptsPath(string configuredPath, string repositoryRoot)
  {
    string candidate = BenchmarkPathResolver.ResolvePath(repositoryRoot, configuredPath, "Database:InitScriptsPath");

    if (Directory.Exists(candidate))
      return candidate;

    throw new DirectoryNotFoundException($"Configured Database:InitScriptsPath does not exist: {candidate}");
  }

  private static Dictionary<string, string> LoadEnvironmentFile(string configuredPath, string repositoryRoot)
  {
    string envFilePath = ResolveEnvFilePath(configuredPath, repositoryRoot);
    var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    foreach (string rawLine in File.ReadAllLines(envFilePath))
    {
      string line = rawLine.Trim();
      if (line.Length == 0 || line.StartsWith('#'))
        continue;

      int separatorIndex = line.IndexOf('=');
      if (separatorIndex <= 0)
        continue;

      string key = line[..separatorIndex].Trim();
      string value = line[(separatorIndex + 1)..].Trim();
      values[key] = value;
    }

    return values;
  }

  private static string ResolveEnvFilePath(string configuredPath, string repositoryRoot)
  {
    string candidate = BenchmarkPathResolver.ResolvePath(repositoryRoot, configuredPath, "Database:EnvFilePath");

    if (File.Exists(candidate))
      return candidate;

    throw new FileNotFoundException($"Configured Database:EnvFilePath does not exist: {candidate}", candidate);
  }

  private static string GetRequiredValue(IReadOnlyDictionary<string, string> values, string key)
  {
    if (values.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value))
      return value;

    throw new InvalidOperationException($"Required key '{key}' was not found in Database:EnvFilePath.");
  }

  private static void RegisterShutdownHook()
  {
    if (shutdownHookRegistered)
      return;

    shutdownHookRegistered = true;
    AppDomain.CurrentDomain.ProcessExit += (_, _) => StopContainer();
  }

  private static void StopContainer()
  {
    if (container is null)
      return;

    try
    {
      container.DisposeAsync().AsTask().GetAwaiter().GetResult();
    }
    catch
    {
      // Best effort shutdown only.
    }
  }
}
