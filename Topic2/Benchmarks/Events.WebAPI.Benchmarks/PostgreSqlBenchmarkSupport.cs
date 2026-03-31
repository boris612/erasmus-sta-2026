using AutoMapper;
using Events.WebAPI.Handlers.EF.Mappings;
using Events.WebAPI.Handlers.EF.Models;
using Events.WebAPI.Benchmarks.Question1;
using Events.WebAPI.Benchmarks.Question2;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Npgsql;

namespace Events.WebAPI.Benchmarks;

internal sealed class PostgreSqlBenchmarkSupport
{
  private const string InsertProcedureName = "benchmark_insert_registration_proc";
  private static readonly string ConfigurationBasePath = ResolveConfigurationBasePath();
  private static readonly Lazy<IConfigurationRoot> LazyConfiguration = new(BuildConfiguration);
  private readonly string adminConnectionString;
  private readonly string connectionString;
  private readonly MapperConfiguration mapperConfiguration;

  public PostgreSqlBenchmarkSupport()
  {
    DatabaseOptions databaseOptions = LoadDatabaseOptions();
    string repositoryRoot = LoadRepositoryRoot();
    adminConnectionString = BenchmarkDatabase.GetAdminConnectionString(databaseOptions, repositoryRoot);
    connectionString = BenchmarkDatabase.GetConnectionString(databaseOptions, repositoryRoot);
    mapperConfiguration = new MapperConfiguration(cfg => cfg.AddProfile<EFMappingProfile>(), NullLoggerFactory.Instance);
  }

  public static IConfigurationRoot LoadConfiguration()
  {
    return LazyConfiguration.Value;
  }

  public static DatabaseOptions LoadDatabaseOptions()
  {
    return LoadConfiguration().GetSection("Database").Get<DatabaseOptions>() ?? new();
  }

  public static string LoadRepositoryRoot()
  {
    string configuredRoot = LoadConfiguration().GetValue<string>("RepositoryRoot") ?? string.Empty;
    return BenchmarkPathResolver.ResolveRepositoryRoot(configuredRoot, ConfigurationBasePath);
  }

  public static async Task<string> LoadConnectionStringAsync()
  {
    return await BenchmarkDatabase.GetConnectionStringAsync(LoadDatabaseOptions(), LoadRepositoryRoot());
  }

  public IMapper CreateMapper()
  {
    return mapperConfiguration.CreateMapper();
  }

  public MapperConfiguration MapperConfiguration => mapperConfiguration;

  public EventsContext CreateContext()
  {
    var dbOptions = new DbContextOptionsBuilder<EventsContext>()
      .UseNpgsql(connectionString)
      .Options;

    return new EventsContext(dbOptions);
  }

  public NpgsqlConnection CreateOpenConnection()
  {
    var connection = new NpgsqlConnection(connectionString);
    connection.Open();
    return connection;
  }

