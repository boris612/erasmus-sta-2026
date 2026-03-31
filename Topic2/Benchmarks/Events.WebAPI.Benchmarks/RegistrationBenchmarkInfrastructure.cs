using AutoMapper;
using Events.WebAPI.CertificateCreator;
using Events.WebAPI.Contract.Command;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Contract.Services.Certificates;
using Events.WebAPI.Contract.Services.EventRegistrationsExcel;
using Events.WebAPI.Contract.Validation;
using Events.WebAPI.Contract.Validation.Sport;
using Events.WebAPI.ExcelExporter;
using Events.WebAPI.Handlers.EF.CommandHandlers;
using Events.WebAPI.Handlers.EF.Mappings;
using Events.WebAPI.Handlers.EF.Models;
using Events.WebAPI.MessageConsumers;
using Events.WebAPI.Util.Settings;
using Events.WebAPI.Util.Validation;
using FluentValidation;
using MassTransit;
using MassTransit.Testing;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Sieve.Services;

namespace Events.WebAPI.Benchmarks;

public sealed class RegistrationBenchmarkInfrastructure
{
  private const string DispatchEventName = "Union Games";
  private const string ProcessingEventName = "City Cup";
  private readonly string connectionString;
  private readonly ILoggerFactory loggerFactory;
  private readonly MapperConfiguration mapperConfiguration;

  public RegistrationBenchmarkInfrastructure(string connectionString, ILoggerFactory loggerFactory)
  {
    this.connectionString = connectionString;
    this.loggerFactory = loggerFactory;
    mapperConfiguration = new MapperConfiguration(cfg => cfg.AddProfile<EFMappingProfile>(), NullLoggerFactory.Instance);
  }

  public EventsContext CreateContext()
  {
    var dbOptions = new DbContextOptionsBuilder<EventsContext>()
      .UseNpgsql(connectionString)
      .Options;

    return new EventsContext(dbOptions);
  }

  public async Task<RegistrationBenchmarkFixture> EnsureFixtureAsync(int requiredPeople)
  {
    await using EventsContext context = CreateContext();
    await EnsurePeopleAsync(context, requiredPeople);

    int sportId = await context.Sports
      .OrderBy(s => s.Id)
      .Select(s => s.Id)
      .FirstAsync();

    int dispatchEventId = await context.Events
      .Where(e => e.Name == DispatchEventName)
      .Select(e => e.Id)
      .FirstAsync();

    int processingEventId = await context.Events
      .Where(e => e.Name == ProcessingEventName)
      .Select(e => e.Id)
      .FirstAsync();

    int[] personIds = await context.People
      .OrderBy(p => p.Id)
      .Take(requiredPeople)
      .Select(p => p.Id)
      .ToArrayAsync();

    if (personIds.Length < requiredPeople)
      throw new InvalidOperationException($"Registration benchmarks require at least {requiredPeople} seeded people in docker-definitions/postgres-eventsdb/init/06-people.sql.");

    return new RegistrationBenchmarkFixture(
      dispatchEventId,
      processingEventId,
      sportId,
      personIds);
  }

  private static async Task EnsurePeopleAsync(EventsContext context, int requiredPeople)
  {
    int existingCount = await context.People.CountAsync();
    if (existingCount >= requiredPeople)
      return;

    int missing = requiredPeople - existingCount;
    var newPeople = Enumerable.Range(existingCount + 1, missing)
      .Select(index => new Person
      {
        FirstName = $"Benchmark{index}",
        LastName = "Person",
        FirstNameTranscription = $"Benchmark{index}",
        LastNameTranscription = "Person",
        AddressLine = "Benchmark Street 1",
        PostalCode = "10000",
        City = "Zagreb",
        AddressCountry = "Croatia",
        Email = $"benchmark{index}@example.com",
        ContactPhone = "+385123456",
        BirthDate = new DateOnly(1990, 1, 1),
        DocumentNumber = $"BENCH-{index:D6}",
        CountryCode = "HR"
      })
      .ToList();

    context.People.AddRange(newPeople);
    await context.SaveChangesAsync();
  }

  public ServiceProvider BuildServiceProvider(
    bool useMassTransitHarness,
    bool useInstrumentedServices = false,
    bool useSimplifiedRegistrationValidator = false,
    string? processingOutputRoot = null)
  {
    ServiceCollection services = CreateBaseServices(
      useInstrumentedServices || useMassTransitHarness,
      useSimplifiedRegistrationValidator,
      processingOutputRoot);

    if (useMassTransitHarness)
    {
      services.AddMassTransitTestHarness(cfg =>
      {
        cfg.AddConsumer<RegistrationNotificationsConsumer>();
        cfg.AddConsumer<EventRegistrationsExcelConsumer>();

        cfg.UsingInMemory((context, busCfg) =>
        {
          busCfg.ConfigureEndpoints(context);
        });
      });
    }
    else
    {
      AddNoOpPublishEndpoint(services);
    }

    return services.BuildServiceProvider();
  }

  public ServiceProvider BuildRabbitMqServiceProvider(
    RabbitMqSettings rabbitMqSettings,
    bool includeConsumers,
    bool useSimplifiedRegistrationValidator = false,
    string? processingOutputRoot = null)
  {
    ServiceCollection services = CreateBaseServices(
      useInstrumentedServices: includeConsumers,
      useSimplifiedRegistrationValidator,
      processingOutputRoot);

    AddRabbitMqMassTransit(services, rabbitMqSettings, includeConsumers);

    return services.BuildServiceProvider();
  }

  public RegistrationDTO CreateRegistrationDto(int eventId, int sportId, int personId)
  {
    return new RegistrationDTO
    {
      EventId = eventId,
      SportId = sportId,
      PersonId = personId
    };
  }

