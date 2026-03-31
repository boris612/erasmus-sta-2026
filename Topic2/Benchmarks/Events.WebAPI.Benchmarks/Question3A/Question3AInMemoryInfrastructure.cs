using AutoMapper;
using Events.WebAPI.Contract.Command;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Contract.Validation;
using Events.WebAPI.Contract.Validation.Sport;
using Events.WebAPI.Handlers.EF.CommandHandlers;
using Events.WebAPI.Handlers.EF.Mappings;
using Events.WebAPI.Handlers.EF.Models;
using Events.WebAPI.Util.Validation;
using FluentValidation;
using MassTransit;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Sieve.Services;

namespace Events.WebAPI.Benchmarks.Question3A;

internal sealed class Question3AInMemoryInfrastructure
{
  private const string DatabaseName = "question3a-mediatr-vs-direct";
  private readonly ServiceProvider serviceProvider;
  private readonly MapperConfiguration mapperConfiguration;

  public Question3AInMemoryInfrastructure()
  {
    mapperConfiguration = new MapperConfiguration(cfg => cfg.AddProfile<EFMappingProfile>(), NullLoggerFactory.Instance);

    var services = new ServiceCollection();
    services.AddLogging(builder =>
    {
      builder.AddFilter("Microsoft.EntityFrameworkCore.Database.Command", LogLevel.Warning);
    });
    services.AddDbContext<EventsContext>(db => db.UseInMemoryDatabase(DatabaseName), ServiceLifetime.Scoped);
    services.AddSingleton<IMapper>(mapperConfiguration.CreateMapper());
    services.AddScoped<ISieveProcessor, SieveProcessor>();
    services.AddScoped<IValidationMessageProvider, ValidationMessageProvider>();
    services.AddValidatorsFromAssemblyContaining<AddSportValidator>();
    services.RemoveAll<IValidator<AddCommand<RegistrationDTO, int>>>();
    services.AddScoped<IValidator<AddCommand<RegistrationDTO, int>>, SimplifiedAddRegistrationValidator>();
    services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
    services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(RegistrationsCommandsHandler).Assembly));
    services.AddScoped<RegistrationsCommandsHandler>();
    services.AddSingleton(CreateNoOpPublishEndpoint());
    services.AddScoped<IPublishEndpoint>(sp => sp.GetRequiredService<Mock<IPublishEndpoint>>().Object);

    serviceProvider = services.BuildServiceProvider();
  }

  public ServiceProvider ServiceProvider => serviceProvider;

  public async Task<Question3AFixture> SeedAsync()
  {
    using IServiceScope scope = serviceProvider.CreateScope();
    EventsContext context = scope.ServiceProvider.GetRequiredService<EventsContext>();

    await context.Database.EnsureDeletedAsync();
    await context.Database.EnsureCreatedAsync();

    var country = new Country
    {
      Code = "HRV",
      Alpha3 = "HRV",
      Name = "Croatia"
    };

    var sport = new Sport
    {
      Name = "Basketball"
    };

    var eventEntity = new Handlers.EF.Models.Event
    {
      Name = "Question 3A InMemory Event",
      EventDate = new DateOnly(2026, 5, 1)
    };

    var person = new Person
    {
      FirstName = "Ana",
      LastName = "Horvat",
      FirstNameTranscription = "Ana",
      LastNameTranscription = "Horvat",
      AddressLine = "Benchmark Street 1",
      PostalCode = "10000",
      City = "Zagreb",
      AddressCountry = "Croatia",
      Email = "ana.horvat@example.com",
      ContactPhone = "+3851234567",
      BirthDate = new DateOnly(1990, 1, 1),
      DocumentNumber = "Q3A-001",
      CountryCode = country.Code,
      CountryCodeNavigation = country
    };

    context.Countries.Add(country);
    context.Sports.Add(sport);
    context.Events.Add(eventEntity);
    context.People.Add(person);
    await context.SaveChangesAsync();

    return new Question3AFixture(eventEntity.Id, sport.Id, person.Id);
  }

  public async Task ResetRegistrationsAsync()
  {
    using IServiceScope scope = serviceProvider.CreateScope();
    EventsContext context = scope.ServiceProvider.GetRequiredService<EventsContext>();

    context.Registrations.RemoveRange(context.Registrations);
    await context.SaveChangesAsync();
  }

  public RegistrationDTO CreateRegistrationDto(Question3AFixture fixture)
  {
    return new RegistrationDTO
    {
      EventId = fixture.EventId,
      SportId = fixture.SportId,
      PersonId = fixture.PersonId
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
}

internal sealed record Question3AFixture(int EventId, int SportId, int PersonId);
