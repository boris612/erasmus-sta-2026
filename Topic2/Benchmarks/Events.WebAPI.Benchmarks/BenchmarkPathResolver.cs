namespace Events.WebAPI.Benchmarks;

internal static class BenchmarkPathResolver
{
  public static string ResolveRepositoryRoot(string configuredRoot, string configurationBasePath)
  {
    if (string.IsNullOrWhiteSpace(configuredRoot))
      throw new InvalidOperationException("RepositoryRoot must be set in appsettings.json.");

    string expandedRoot = Environment.ExpandEnvironmentVariables(configuredRoot.Trim());
    string candidate = Path.IsPathRooted(expandedRoot)
      ? Path.GetFullPath(expandedRoot)
      : Path.GetFullPath(Path.Combine(configurationBasePath, expandedRoot));

    if (Directory.Exists(candidate))
      return candidate;

    throw new DirectoryNotFoundException($"Configured RepositoryRoot does not exist: {candidate}");
  }

  public static string ResolvePath(string repositoryRoot, string configuredPath, string settingPath)
  {
    if (string.IsNullOrWhiteSpace(configuredPath))
      throw new InvalidOperationException($"{settingPath} must be set in appsettings.json.");

    string expandedPath = Environment.ExpandEnvironmentVariables(configuredPath.Trim());
    return Path.IsPathRooted(expandedPath)
      ? Path.GetFullPath(expandedPath)
      : Path.GetFullPath(Path.Combine(repositoryRoot, expandedPath));
  }
}
