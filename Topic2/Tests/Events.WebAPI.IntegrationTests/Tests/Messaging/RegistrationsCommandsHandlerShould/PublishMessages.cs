using AutoMapper;
using Events.WebAPI.Contract.Command;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Contract.Messages;
using Events.WebAPI.Handlers.EF.CommandHandlers;
using Events.WebAPI.Handlers.EF.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using MobilityOne.Common.Commands;
using Moq;
using Tests.ApplicationFactories.TestAuth;
using Tests.Helpers;

namespace Tests.Messaging.RegistrationsCommandsHandlerShould;

public class PublishMessages : IClassFixture<TestAuthWebApplicationFactory>
{
  private readonly TestAuthWebApplicationFactory factory;

  public PublishMessages(TestAuthWebApplicationFactory factory)
  {
    this.factory = factory;
    factory.ResetDatabase();
  }

  [Fact]
  public async Task PublishRegistrationCreatedAfterAdd()
  {
    using IServiceScope scope = factory.Services.CreateScope();
    EventsContext ctx = scope.ServiceProvider.GetRequiredService<EventsContext>();
    SeedRegistrationDependencies(ctx);
    int eventId = TestEntityLookup.GetEventId(ctx, "Summer Games");
    int personId = TestEntityLookup.GetPersonId(ctx, "DOC-200");
    int sportId = TestEntityLookup.GetSportId(ctx, "Rowing");

    var publishEndpoint = new Mock<IPublishEndpoint>();
    publishEndpoint
      .Setup(endpoint => endpoint.Publish(It.IsAny<RegistrationCreated>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var handler = CreateHandler(scope.ServiceProvider, ctx, publishEndpoint);
    var dto = new RegistrationDTO
    {
      EventId = eventId,
      PersonId = personId,
      SportId = sportId
    };

    int id = await handler.Handle(new AddCommand<RegistrationDTO, int>(dto), CancellationToken.None);

    Assert.True(id > 0);
    Assert.Contains(ctx.Registrations, registration => registration.Id == id);
    publishEndpoint.Verify(endpoint => endpoint.Publish(
      It.Is<RegistrationCreated>(message =>
        message.RegistrationId == id &&
        message.EventId == dto.EventId &&
        message.PersonId == dto.PersonId &&
        message.SportId == dto.SportId),
      It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task PublishRegistrationUpdatedAfterUpdate()
  {
    using IServiceScope scope = factory.Services.CreateScope();
    EventsContext ctx = scope.ServiceProvider.GetRequiredService<EventsContext>();
    SeedRegistrationDependencies(ctx);

    ctx.Events.Add(new Events.WebAPI.Handlers.EF.Models.Event { Name = "Winter Games", EventDate = new DateOnly(2026, 2, 10) });
    ctx.Sports.Add(new Sport { Name = "Cycling" });
    ctx.People.Add(CreatePersonEntity("Ana", "Ani", "HR", "DOC-201"));
    await ctx.SaveChangesAsync();

    var existingRegistration = new Registration
    {
      EventId = TestEntityLookup.GetEventId(ctx, "Summer Games"),
      PersonId = TestEntityLookup.GetPersonId(ctx, "DOC-200"),
      SportId = TestEntityLookup.GetSportId(ctx, "Rowing"),
      RegisteredAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Unspecified)
    };
    ctx.Registrations.Add(existingRegistration);
    await ctx.SaveChangesAsync();
    int previousEventId = existingRegistration.EventId;
    int previousPersonId = existingRegistration.PersonId;
    int previousSportId = existingRegistration.SportId;

    var publishEndpoint = new Mock<IPublishEndpoint>();
    publishEndpoint
      .Setup(endpoint => endpoint.Publish(It.IsAny<RegistrationUpdated>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var handler = CreateHandler(scope.ServiceProvider, ctx, publishEndpoint);
    var dto = new RegistrationDTO
    {
      Id = existingRegistration.Id,
      EventId = TestEntityLookup.GetEventId(ctx, "Winter Games"),
      PersonId = TestEntityLookup.GetPersonId(ctx, "DOC-201"),
      SportId = TestEntityLookup.GetSportId(ctx, "Cycling")
    };

    await handler.Handle(new UpdateCommand<RegistrationDTO>(dto), CancellationToken.None);

    Registration updated = await ctx.Registrations.SingleAsync(registration => registration.Id == existingRegistration.Id);
    Assert.Equal(dto.EventId, updated.EventId);
    Assert.Equal(dto.PersonId, updated.PersonId);
    Assert.Equal(dto.SportId, updated.SportId);

    publishEndpoint.Verify(endpoint => endpoint.Publish(
      It.Is<RegistrationUpdated>(message =>
        message.RegistrationId == dto.Id &&
        message.EventId == dto.EventId &&
        message.PersonId == dto.PersonId &&
        message.SportId == dto.SportId &&
        message.PreviousEventId == previousEventId &&
        message.PreviousPersonId == previousPersonId &&
        message.PreviousSportId == previousSportId),
      It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task PublishRegistrationDeletedAfterDelete()
  {
    using IServiceScope scope = factory.Services.CreateScope();
    EventsContext ctx = scope.ServiceProvider.GetRequiredService<EventsContext>();
    SeedRegistrationDependencies(ctx);

    var existingRegistration = new Registration
    {
      EventId = TestEntityLookup.GetEventId(ctx, "Summer Games"),
      PersonId = TestEntityLookup.GetPersonId(ctx, "DOC-200"),
      SportId = TestEntityLookup.GetSportId(ctx, "Rowing"),
      RegisteredAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Unspecified)
    };
    ctx.Registrations.Add(existingRegistration);
    await ctx.SaveChangesAsync();

    var publishEndpoint = new Mock<IPublishEndpoint>();
    publishEndpoint
      .Setup(endpoint => endpoint.Publish(It.IsAny<RegistrationDeleted>(), It.IsAny<CancellationToken>()))
      .Returns(Task.CompletedTask);

    var handler = CreateHandler(scope.ServiceProvider, ctx, publishEndpoint);

    await handler.Handle(new DeleteCommand<RegistrationDTO, int>(existingRegistration.Id), CancellationToken.None);

    ctx.ChangeTracker.Clear();
    Assert.False(await ctx.Registrations.AsNoTracking().AnyAsync(registration => registration.Id == existingRegistration.Id));
    publishEndpoint.Verify(endpoint => endpoint.Publish(
      It.Is<RegistrationDeleted>(message =>
        message.RegistrationId == existingRegistration.Id &&
        message.EventId == existingRegistration.EventId &&
        message.PersonId == existingRegistration.PersonId &&
        message.SportId == existingRegistration.SportId),
      It.IsAny<CancellationToken>()), Times.Once);
  }

  private static RegistrationsCommandsHandler CreateHandler(IServiceProvider services, EventsContext ctx, Mock<IPublishEndpoint> publishEndpoint)
  {
    IMapper mapper = services.GetRequiredService<IMapper>();

    return new RegistrationsCommandsHandler(
      ctx,
      NullLogger<RegistrationsCommandsHandler>.Instance,
      mapper,
      publishEndpoint.Object);
  }

  private static void SeedRegistrationDependencies(EventsContext ctx)
  {
    if (!ctx.Countries.Any(item => item.Code == "HR"))
      ctx.Countries.Add(new Country { Code = "HR", Name = "Croatia", Alpha3 = "HRV" });

    if (!ctx.Events.Any(item => item.Name == "Summer Games"))
      ctx.Events.Add(new Events.WebAPI.Handlers.EF.Models.Event { Name = "Summer Games", EventDate = new DateOnly(2026, 6, 15) });

    if (!ctx.Sports.Any(item => item.Name == "Rowing"))
      ctx.Sports.Add(new Sport { Name = "Rowing" });

    if (!ctx.People.Any(item => item.DocumentNumber == "DOC-200"))
      ctx.People.Add(CreatePersonEntity("Iva", "Ivic", "HR", "DOC-200"));

    ctx.SaveChanges();
  }

  private static Person CreatePersonEntity(string firstName, string lastName, string countryCode, string documentNumber)
  {
    return new Person
    {
      FirstName = firstName,
      LastName = lastName,
      FirstNameTranscription = firstName,
      LastNameTranscription = lastName,
      AddressLine = "Street 1",
      PostalCode = "10000",
      City = "Zagreb",
      AddressCountry = "Croatia",
      Email = $"{firstName.ToLowerInvariant()}@example.com",
      ContactPhone = "+385123456",
      BirthDate = new DateOnly(1990, 1, 1),
      DocumentNumber = documentNumber,
      CountryCode = countryCode
    };
  }
}
