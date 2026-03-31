using System.Net;
using System.Net.Http.Json;
using System.Text;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Handlers.EF.Models;
using Microsoft.Extensions.DependencyInjection;
using Tests.ApplicationFactories.TestAuth;
using Tests.Helpers;

namespace Tests.RegistrationsController.Given;

public class UserWithWriteScopeShould : IClassFixture<TestAuthWebApplicationFactory>
{
  private readonly TestAuthWebApplicationFactory factory;
  private readonly HttpClient client;

  public UserWithWriteScopeShould(TestAuthWebApplicationFactory factory)
  {
    this.factory = factory;
    factory.ResetDatabase();
    SeedRegistrations(factory.Services);
    client = factory.CreateClient().WithScopes("events:read", "events:write");
  }

  [Fact]
  public async Task CreateRegistration()
  {
    HttpResponseMessage response = await client.PostAsJsonAsync("/Registrations", new RegistrationDTO
    {
      EventId = TestEntityLookup.GetEventId(factory.Services, "Summer Games"),
      PersonId = TestEntityLookup.GetPersonId(factory.Services, "DOC-200"),
      SportId = TestEntityLookup.GetSportId(factory.Services, "Swimming")
    });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    RegistrationDTO? payload = await response.Content.ReadFromJsonAsync<RegistrationDTO>();
    Assert.NotNull(payload);
    Assert.True(payload!.Id > 0);
    Assert.Equal(TestEntityLookup.GetSportId(factory.Services, "Swimming"), payload.SportId);
  }

  [Fact]
  public async Task UpdateRegistration()
  {
    int registrationId = TestEntityLookup.GetRegistrationId(factory.Services, "Summer Games", "DOC-200", "Rowing");
    HttpResponseMessage response = await client.PutAsJsonAsync($"/Registrations/{registrationId}", new RegistrationDTO
    {
      Id = registrationId,
      EventId = TestEntityLookup.GetEventId(factory.Services, "Summer Games"),
      PersonId = TestEntityLookup.GetPersonId(factory.Services, "DOC-200"),
      SportId = TestEntityLookup.GetSportId(factory.Services, "Swimming")
    });

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    RegistrationDTO? updated = await (await client.GetAsync($"/Registrations/{registrationId}")).Content.ReadFromJsonAsync<RegistrationDTO>();
    Assert.NotNull(updated);
    Assert.Equal(TestEntityLookup.GetSportId(factory.Services, "Swimming"), updated!.SportId);
  }

  [Fact]
  public async Task PatchRegistration()
  {
    int registrationId = TestEntityLookup.GetRegistrationId(factory.Services, "Summer Games", "DOC-200", "Rowing");
    int swimmingId = TestEntityLookup.GetSportId(factory.Services, "Swimming");
    const string patchDocument = """
      [
        { "op": "replace", "path": "/sportId", "value": __SPORT_ID__ }
      ]
      """;
    string effectivePatchDocument = patchDocument.Replace("__SPORT_ID__", swimmingId.ToString());

    using var content = new StringContent(effectivePatchDocument, Encoding.UTF8, "application/json-patch+json");
    HttpResponseMessage response = await client.PatchAsync($"/Registrations/{registrationId}", content);

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    RegistrationDTO? updated = await (await client.GetAsync($"/Registrations/{registrationId}")).Content.ReadFromJsonAsync<RegistrationDTO>();
    Assert.NotNull(updated);
    Assert.Equal(swimmingId, updated!.SportId);
  }

  [Fact]
  public async Task DeleteRegistration()
  {
    int registrationId = TestEntityLookup.GetRegistrationId(factory.Services, "Summer Games", "DOC-200", "Rowing");
    HttpResponseMessage deleteResponse = await client.DeleteAsync($"/Registrations/{registrationId}");

    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

    HttpResponseMessage getResponse = await client.GetAsync($"/Registrations/{registrationId}");
    Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
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
