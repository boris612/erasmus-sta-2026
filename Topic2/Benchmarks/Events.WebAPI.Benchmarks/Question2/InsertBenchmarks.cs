using AutoMapper;
using BenchmarkDotNet.Attributes;
using EFCore.BulkExtensions;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Handlers.EF.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;

namespace Events.WebAPI.Benchmarks.Question2;

[MemoryDiagnoser]
[ArtifactsPath("Topic2/Benchmarks/results")]
public class Question2InsertBenchmarks
{
  private const int MaxInsertCount = 1000;
  private PostgreSqlBenchmarkSupport support = null!;
  private InsertFixture fixture = null!;
  private IMapper mapper = null!;

  [Params(1, 10, 1000)]
  public int InsertCount { get; set; }

  [GlobalSetup]
  public void Setup()
  {
    support = new PostgreSqlBenchmarkSupport();
    fixture = support.PrepareInsertFixture(MaxInsertCount);
    mapper = support.CreateMapper();
  }

  [GlobalCleanup]
  public void Cleanup()
  {
    support.CleanupInsertFixture(fixture);
  }

  [Benchmark]
  public async Task<int> AutoMapper_To_EF_Insert()
  {
    int insertedCount = 0;
    await using EventsContext context = support.CreateContext();
    await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

    for (int i = 0; i < InsertCount; i++)
    {
      Registration entity = mapper.Map<Registration>(new RegistrationDTO
      {
        EventId = fixture.EventId,
        SportId = fixture.SportId,
        PersonId = fixture.PersonIds[i]
      });
      entity.RegisteredAt = fixture.Marker;
      context.Registrations.Add(entity);
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();
      insertedCount++;
    }

    await transaction.RollbackAsync();
    return insertedCount;
  }

  [Benchmark]
  public async Task<int> Manual_EF_Insert()
  {
    int insertedCount = 0;
    await using EventsContext context = support.CreateContext();
    await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

    for (int i = 0; i < InsertCount; i++)
    {
      var entity = new Registration
      {
        EventId = fixture.EventId,
        SportId = fixture.SportId,
        PersonId = fixture.PersonIds[i],
        RegisteredAt = fixture.Marker
      };

      context.Registrations.Add(entity);
      await context.SaveChangesAsync();
      context.ChangeTracker.Clear();
      insertedCount++;
    }

    await transaction.RollbackAsync();
    return insertedCount;
  }

  [Benchmark]
  public async Task<int> AddRange_EF_Insert()
  {
    await using EventsContext context = support.CreateContext();
    await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

    var entities = Enumerable.Range(0, InsertCount)
      .Select(i => new Registration
      {
        EventId = fixture.EventId,
        SportId = fixture.SportId,
        PersonId = fixture.PersonIds[i],
        RegisteredAt = fixture.Marker
      })
      .ToList();

    context.Registrations.AddRange(entities);
    int insertedCount = await context.SaveChangesAsync();

    await transaction.RollbackAsync();
    return insertedCount;
  }

  [Benchmark]
  public async Task<int> AdoNet_Insert()
  {
    int insertedCount = 0;
    await using NpgsqlConnection connection = support.CreateOpenConnection();
    await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
    await using var command = new NpgsqlCommand(
      """
      insert into registration (person_id, sport_id, event_id, registered_at)
      values (@person_id, @sport_id, @event_id, @registered_at)
      returning id;
      """,
      connection,
      transaction);

    var personIdParameter = command.Parameters.Add("person_id", NpgsqlTypes.NpgsqlDbType.Integer);
    command.Parameters.AddWithValue("sport_id", fixture.SportId);
    command.Parameters.AddWithValue("event_id", fixture.EventId);
    command.Parameters.AddWithValue("registered_at", fixture.Marker);
    await command.PrepareAsync();

    for (int i = 0; i < InsertCount; i++)
    {
      personIdParameter.Value = fixture.PersonIds[i];

      await command.ExecuteScalarAsync();
      insertedCount++;
    }

    await transaction.RollbackAsync();
    return insertedCount;
  }

  [Benchmark]
  public async Task<int> EfCore_BulkExtensions_Insert()
  {
    await using EventsContext context = support.CreateContext();
    await using IDbContextTransaction transaction = await context.Database.BeginTransactionAsync();

    var entities = Enumerable.Range(0, InsertCount)
      .Select(i => new Registration
      {
        EventId = fixture.EventId,
        SportId = fixture.SportId,
        PersonId = fixture.PersonIds[i],
        RegisteredAt = fixture.Marker
      })
      .ToList();

    await context.BulkInsertAsync(entities);
    await transaction.RollbackAsync();
    return entities.Count;
  }

  [Benchmark]
  public async Task<int> AdoNet_BulkInsert_Unnest()
  {
    await using NpgsqlConnection connection = support.CreateOpenConnection();
    await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
    await using var command = new NpgsqlCommand(
      """
      insert into registration (person_id, sport_id, event_id, registered_at)
      select person_id, @sport_id, @event_id, @registered_at
      from unnest(@person_ids) as input(person_id);
      """,
      connection,
      transaction);

    command.Parameters.AddWithValue("sport_id", fixture.SportId);
    command.Parameters.AddWithValue("event_id", fixture.EventId);
    command.Parameters.AddWithValue("registered_at", fixture.Marker);
    command.Parameters.AddWithValue("person_ids", fixture.PersonIds.Take(InsertCount).ToArray());
    await command.PrepareAsync();

    int insertedCount = await command.ExecuteNonQueryAsync();
    await transaction.RollbackAsync();
    return insertedCount;
  }

  [Benchmark(Baseline = true)]
  public async Task<int> StoredProcedure_Insert()
  {
    int insertedCount = 0;
    await using NpgsqlConnection connection = support.CreateOpenConnection();
    await using NpgsqlTransaction transaction = await connection.BeginTransactionAsync();
    await using var command = new NpgsqlCommand(
      $"call {fixture.ProcedureName}(@person_id, @sport_id, @event_id, @registered_at, @registration_id);",
      connection,
      transaction);

    var personIdParameter = command.Parameters.Add("person_id", NpgsqlTypes.NpgsqlDbType.Integer);
    command.Parameters.AddWithValue("sport_id", fixture.SportId);
    command.Parameters.AddWithValue("event_id", fixture.EventId);
    command.Parameters.AddWithValue("registered_at", fixture.Marker);

    var idParameter = new NpgsqlParameter<int>("registration_id", 0)
    {
      Direction = System.Data.ParameterDirection.InputOutput
    };
    command.Parameters.Add(idParameter);
    await command.PrepareAsync();

    for (int i = 0; i < InsertCount; i++)
    {
      personIdParameter.Value = fixture.PersonIds[i];
      idParameter.TypedValue = 0;
      await command.ExecuteNonQueryAsync();
      insertedCount++;
    }

    await transaction.RollbackAsync();
    return insertedCount;
  }
}