  private static Mock<IPublishEndpoint> CreateNoOpPublishEndpoint()
  {
    var publishEndpoint = new Mock<IPublishEndpoint>(MockBehavior.Loose);

    publishEndpoint
      .Setup(x => x.Publish(It.IsAny<object>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);
    publishEndpoint
      .Setup(x => x.Publish(It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);
    publishEndpoint
      .Setup(x => x.Publish(It.IsAny<object>(), It.IsAny<IPipe<PublishContext>>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);
    publishEndpoint
      .Setup(x => x.Publish(It.IsAny<object>(), It.IsAny<Type>(), It.IsAny<IPipe<PublishContext>>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    publishEndpoint
      .Setup(x => x.Publish<object>(It.IsAny<object>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);
    publishEndpoint
      .Setup(x => x.Publish<object>(It.IsAny<object>(), It.IsAny<IPipe<PublishContext<object>>>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    return publishEndpoint;
  }

  private ServiceCollection CreateBaseServices(
    bool useInstrumentedServices,
    bool useSimplifiedRegistrationValidator,
    string? processingOutputRoot)
  {
    var services = new ServiceCollection();
    services.AddSingleton(loggerFactory);
    services.AddLogging(builder =>
    {
      builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
    });

    services.AddDbContext<EventsContext>(db => db.UseNpgsql(connectionString), ServiceLifetime.Scoped);
    services.AddSingleton<IMapper>(mapperConfiguration.CreateMapper());
    services.AddScoped<ISieveProcessor, SieveProcessor>();
    services.AddScoped<IValidationMessageProvider, ValidationMessageProvider>();
    services.AddValidatorsFromAssemblyContaining<AddSportValidator>();
    if (useSimplifiedRegistrationValidator)
    {
      services.RemoveAll<IValidator<AddCommand<RegistrationDTO, int>>>();
      services.AddScoped<IValidator<AddCommand<RegistrationDTO, int>>, SimplifiedAddRegistrationValidator>();
    }
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RegistrationsCommandsHandler).Assembly));
    services.AddScoped<RegistrationsCommandsHandler>();

    if (useInstrumentedServices)
    {
      string resolvedOutputRoot = ResolveProcessingOutputRoot(processingOutputRoot);
      services.AddSingleton<IHostEnvironment>(new ScenarioHostEnvironment(resolvedOutputRoot));
      services.AddSingleton<IOptions<CertificateCreatorPathsOptions>>(Options.Create(new CertificateCreatorPathsOptions { Certificates = resolvedOutputRoot }));
      services.AddSingleton<IOptions<ExcelExporterPathsOptions>>(Options.Create(new ExcelExporterPathsOptions { Certificates = resolvedOutputRoot }));
      services.AddSingleton<ServiceCompletionMonitor>();
      services.AddScoped<RegistrationCertificateService>();
      services.AddScoped<EventRegistrationsExcelService>();
      services.AddScoped<IRegistrationCertificateService, InstrumentedRegistrationCertificateService>();
      services.AddScoped<IEventRegistrationsExcelService, InstrumentedEventRegistrationsExcelService>();
    }
    else
    {
      services.AddScoped<IRegistrationCertificateService, RegistrationCertificateService>();
      services.AddScoped<IEventRegistrationsExcelService, EventRegistrationsExcelService>();
    }

    return services;
  }

  private static void AddRabbitMqMassTransit(
    IServiceCollection services,
    RabbitMqSettings rabbitMqSettings,
    bool includeConsumers)
  {
    services.AddOptions<RabbitMqSettings>()
      .Configure(options =>
      {
        options.Host = rabbitMqSettings.Host;
        options.Username = rabbitMqSettings.Username;
        options.Password = rabbitMqSettings.Password;
      });

    services.AddMassTransit(x =>
    {
      if (includeConsumers)
      {
        x.AddConsumer<RegistrationNotificationsConsumer>();
        x.AddConsumer<EventRegistrationsExcelConsumer>();
      }

      x.UsingRabbitMq((context, cfg) =>
      {
        cfg.Host(new Uri(rabbitMqSettings.Host), h =>
        {
          h.Username(rabbitMqSettings.Username);
          h.Password(rabbitMqSettings.Password);
        });

        if (includeConsumers)
        {
          cfg.ReceiveEndpoint("events-webapi-registration-changes", e =>
          {
            e.ConfigureConsumer<RegistrationNotificationsConsumer>(context);
            e.ConfigureConsumer<EventRegistrationsExcelConsumer>(context);
          });
        }
      });
    });
  }

  private static void AddNoOpPublishEndpoint(IServiceCollection services)
  {
    services.AddSingleton(CreateNoOpPublishEndpoint());
    services.AddScoped<IPublishEndpoint>(sp => sp.GetRequiredService<Mock<IPublishEndpoint>>().Object);
  }

  private static string ResolveProcessingOutputRoot(string? processingOutputRoot)
  {
    if (string.IsNullOrWhiteSpace(processingOutputRoot))
      throw new InvalidOperationException("A processing output root is required when building processing-related services.");

    Directory.CreateDirectory(processingOutputRoot);
    return processingOutputRoot;
  }
}

public sealed record RegistrationBenchmarkFixture(
  int DispatchEventId,
  int ProcessingEventId,
  int SportId,
  IReadOnlyList<int> PersonIds);

internal sealed class ScenarioHostEnvironment(string contentRootPath) : IHostEnvironment
{
  public string EnvironmentName { get; set; } = Environments.Development;
  public string ApplicationName { get; set; } = nameof(Events.WebAPI.Benchmarks);
  public string ContentRootPath { get; set; } = contentRootPath;
  public IFileProvider ContentRootFileProvider { get; set; } = new PhysicalFileProvider(contentRootPath);
}
