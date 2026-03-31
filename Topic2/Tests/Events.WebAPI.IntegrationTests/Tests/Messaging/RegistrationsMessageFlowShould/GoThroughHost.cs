using System.Net;
using System.Net.Http.Json;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Contract.Messages;
using Events.WebAPI.Handlers.EF.Models;
using Tests.ApplicationFactories.Messaging;
using Events.WebAPI.MessageConsumers;
using MassTransit.Testing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Tests.Helpers;

namespace Tests.Messaging.RegistrationsMessageFlowShould;

public class GoThroughHost : IAsyncLifetime
{
  private MessagingWebApplicationFactory factory = null!;
  private HttpClient client = null!;
  private ITestHarness harness = null!;
  private IConsumerTestHarness<RegistrationNotificationsConsumer> certificateConsumerHarness = null!;
  private IConsumerTestHarness<EventRegistrationsExcelConsumer> excelConsumerHarness = null!;

  public async Task InitializeAsync()
  {
    // Each test needs its own host and MassTransit harness instance; sharing them
    // across tests makes message assertions depend on the execution of previous tests.
    factory = new MessagingWebApplicationFactory();
    await factory.InitializeAsync();
    factory.ResetDatabase();
    factory.ResetServiceMocks();
    client = factory.CreateClient().WithScopes("events:read", "events:write");
    harness = factory.Services.GetRequiredService<ITestHarness>();
    certificateConsumerHarness = harness.GetConsumerHarness<RegistrationNotificationsConsumer>();
    excelConsumerHarness = harness.GetConsumerHarness<EventRegistrationsExcelConsumer>();
    SeedRegistrationDependencies(factory.Services);
    await harness.Start();
  }

  public async Task DisposeAsync()
  {
    await harness.Stop();
    await ((IAsyncLifetime)factory).DisposeAsync();
  }

