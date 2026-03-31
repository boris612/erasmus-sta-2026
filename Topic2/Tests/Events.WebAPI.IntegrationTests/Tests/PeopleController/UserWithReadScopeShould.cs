using System.Net;
using System.Net.Http.Json;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Handlers.EF.Models;
using Microsoft.Extensions.DependencyInjection;
using Tests.ApplicationFactories.TestAuth;
using Tests.Helpers;

namespace Tests.PeopleController.Given;

public class UserWithReadScopeShould : IClassFixture<TestAuthWebApplicationFactory>
{
  private readonly TestAuthWebApplicationFactory factory;
  private readonly HttpClient client;

  public UserWithReadScopeShould(TestAuthWebApplicationFactory factory)
  {
    this.factory = factory;
    factory.ResetDatabase();
    SeedPeople(factory.Services);
    client = factory.CreateClient().WithScopes("events:read");
  }

  [Fact]
  public async Task ReturnFilteredItems()
  {
    HttpResponseMessage response = await client.GetAsync("/People?filters=LastName@=*vic");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    Items<PersonDTO>? payload = await response.Content.ReadFromJsonAsync<Items<PersonDTO>>();
    Assert.NotNull(payload);
    Assert.Equal(1, payload!.Count);
    Assert.Single(payload.Data!);
    Assert.Equal("Ivic", payload.Data![0].LastName);
  }

  [Fact]
  public async Task ReturnSingleItem()
  {
    int personId = TestEntityLookup.GetPersonId(factory.Services, "DOC-001");
    HttpResponseMessage response = await client.GetAsync($"/People/{personId}");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    PersonDTO? payload = await response.Content.ReadFromJsonAsync<PersonDTO>();
    Assert.NotNull(payload);
    Assert.Equal(personId, payload!.Id);
    Assert.Equal("Iva", payload.FirstName);
  }

  [Fact]
  public async Task Return403ForCreate()
  {
    HttpResponseMessage response = await client.PostAsJsonAsync("/People", CreatePersonDto());

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }

  private static void SeedPeople(IServiceProvider services)
  {
    using IServiceScope scope = services.CreateScope();
    EventsContext ctx = scope.ServiceProvider.GetRequiredService<EventsContext>();

    if (ctx.People.Any())
      return;

    ctx.People.AddRange(
      CreatePersonEntity("Iva", "Ivic", "DOC-001"),
      CreatePersonEntity("Ana", "Horvat", "DOC-002"));
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

  private static PersonDTO CreatePersonDto()
  {
    return new PersonDTO
    {
      FirstName = "Maja",
      LastName = "Kovac",
      FirstNameTranscription = "Maja",
      LastNameTranscription = "Kovac",
      AddressLine = "Street 2",
      PostalCode = "21000",
      City = "Split",
      AddressCountry = "Croatia",
      Email = "maja@example.com",
      ContactPhone = "+385987654",
      BirthDate = new DateOnly(1992, 2, 2),
      DocumentNumber = "DOC-003",
      CountryCode = "HR"
    };
  }
}
