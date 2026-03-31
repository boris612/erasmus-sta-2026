using BenchmarkDotNet.Attributes;
using Events.WebAPI.Contract.Command;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Contract.Messages;
using Events.WebAPI.Handlers.EF.Models;
using Events.WebAPI.MessageConsumers;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Events.WebAPI.Benchmarks.Question4;

[MemoryDiagnoser]
[ArtifactsPath("Topic2/Benchmarks/results")]
public class Question4ARabbitMqBenchmarks
{
  private RegistrationBenchmarkInfrastructure infrastructure = null!;
  private RegistrationBenchmarkFixture fixture = null!;
  private ServiceProvider rabbitMqProvider = null!;
  private ServiceProvider rabbitMqPublishOnlyProvider = null!;
  private ServiceProvider noBusProvider = null!;
  private RabbitMqBenchmarkContainer rabbitMqContainer = null!;
  private RabbitMqBenchmarkContainer rabbitMqPublishOnlyContainer = null!;
  private IBusControl busControl = null!;
  private IBusControl publishOnlyBusControl = null!;
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

    rabbitMqContainer = new RabbitMqBenchmarkContainer();
    rabbitMqPublishOnlyContainer = new RabbitMqBenchmarkContainer();
    await rabbitMqContainer.StartAsync();
    await rabbitMqPublishOnlyContainer.StartAsync();

    rabbitMqProvider = infrastructure.BuildRabbitMqServiceProvider(
      rabbitMqContainer.Settings,
      includeConsumers: true,
      useSimplifiedRegistrationValidator: true,
      processingOutputRoot: outputRoot);
    rabbitMqPublishOnlyProvider = infrastructure.BuildRabbitMqServiceProvider(
      rabbitMqPublishOnlyContainer.Settings,
      includeConsumers: false,
      useSimplifiedRegistrationValidator: true);
    noBusProvider = infrastructure.BuildServiceProvider(
      useMassTransitHarness: false,
      useInstrumentedServices: true,
      useSimplifiedRegistrationValidator: true,
      processingOutputRoot: outputRoot);
    busControl = rabbitMqProvider.GetRequiredService<IBusControl>();
    publishOnlyBusControl = rabbitMqPublishOnlyProvider.GetRequiredService<IBusControl>();
    await busControl.StartAsync(TimeSpan.FromSeconds(30));
    await publishOnlyBusControl.StartAsync(TimeSpan.FromSeconds(30));

    await CleanupStateAsync();
  }

  [GlobalCleanup]
  public async Task Cleanup()
  {
    try
    {
      if (busControl is not null)
        await busControl.StopAsync(TimeSpan.FromSeconds(30));

      if (publishOnlyBusControl is not null)
        await publishOnlyBusControl.StopAsync(TimeSpan.FromSeconds(30));
    }
    finally
    {
      if (rabbitMqProvider is IAsyncDisposable asyncRabbitMqProvider)
        await asyncRabbitMqProvider.DisposeAsync();
      else
        rabbitMqProvider?.Dispose();

      if (rabbitMqPublishOnlyProvider is IAsyncDisposable asyncRabbitMqPublishOnlyProvider)
        await asyncRabbitMqPublishOnlyProvider.DisposeAsync();
      else
        rabbitMqPublishOnlyProvider?.Dispose();

      if (noBusProvider is IAsyncDisposable asyncNoBusProvider)
        await asyncNoBusProvider.DisposeAsync();
      else
        noBusProvider?.Dispose();

      if (rabbitMqContainer is not null)
        await rabbitMqContainer.DisposeAsync();

      if (rabbitMqPublishOnlyContainer is not null)
        await rabbitMqPublishOnlyContainer.DisposeAsync();
    }
  }

  [Benchmark]
  public async Task<int> MediatR_With_RabbitMq_MessageBus()
  {
    int personId = GetNextPersonId();
    ServiceCompletionMonitor monitor = rabbitMqProvider.GetRequiredService<ServiceCompletionMonitor>();
    monitor.BeginIteration(expectedCertificates: 1, expectedExcels: 1);

    try
    {
      using IServiceScope scope = rabbitMqProvider.CreateScope();
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

  [Benchmark]
  public async Task<int> MediatR_With_RabbitMq_Publish_Only()
  {
    int personId = GetNextPersonId();

    try
    {
      using IServiceScope scope = rabbitMqPublishOnlyProvider.CreateScope();
      IMediator mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
      return await mediator.Send(CreateRegistrationCommand(personId), CancellationToken.None);
    }
    finally
    {
      await CleanupStateAsync();
    }
  }

  [Benchmark]
  public async Task JustPublishMessage()
  {
    int personId = GetNextPersonId();

    using IServiceScope scope = rabbitMqPublishOnlyProvider.CreateScope();
    IPublishEndpoint publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

    await publishEndpoint.Publish(new RegistrationCreated
    {
      RegistrationId = 0,
      PersonId = personId,
      EventId = fixture.ProcessingEventId,
      SportId = fixture.SportId
    }, CancellationToken.None);
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
