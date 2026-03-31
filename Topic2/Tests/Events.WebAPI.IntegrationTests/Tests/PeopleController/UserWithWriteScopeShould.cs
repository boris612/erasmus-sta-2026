using System.Net;
using System.Net.Http.Json;
using System.Text;
using Events.WebAPI.Contract.DTOs;
using Events.WebAPI.Handlers.EF.Models;
using Microsoft.Extensions.DependencyInjection;
using Tests.ApplicationFactories.TestAuth;
using Tests.Helpers;

namespace Tests.PeopleController.Given;

public class UserWithWriteScopeShould : IClassFixture<TestAuthWebApplicationFactory>
{
  private readonly TestAuthWebApplicationFactory factory;
  private readonly HttpClient client;

  public UserWithWriteScopeShould(TestAuthWebApplicationFactory factory)
  {
    this.factory = factory;
    factory.ResetDatabase();
    SeedPeople(factory.Services);
    client = factory.CreateClient().WithScopes("events:read", "events:write");
  }

  [Fact]
  public async Task CreatePerson()
  {
    HttpResponseMessage response = await client.PostAsJsonAsync("/People", CreatePersonDto("Maja", "Kovac", "DOC-003"));

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    PersonDTO? payload = await response.Content.ReadFromJsonAsync<PersonDTO>();
    Assert.NotNull(payload);
    Assert.True(payload!.Id > 0);
    Assert.Equal("Maja", payload.FirstName);
  }

  [Fact]
  public async Task UpdatePerson()
  {
    int personId = TestEntityLookup.GetPersonId(factory.Services, "DOC-001");
    PersonDTO dto = CreatePersonDto("Iva", "Peric", "DOC-001");
    dto.Id = personId;

    HttpResponseMessage response = await client.PutAsJsonAsync($"/People/{personId}", dto);

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    PersonDTO? updated = await (await client.GetAsync($"/People/{personId}")).Content.ReadFromJsonAsync<PersonDTO>();
    Assert.NotNull(updated);
    Assert.Equal("Peric", updated!.LastName);
  }

  [Fact]
  public async Task PatchPerson()
  {
    int personId = TestEntityLookup.GetPersonId(factory.Services, "DOC-002");
    const string patchDocument = """
      [
        { "op": "replace", "path": "/city", "value": "Osijek" }
      ]
      """;

    using var content = new StringContent(patchDocument, Encoding.UTF8, "application/json-patch+json");
    HttpResponseMessage response = await client.PatchAsync($"/People/{personId}", content);

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    PersonDTO? updated = await (await client.GetAsync($"/People/{personId}")).Content.ReadFromJsonAsync<PersonDTO>();
    Assert.NotNull(updated);
    Assert.Equal("Osijek", updated!.City);
  }

  [Fact]
  public async Task DeletePerson()
  {
    int personId = TestEntityLookup.GetPersonId(factory.Services, "DOC-002");
    HttpResponseMessage deleteResponse = await client.DeleteAsync($"/People/{personId}");

    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

    HttpResponseMessage getResponse = await client.GetAsync($"/People/{personId}");
    Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
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

  private static PersonDTO CreatePersonDto(string firstName, string lastName, string documentNumber)
  {
    return new PersonDTO
    {
      FirstName = firstName,
      LastName = lastName,
      FirstNameTranscription = firstName,
      LastNameTranscription = lastName,
      AddressLine = "Street 2",
      PostalCode = "21000",
      City = "Split",
      AddressCountry = "Croatia",
      Email = $"{firstName.ToLowerInvariant()}@example.com",
      ContactPhone = "+385987654",
      BirthDate = new DateOnly(1992, 2, 2),
      DocumentNumber = documentNumber,
      CountryCode = "HR"
    };
  }
}