  public ProjectionFixture PrepareProjectionFixture(int rowCount)
  {
    using var connection = CreateOpenConnection();

    int eventId = ExecuteScalar<int>(connection, "select id from event order by id limit 1;");
    int sportId = ExecuteScalar<int>(connection, "select id from sport order by id limit 1;");
    int[] personIds = ExecutePersonIds(connection, rowCount);
    DateTime marker = new(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

    CleanupProjectionRows(connection, eventId, sportId, personIds, marker);

    using var insertCommand = new NpgsqlCommand(
      """
      insert into registration (person_id, sport_id, event_id, registered_at)
      select unnest(@person_ids), @sport_id, @event_id, @registered_at;
      """,
      connection);

    insertCommand.Parameters.AddWithValue("person_ids", personIds);
    insertCommand.Parameters.AddWithValue("sport_id", sportId);
    insertCommand.Parameters.AddWithValue("event_id", eventId);
    insertCommand.Parameters.AddWithValue("registered_at", marker);
    insertCommand.ExecuteNonQuery();

    return new ProjectionFixture(eventId, sportId, personIds, marker);
  }

  public void CleanupProjectionFixture(ProjectionFixture fixture)
  {
    using var connection = CreateOpenConnection();
    CleanupProjectionRows(connection, fixture.EventId, fixture.SportId, fixture.PersonIds, fixture.Marker);
  }

  public InsertFixture PrepareInsertFixture(int rowCount)
  {
    using var connection = CreateOpenConnection();

    int eventId = ExecuteScalar<int>(connection, "select id from event order by id limit 1;");
    int sportId = ExecuteScalar<int>(connection, "select id from sport order by id limit 1;");
    int[] personIds = ExecutePersonIds(connection, rowCount);
    DateTime marker = new(2000, 1, 2, 0, 0, 0, DateTimeKind.Unspecified);
    CleanupInsertRows(connection, eventId, sportId, personIds, marker);
    EnsureInsertProcedure();

    return new InsertFixture(
      eventId,
      sportId,
      personIds,
      marker,
      InsertProcedureName);
  }

  public void CleanupInsertFixture(InsertFixture fixture)
  {
    using var connection = CreateOpenConnection();
    CleanupInsertRows(connection, fixture.EventId, fixture.SportId, fixture.PersonIds, fixture.Marker);
  }

  public void CleanupRegistrations(int eventId, int sportId, IReadOnlyCollection<int> personIds, DateTime marker)
  {
    using var connection = CreateOpenConnection();
    CleanupInsertRows(connection, eventId, sportId, personIds, marker);
  }

  private void EnsureInsertProcedure()
  {
    string sql =
      $"""
      create or replace procedure {InsertProcedureName}(
        in p_person_id integer,
        in p_sport_id integer,
        in p_event_id integer,
        in p_registered_at timestamp without time zone,
        inout p_registration_id integer default null)
      language plpgsql
      as $$
      begin
        insert into registration (person_id, sport_id, event_id, registered_at)
        values (p_person_id, p_sport_id, p_event_id, p_registered_at)
        returning id into p_registration_id;
      end;
      $$;
      """;

    using var connection = new NpgsqlConnection(adminConnectionString);
    connection.Open();

    using var command = new NpgsqlCommand(sql, connection);
    command.ExecuteNonQuery();
  }

  private static void CleanupProjectionRows(NpgsqlConnection connection, int eventId, int sportId, IReadOnlyCollection<int> personIds, DateTime marker)
  {
    using var deleteCommand = new NpgsqlCommand(
      """
      delete from registration
      where event_id = @event_id
        and sport_id = @sport_id
        and registered_at = @registered_at
        and person_id = any(@person_ids);
      """,
      connection);

    deleteCommand.Parameters.AddWithValue("event_id", eventId);
    deleteCommand.Parameters.AddWithValue("sport_id", sportId);
    deleteCommand.Parameters.AddWithValue("registered_at", marker);
    deleteCommand.Parameters.AddWithValue("person_ids", personIds.ToArray());
    deleteCommand.ExecuteNonQuery();
  }

  private static void CleanupInsertRows(NpgsqlConnection connection, int eventId, int sportId, IReadOnlyCollection<int> personIds, DateTime marker)
  {
    using var deleteCommand = new NpgsqlCommand(
      """
      delete from registration
      where event_id = @event_id
        and sport_id = @sport_id
        and registered_at = @registered_at
        and person_id = any(@person_ids);
      """,
      connection);

    deleteCommand.Parameters.AddWithValue("event_id", eventId);
    deleteCommand.Parameters.AddWithValue("sport_id", sportId);
    deleteCommand.Parameters.AddWithValue("registered_at", marker);
    deleteCommand.Parameters.AddWithValue("person_ids", personIds.ToArray());
    deleteCommand.ExecuteNonQuery();
  }

  private static int[] ExecutePersonIds(NpgsqlConnection connection, int rowCount)
  {
    using var command = new NpgsqlCommand("select id from person order by id limit @take;", connection);
    command.Parameters.AddWithValue("take", rowCount);

    var result = new List<int>(rowCount);
    using NpgsqlDataReader reader = command.ExecuteReader();
    while (reader.Read())
      result.Add(reader.GetInt32(0));

    if (result.Count < rowCount)
      throw new InvalidOperationException($"Projection benchmark requires at least {rowCount} rows in person table.");

    return result.ToArray();
  }

  private static T ExecuteScalar<T>(NpgsqlConnection connection, string sql)
  {
    using var command = new NpgsqlCommand(sql, connection);
    object? result = command.ExecuteScalar();
    if (result is null or DBNull)
      throw new InvalidOperationException($"Expected a scalar result for SQL: {sql}");

    return (T)Convert.ChangeType(result, typeof(T), System.Globalization.CultureInfo.InvariantCulture);
  }

  private static IConfigurationRoot BuildConfiguration()
  {
    return new ConfigurationBuilder()
      .SetBasePath(ConfigurationBasePath)
      .AddJsonFile("appsettings.json", optional: false)
      .Build();
  }

  private static string ResolveConfigurationBasePath()
  {
    string currentPath = AppContext.BaseDirectory;

    while (true)
    {
      if (File.Exists(Path.Combine(currentPath, "Events.WebAPI.Benchmarks.csproj")))
        return currentPath;

      DirectoryInfo? parent = Directory.GetParent(currentPath);
      if (parent is null)
        return AppContext.BaseDirectory;

      currentPath = parent.FullName;
    }
  }
}
