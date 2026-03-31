using System.Net;
using Events.WebAPI.Handlers.EF.Models;
using Microsoft.Extensions.DependencyInjection;
using Tests.ApplicationFactories.TestAuth;
using Tests.Helpers;

namespace Tests.EventsController.Given;

public class UserWithReadScopeShould : IClassFixture<TestAuthWebApplicationFactory>
{
  private readonly TestAuthWebApplicationFactory factory;
  private readonly HttpClient client;

  public UserWithReadScopeShould(TestAuthWebApplicationFactory factory)
  {
    this.factory = factory;
    factory.ResetDatabase();
    SeedEventRegistrations(factory.Services);
    client = factory.CreateClient().WithScopes("events:read");
  }

  [Fact]
  public async Task GenerateAndReturnRegistrationsExcelWhenMissing()
  {
    int eventId = TestEntityLookup.GetEventId(factory.Services, "Summer Games");

    HttpResponseMessage response = await client.GetAsync($"/Events/{eventId}/RegistrationsExcel");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", response.Content.Headers.ContentType?.MediaType);
    Assert.NotNull(response.Content.Headers.ContentDisposition?.FileName);
  }

  private static void SeedEventRegistrations(IServiceProvider services)
  {
    using IServiceScope scope = services.CreateScope();
    EventsContext ctx = scope.ServiceProvider.GetRequiredService<EventsContext>();

    if (!ctx.Events.Any(e => e.Name == "Summer Games"))
      ctx.Events.Add(new Event { Name = "Summer Games", EventDate = new DateOnly(2026, 6, 15) });
    if (!ctx.Sports.Any(s => s.Name == "Rowing"))
      ctx.Sports.Add(new Sport { Name = "Rowing" });
    if (!ctx.People.Any(p => p.DocumentNumber == "DOC-300"))
      ctx.People.Add(CreatePersonEntity("Ana", "Anic", "DOC-300"));
    ctx.SaveChanges();

    if (!ctx.Registrations.Any())
    {
      ctx.Registrations.Add(new Registration
      {
        EventId = ctx.Events.Single(e => e.Name == "Summer Games").Id,
        PersonId = ctx.People.Single(p => p.DocumentNumber == "DOC-300").Id,
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
