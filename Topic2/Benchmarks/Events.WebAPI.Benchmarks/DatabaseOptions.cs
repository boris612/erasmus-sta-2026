namespace Events.WebAPI.Benchmarks;

public sealed class DatabaseOptions
{
  public string PostgreSqlImage { get; set; } = "postgres:18-alpine";
  public string InitScriptsPath { get; set; } = string.Empty;
  public string EnvFilePath { get; set; } = string.Empty;
}