  [Fact]
  public async Task PublishCreatedMessageAndInvokeBothConsumersAfterAdd()
  {
    int eventId = TestEntityLookup.GetEventId(factory.Services, "Summer Games");
    int personId = TestEntityLookup.GetPersonId(factory.Services, "DOC-200");
    int sportId = TestEntityLookup.GetSportId(factory.Services, "Rowing");
    var dto = new RegistrationDTO
    {
      EventId = eventId,
      PersonId = personId,
      SportId = sportId
    };

    HttpResponseMessage response = await client.PostAsJsonAsync("/Registrations", dto);

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    Assert.True(
      await harness.Published.Any<RegistrationCreated>(),
      "Expected RegistrationCreated to be published to the MassTransit test harness after POST /Registrations.");
    Assert.True(
      await harness.Consumed.Any<RegistrationCreated>(),
      "Expected RegistrationCreated to be consumed by registered consumers after POST /Registrations.");
    Assert.True(
      await certificateConsumerHarness.Consumed.Any<RegistrationCreated>(),
      "Expected RegistrationCreated to be consumed by RegistrationNotificationsConsumer after POST /Registrations.");
    Assert.True(
      await excelConsumerHarness.Consumed.Any<RegistrationCreated>(),
      "Expected RegistrationCreated to be consumed by EventRegistrationsExcelConsumer after POST /Registrations.");

    factory.CertificateServiceMock.Verify(x => x.SynchronizeCertificateAsync(eventId, personId, It.IsAny<CancellationToken>()), Times.Once);
    factory.ExcelServiceMock.Verify(x => x.SynchronizeAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task PublishUpdatedMessageAndInvokeCurrentAndPreviousSynchronizationsAfterUpdate()
  {
    SeedUpdatedRegistrationScenario(factory.Services);
    int registrationId = TestEntityLookup.GetRegistrationId(factory.Services, "Summer Games", "DOC-200", "Rowing");
    int previousEventId = TestEntityLookup.GetEventId(factory.Services, "Summer Games");
    int previousPersonId = TestEntityLookup.GetPersonId(factory.Services, "DOC-200");
    int currentEventId = TestEntityLookup.GetEventId(factory.Services, "Winter Games");
    int currentPersonId = TestEntityLookup.GetPersonId(factory.Services, "DOC-201");
    int currentSportId = TestEntityLookup.GetSportId(factory.Services, "Cycling");

    var dto = new RegistrationDTO
    {
      Id = registrationId,
      EventId = currentEventId,
      PersonId = currentPersonId,
      SportId = currentSportId
    };

    HttpResponseMessage response = await client.PutAsJsonAsync($"/Registrations/{registrationId}", dto);

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.True(
      await harness.Published.Any<RegistrationUpdated>(),
      "Expected RegistrationUpdated to be published to the MassTransit test harness after PUT /Registrations/{id}.");
    Assert.True(
      await harness.Consumed.Any<RegistrationUpdated>(),
      "Expected RegistrationUpdated to be consumed by registered consumers after PUT /Registrations/{id}.");
    Assert.True(
      await certificateConsumerHarness.Consumed.Any<RegistrationUpdated>(),
      "Expected RegistrationUpdated to be consumed by RegistrationNotificationsConsumer after PUT /Registrations/{id}.");
    Assert.True(
      await excelConsumerHarness.Consumed.Any<RegistrationUpdated>(),
      "Expected RegistrationUpdated to be consumed by EventRegistrationsExcelConsumer after PUT /Registrations/{id}.");

    factory.CertificateServiceMock.Verify(x => x.SynchronizeCertificateAsync(currentEventId, currentPersonId, It.IsAny<CancellationToken>()), Times.Once);
    factory.CertificateServiceMock.Verify(x => x.SynchronizeCertificateAsync(previousEventId, previousPersonId, It.IsAny<CancellationToken>()), Times.Once);
    factory.ExcelServiceMock.Verify(x => x.SynchronizeAsync(currentEventId, It.IsAny<CancellationToken>()), Times.Once);
    factory.ExcelServiceMock.Verify(x => x.SynchronizeAsync(previousEventId, It.IsAny<CancellationToken>()), Times.Once);
  }

  [Fact]
  public async Task PublishDeletedMessageAndInvokeBothConsumersAfterDelete()
  {
    SeedDeletedRegistrationScenario(factory.Services);
    int registrationId = TestEntityLookup.GetRegistrationId(factory.Services, "Summer Games", "DOC-200", "Rowing");
    int eventId = TestEntityLookup.GetEventId(factory.Services, "Summer Games");
    int personId = TestEntityLookup.GetPersonId(factory.Services, "DOC-200");

    HttpResponseMessage response = await client.DeleteAsync($"/Registrations/{registrationId}");

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    Assert.True(
      await harness.Published.Any<RegistrationDeleted>(),
      "Expected RegistrationDeleted to be published to the MassTransit test harness after DELETE /Registrations/{id}.");
    Assert.True(
      await harness.Consumed.Any<RegistrationDeleted>(),
      "Expected RegistrationDeleted to be consumed by registered consumers after DELETE /Registrations/{id}.");
    Assert.True(
      await certificateConsumerHarness.Consumed.Any<RegistrationDeleted>(),
      "Expected RegistrationDeleted to be consumed by RegistrationNotificationsConsumer after DELETE /Registrations/{id}.");
    Assert.True(
      await excelConsumerHarness.Consumed.Any<RegistrationDeleted>(),
      "Expected RegistrationDeleted to be consumed by EventRegistrationsExcelConsumer after DELETE /Registrations/{id}.");

    factory.CertificateServiceMock.Verify(x => x.SynchronizeCertificateAsync(eventId, personId, It.IsAny<CancellationToken>()), Times.Once);
    factory.ExcelServiceMock.Verify(x => x.SynchronizeAsync(eventId, It.IsAny<CancellationToken>()), Times.Once);
  }

  private static void SeedRegistrationDependencies(IServiceProvider services)
  {
    using IServiceScope scope = services.CreateScope();
    EventsContext ctx = scope.ServiceProvider.GetRequiredService<EventsContext>();

    if (!ctx.Countries.Any(c => c.Code == "HR"))
      ctx.Countries.Add(new Country { Code = "HR", Name = "Croatia", Alpha3 = "HRV" });
    if (!ctx.Events.Any(e => e.Name == "Summer Games"))
      ctx.Events.Add(new Event { Name = "Summer Games", EventDate = new DateOnly(2026, 6, 15) });
    if (!ctx.Sports.Any(s => s.Name == "Rowing"))
      ctx.Sports.Add(new Sport { Name = "Rowing" });
    if (!ctx.People.Any(p => p.DocumentNumber == "DOC-200"))
      ctx.People.Add(CreatePersonEntity("Iva", "Ivic", "HR", "DOC-200"));

    ctx.SaveChanges();
  }

  private static void SeedUpdatedRegistrationScenario(IServiceProvider services)
  {
    using IServiceScope scope = services.CreateScope();
    EventsContext ctx = scope.ServiceProvider.GetRequiredService<EventsContext>();

    if (!ctx.Events.Any(e => e.Name == "Winter Games"))
      ctx.Events.Add(new Event { Name = "Winter Games", EventDate = new DateOnly(2026, 2, 10) });
    if (!ctx.Sports.Any(s => s.Name == "Cycling"))
      ctx.Sports.Add(new Sport { Name = "Cycling" });
    if (!ctx.People.Any(p => p.DocumentNumber == "DOC-201"))
      ctx.People.Add(CreatePersonEntity("Ana", "Ani", "HR", "DOC-201"));

    ctx.SaveChanges();

    if (!ctx.Registrations.Any(r =>
          r.Event.Name == "Summer Games" &&
          r.Person.DocumentNumber == "DOC-200" &&
          r.Sport.Name == "Rowing"))
    {
      ctx.Registrations.Add(new Registration
      {
        EventId = ctx.Events.Single(e => e.Name == "Summer Games").Id,
        PersonId = ctx.People.Single(p => p.DocumentNumber == "DOC-200").Id,
        SportId = ctx.Sports.Single(s => s.Name == "Rowing").Id,
        RegisteredAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Unspecified)
      });
    }

    ctx.SaveChanges();
  }

  private static void SeedDeletedRegistrationScenario(IServiceProvider services)
  {
    using IServiceScope scope = services.CreateScope();
    EventsContext ctx = scope.ServiceProvider.GetRequiredService<EventsContext>();

    if (!ctx.Registrations.Any(r =>
          r.Event.Name == "Summer Games" &&
          r.Person.DocumentNumber == "DOC-200" &&
          r.Sport.Name == "Rowing"))
    {
      ctx.Registrations.Add(new Registration
      {
        EventId = ctx.Events.Single(e => e.Name == "Summer Games").Id,
        PersonId = ctx.People.Single(p => p.DocumentNumber == "DOC-200").Id,
        SportId = ctx.Sports.Single(s => s.Name == "Rowing").Id,
        RegisteredAt = new DateTime(2026, 3, 1, 10, 0, 0, DateTimeKind.Unspecified)
      });
      ctx.SaveChanges();
    }
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
