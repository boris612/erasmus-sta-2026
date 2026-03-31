using AutoMapper.QueryableExtensions;
using BenchmarkDotNet.Attributes;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Handlers.EF.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Events.WebAPI.Benchmarks.Question1;

[MemoryDiagnoser]
[ArtifactsPath("Topic2/Benchmarks/results")]
public class Question1ProjectionBenchmarks
{
  private PostgreSqlBenchmarkSupport support = null!;
  private ProjectionFixture fixture = null!;

  [Params(20, 100, 1000)]
  public int RowCount { get; set; }

  [GlobalSetup]
  public void Setup()
  {
    support = new PostgreSqlBenchmarkSupport();
    fixture = support.PrepareProjectionFixture(RowCount);
  }

  [GlobalCleanup]
  public void Cleanup()
  {
    support.CleanupProjectionFixture(fixture);
  }

  [Benchmark]
  public async Task<List<RegistrationDTO>> Ef_ProjectTo()
  {
    await using EventsContext context = support.CreateContext();
    return await context.Registrations
      .AsNoTracking()
      .Where(r => r.EventId == fixture.EventId && r.SportId == fixture.SportId && r.RegisteredAt == fixture.Marker)
      .OrderBy(r => r.Id)
      .ProjectTo<RegistrationDTO>(support.MapperConfiguration)
      .Take(RowCount)
      .ToListAsync();
  }

  [Benchmark]
  public async Task<List<RegistrationDTO>> Ef_ManualProjection()
  {
    await using EventsContext context = support.CreateContext();
    return await context.Registrations
      .AsNoTracking()
      .Where(r => r.EventId == fixture.EventId && r.SportId == fixture.SportId && r.RegisteredAt == fixture.Marker)
      .OrderBy(r => r.Id)
      .Select(r => new RegistrationDTO
      {
        Id = r.Id,
        EventId = r.EventId,
        PersonId = r.PersonId,
        SportId = r.SportId,
        RegisteredAt = r.RegisteredAt,
        PersonName = r.Person.FirstName + " " + r.Person.LastName,
        PersonTranscription = r.Person.FirstNameTranscription + " " + r.Person.LastNameTranscription,
        PersonFirstNameTranscription = r.Person.FirstNameTranscription,
        PersonLastNameTranscription = r.Person.LastNameTranscription,
        CountryCode = r.Person.CountryCode,
        CountryName = r.Person.CountryCodeNavigation.Name,
        SportName = r.Sport.Name
      })
      .Take(RowCount)
      .ToListAsync();
  }

  [Benchmark(Baseline = true)]
  public async Task<List<RegistrationDTO>> AdoNet_Projection()
  {
    var result = new List<RegistrationDTO>(RowCount);
    await using NpgsqlConnection connection = support.CreateOpenConnection();
    await using var command = new NpgsqlCommand(
      """
      select r.id,
             r.event_id,
             r.person_id,
             r.sport_id,
             r.registered_at,
             p.first_name || ' ' || p.last_name as person_name,
             p.first_name_transcription || ' ' || p.last_name_transcription as person_transcription,
             p.first_name_transcription,
             p.last_name_transcription,
             p.country_code,
             c.name as country_name,
             s.name as sport_name
      from registration r
      join person p on p.id = r.person_id
      join country c on c.code = p.country_code
      join sport s on s.id = r.sport_id
      where r.event_id = @event_id
        and r.sport_id = @sport_id
        and r.registered_at = @registered_at
      order by r.id
      limit @take;
      """,
      connection);

    command.Parameters.AddWithValue("event_id", fixture.EventId);
    command.Parameters.AddWithValue("sport_id", fixture.SportId);
    command.Parameters.AddWithValue("registered_at", fixture.Marker);
    command.Parameters.AddWithValue("take", RowCount);

    await command.PrepareAsync(); //this has significant impact!

    await using NpgsqlDataReader reader = await command.ExecuteReaderAsync();
    while (await reader.ReadAsync())
    {
      result.Add(new RegistrationDTO
      {
        Id = reader.GetInt32(0),
        EventId = reader.GetInt32(1),
        PersonId = reader.GetInt32(2),
        SportId = reader.GetInt32(3),
        RegisteredAt = reader.GetDateTime(4),
        PersonName = reader.GetString(5),
        PersonTranscription = reader.GetString(6),
        PersonFirstNameTranscription = reader.GetString(7),
        PersonLastNameTranscription = reader.GetString(8),
        CountryCode = reader.GetString(9),
        CountryName = reader.GetString(10),
        SportName = reader.GetString(11)
      });
    }

    return result;
  }
}
