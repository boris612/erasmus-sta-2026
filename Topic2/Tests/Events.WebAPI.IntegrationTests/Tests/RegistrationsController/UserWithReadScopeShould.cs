using System.Net;
using System.Net.Http.Json;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Handlers.EF.Models;
using Microsoft.Extensions.DependencyInjection;
using Tests.ApplicationFactories.TestAuth;
using Tests.Helpers;

namespace Tests.RegistrationsController.Given;

public class UserWithReadScopeShould : IClassFixture<TestAuthWebApplicationFactory>
{
  private readonly TestAuthWebApplicationFactory factory;
  private readonly HttpClient client;

  public UserWithReadScopeShould(TestAuthWebApplicationFactory factory)
  {
    this.factory = factory;
    factory.ResetDatabase();
    SeedRegistrations(factory.Services);
    client = factory.CreateClient().WithScopes("events:read");
  }

  [Fact]
  public async Task ReturnFilteredItems()
  {
    HttpResponseMessage response = await client.GetAsync("/Registrations?filters=SportName@=*row");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    Items<RegistrationDTO>? payload = await response.Content.ReadFromJsonAsync<Items<RegistrationDTO>>();
    Assert.NotNull(payload);
    Assert.Equal(1, payload!.Count);
    Assert.Single(payload.Data!);
    Assert.Equal("Rowing", payload.Data![0].SportName);
  }

  [Fact]
  public async Task ReturnSingleItem()
  {
    int registrationId = TestEntityLookup.GetRegistrationId(factory.Services, "Summer Games", "DOC-200", "Rowing");
    HttpResponseMessage response = await client.GetAsync($"/Registrations/{registrationId}");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    RegistrationDTO? payload = await response.Content.ReadFromJsonAsync<RegistrationDTO>();
    Assert.NotNull(payload);
    Assert.Equal(registrationId, payload!.Id);
    Assert.Equal(TestEntityLookup.GetEventId(factory.Services, "Summer Games"), payload.EventId);
  }

  [Fact]
  public async Task GenerateAndReturnCertificateWhenMissing()
  {
    int registrationId = TestEntityLookup.GetRegistrationId(factory.Services, "Summer Games", "DOC-200", "Rowing");

    HttpResponseMessage response = await client.GetAsync($"/Registrations/{registrationId}/Certificate");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("application/pdf", response.Content.Headers.ContentType?.MediaType);
    Assert.NotNull(response.Content.Headers.ContentDisposition?.FileName);
  }

  [Fact]
  public async Task Return403ForCreate()
  {
    HttpResponseMessage response = await client.PostAsJsonAsync("/Registrations", new RegistrationDTO
    {
      EventId = TestEntityLookup.GetEventId(factory.Services, "Summer Games"),
      PersonId = TestEntityLookup.GetPersonId(factory.Services, "DOC-200"),
      SportId = TestEntityLookup.GetSportId(factory.Services, "Swimming")
    });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  private static void SeedRegistrations(IServiceProvider services)
  {
    using IServiceScope scope = services.CreateScope();
    EventsContext ctx = scope.ServiceProvider.GetRequiredService<EventsContext>();

    if (!ctx.Events.Any(e => e.Name == "Summer Games"))
      ctx.Events.Add(new Event { Name = "Summer Games", EventDate = new DateOnly(2026, 6, 15) });
    if (!ctx.Sports.Any(s => s.Name == "Rowing"))
      ctx.Sports.Add(new Sport { Name = "Rowing" });
    if (!ctx.People.Any(p => p.DocumentNumber == "DOC-200"))
      ctx.People.Add(CreatePersonEntity("Iva", "Ivic", "DOC-200"));
    ctx.SaveChanges();

    if (!ctx.Registrations.Any())
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
  private static Person CreatePersonEntity(string firstName, string lastName, string documentNumber)
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
      CountryCode = "HR"
    };
  }
}
