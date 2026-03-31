using System.Net;
using System.Net.Http.Json;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Events.WebAPI.Contract.DTOs;
using Microsoft.Extensions.Logging.Abstractions;

namespace Events.WebAPI.Benchmarks.Question2A;

[MemoryDiagnoser]
[ArtifactsPath("Topic2/Benchmarks/results")]
public class Question2AHttpInsertBenchmarks
{
  private const int RequiredPeople = 50000;
  private static readonly DateTime Marker = new(2000, 1, 4, 0, 0, 0, DateTimeKind.Unspecified);

  private RegistrationBenchmarkInfrastructure infrastructure = null!;
  private RegistrationBenchmarkFixture fixture = null!;
  private PostgreSqlBenchmarkSupport support = null!;
  private Question2ABenchmarkWebApplicationFactory factory = null!;
  private HttpClient client = null!;
  private int nextPersonIndex;

  [GlobalSetup]
  public async Task Setup()
  {
    string connectionString = await PostgreSqlBenchmarkSupport.LoadConnectionStringAsync();
    infrastructure = new RegistrationBenchmarkInfrastructure(connectionString, NullLoggerFactory.Instance);
    fixture = await infrastructure.EnsureFixtureAsync(RequiredPeople);
    support = new PostgreSqlBenchmarkSupport();
    support.CleanupRegistrations(fixture.DispatchEventId, fixture.SportId, fixture.PersonIds, Marker);

    factory = new Question2ABenchmarkWebApplicationFactory(connectionString);
    client = factory.CreateClient();
    nextPersonIndex = 0;
  }

  [GlobalCleanup]
  public void Cleanup()
  {
    support.CleanupRegistrations(fixture.DispatchEventId, fixture.SportId, fixture.PersonIds, Marker);
    client.Dispose();
    factory.Dispose();
  }

  [Benchmark]
  public async Task<HttpStatusCode> Post_Registration_With_EF_AutoMapper()
  {
    using HttpResponseMessage response = await client.PostAsJsonAsync(
      "/_benchmarks/registrations/ef-automapper",
      CreateDto());
    response.EnsureSuccessStatusCode();
    return response.StatusCode;
  }

  [Benchmark]
  public async Task<HttpStatusCode> Post_Registration_With_EF_AutoMapper_NoRollback()
  {
    using HttpResponseMessage response = await client.PostAsJsonAsync(
      "/_benchmarks/registrations/ef-automapper-no-rollback",
      CreateDto());
    response.EnsureSuccessStatusCode();
    return response.StatusCode;
  }

  [Benchmark(Baseline = true)]
  public async Task<HttpStatusCode> Post_Registration_With_AdoNet()
  {
    using HttpResponseMessage response = await client.PostAsJsonAsync(
      "/_benchmarks/registrations/adonet",
      CreateDto());
    response.EnsureSuccessStatusCode();
    return response.StatusCode;
  }

  [Benchmark]
  public async Task<HttpStatusCode> Post_Registration_With_AdoNet_NoRollback()
  {
    using HttpResponseMessage response = await client.PostAsJsonAsync(
      "/_benchmarks/registrations/adonet-no-rollback",
      CreateDto());
    response.EnsureSuccessStatusCode();
    return response.StatusCode;
  }

  private RegistrationDTO CreateDto()
  {
    int index = Interlocked.Increment(ref nextPersonIndex) - 1;
    if (index >= fixture.PersonIds.Count)
      throw new InvalidOperationException($"Question2A requires more than {fixture.PersonIds.Count} unique people for the benchmark run.");

    return new RegistrationDTO
    {
      EventId = fixture.DispatchEventId,
      SportId = fixture.SportId,
      PersonId = fixture.PersonIds[index],
      RegisteredAt = Marker
    };
  }
}
