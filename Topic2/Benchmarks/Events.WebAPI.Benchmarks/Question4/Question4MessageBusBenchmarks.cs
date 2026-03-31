using BenchmarkDotNet.Attributes;
using Events.WebAPI.Contract.Command;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Contract.Messages;
using Events.WebAPI.Handlers.EF.Models;
using Events.WebAPI.MessageConsumers;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Events.WebAPI.Benchmarks.Question4;

[MemoryDiagnoser]
[ArtifactsPath("Topic2/Benchmarks/results")]
public class Question4MessageBusBenchmarks
{
  private RegistrationBenchmarkInfrastructure infrastructure = null!;
  private RegistrationBenchmarkFixture fixture = null!;
  private ServiceProvider busProvider = null!;
  private ServiceProvider noBusProvider = null!;
  private ITestHarness harness = null!;
  private int nextPersonIndex;
  private string outputRoot = null!;

  [GlobalSetup]
  public async Task Setup()
  {
    string connectionString = await PostgreSqlBenchmarkSupport.LoadConnectionStringAsync();
    IConfiguration configuration = PostgreSqlBenchmarkSupport.LoadConfiguration();
    string repositoryRoot = PostgreSqlBenchmarkSupport.LoadRepositoryRoot();
    int personPoolSize = configuration.GetValue<int?>("Question4:PersonPoolSize") ?? 128;
    string outputRootSetting = configuration.GetValue<string>("Question4:OutputRoot") ?? "Topic2/.artifacts/perf";
    outputRoot = BenchmarkPathResolver.ResolvePath(repositoryRoot, outputRootSetting, "Question4:OutputRoot");

    infrastructure = new RegistrationBenchmarkInfrastructure(connectionString, NullLoggerFactory.Instance);
    fixture = await infrastructure.EnsureFixtureAsync(Math.Max(personPoolSize, 32));
    busProvider = infrastructure.BuildServiceProvider(
      useMassTransitHarness: true,
      useSimplifiedRegistrationValidator: true,
      processingOutputRoot: outputRoot);
    noBusProvider = infrastructure.BuildServiceProvider(
      useMassTransitHarness: false,
      useInstrumentedServices: true,
      useSimplifiedRegistrationValidator: true,
      processingOutputRoot: outputRoot);
    harness = busProvider.GetRequiredService<ITestHarness>();
    await harness.Start();

    await CleanupStateAsync();
  }

  [GlobalCleanup]
  public async Task Cleanup()
  {
    try
    {
      if (harness is not null)
        await harness.Stop();
    }
    finally
    {
      if (busProvider is IAsyncDisposable asyncBusProvider)
        await asyncBusProvider.DisposeAsync();
      else
        busProvider?.Dispose();

      if (noBusProvider is IAsyncDisposable asyncNoBusProvider)
        await asyncNoBusProvider.DisposeAsync();
      else
        noBusProvider?.Dispose();
    }
  }

  [Benchmark]
  public async Task<int> MediatR_With_MessageBus()
  {
    int personId = GetNextPersonId();
    ServiceCompletionMonitor monitor = busProvider.GetRequiredService<ServiceCompletionMonitor>();
    monitor.BeginIteration(expectedCertificates: 1, expectedExcels: 1);

    try
    {
      using IServiceScope scope = busProvider.CreateScope();
      IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
      int registrationId = await mediator.Send(CreateRegistrationCommand(personId), CancellationToken.None);
      await monitor.WaitForCompletionAsync(TimeSpan.FromSeconds(30));
      return registrationId;
    }
    finally
    {
      await CleanupStateAsync();
    }
  }

  [Benchmark(Baseline = true)]
  public async Task<int> MediatR_Without_MessageBus()
  {
    int personId = GetNextPersonId();

    try
    {
      int registrationId;
      using (IServiceScope scope = noBusProvider.CreateScope())
      {
        IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        registrationId = await mediator.Send(CreateRegistrationCommand(personId), CancellationToken.None);
      }

      var message = new RegistrationCreated
      {
        RegistrationId = registrationId,
        PersonId = personId,
        EventId = fixture.ProcessingEventId,
        SportId = fixture.SportId
      };

      Task notificationsTask = ConsumeNotificationsDirectlyAsync(message);
      Task excelTask = ConsumeExcelDirectlyAsync(message);
      await Task.WhenAll(notificationsTask, excelTask);
      return registrationId;
    }
    finally
    {
      await CleanupStateAsync();
    }
  }

  private AddCommand<RegistrationDTO, int> CreateRegistrationCommand(int personId)
  {
    return new AddCommand<RegistrationDTO, int>(
      infrastructure.CreateRegistrationDto(fixture.ProcessingEventId, fixture.SportId, personId));
  }

  private int GetNextPersonId()
  {
    int index = Interlocked.Increment(ref nextPersonIndex);
    return fixture.PersonIds[index % fixture.PersonIds.Count];
  }

  private async Task CleanupStateAsync()
  {
    if (Directory.Exists(outputRoot))
      Directory.Delete(outputRoot, recursive: true);

    Directory.CreateDirectory(outputRoot);

    await using EventsContext context = infrastructure.CreateContext();
    await context.Registrations
      .Where(r => r.EventId == fixture.ProcessingEventId)
      .ExecuteDeleteAsync();
  }

  private static ConsumeContext<RegistrationCreated> CreateConsumeContext(RegistrationCreated message)
  {
    var consumeContext = new Mock<ConsumeContext<RegistrationCreated>>(MockBehavior.Loose);
    consumeContext.SetupGet(x => x.Message).Returns(message);
    consumeContext.SetupGet(x => x.CancellationToken).Returns(CancellationToken.None);
    return consumeContext.Object;
  }

  private async Task ConsumeNotificationsDirectlyAsync(RegistrationCreated message)
  {
    using IServiceScope scope = noBusProvider.CreateScope();
    var consumer = new RegistrationNotificationsConsumer(
      scope.ServiceProvider.GetRequiredService<Events.WebAPI.Contract.Services.Certificates.IRegistrationCertificateService>());

    await consumer.Consume(CreateConsumeContext(message));
  }

  private async Task ConsumeExcelDirectlyAsync(RegistrationCreated message)
  {
    using IServiceScope scope = noBusProvider.CreateScope();
    var consumer = new EventRegistrationsExcelConsumer(
      scope.ServiceProvider.GetRequiredService<Events.WebAPI.Contract.Services.EventRegistrationsExcel.IEventRegistrationsExcelService>());

    await consumer.Consume(CreateConsumeContext(message));
  }
}
