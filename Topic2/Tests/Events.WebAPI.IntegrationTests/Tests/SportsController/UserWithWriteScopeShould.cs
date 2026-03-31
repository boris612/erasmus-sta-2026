using System.Net;
using System.Net.Http.Json;
using System.Text;
using Events.WebAPI.Contract.DTOs;
using Tests.ApplicationFactories.TestAuth;
using Tests.Helpers;

namespace Tests.SportsController.Given;

public class UserWithWriteScopeShould : IClassFixture<TestAuthWebApplicationFactory>
{
  private readonly TestAuthWebApplicationFactory factory;
  private readonly HttpClient client;

  public UserWithWriteScopeShould(TestAuthWebApplicationFactory factory)
  {
    this.factory = factory;
    factory.ResetDatabase();
    client = factory.CreateClient().WithScopes("events:read", "events:write");
  }

  [Fact]
  public async Task CreateSport()
  {
    HttpResponseMessage response = await client.PostAsJsonAsync("/Sports", new SportDTO { Name = "Volleyball" });

    Assert.Equal(HttpStatusCode.Created, response.StatusCode);

    SportDTO? payload = await response.Content.ReadFromJsonAsync<SportDTO>();
    Assert.NotNull(payload);
    Assert.True(payload!.Id > 0);
    Assert.Equal("Volleyball", payload.Name);
  }

  [Fact]
  public async Task UpdateSport()
  {
    int sportId = TestEntityLookup.GetSportId(factory.Services, "Athletics");
    HttpResponseMessage response = await client.PutAsJsonAsync($"/Sports/{sportId}", new SportDTO { Id = sportId, Name = "Track and Field" });

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    SportDTO? updated = await (await client.GetAsync($"/Sports/{sportId}")).Content.ReadFromJsonAsync<SportDTO>();
    Assert.NotNull(updated);
    Assert.Equal("Track and Field", updated!.Name);
  }

  [Fact]
  public async Task PatchSport()
  {
    int sportId = TestEntityLookup.GetSportId(factory.Services, "Swimming");
    const string patchDocument = """
      [
        { "op": "replace", "path": "/name", "value": "Water Polo" }
      ]
      """;

    using var content = new StringContent(patchDocument, Encoding.UTF8, "application/json-patch+json");
    HttpResponseMessage response = await client.PatchAsync($"/Sports/{sportId}", content);

    Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

    SportDTO? updated = await (await client.GetAsync($"/Sports/{sportId}")).Content.ReadFromJsonAsync<SportDTO>();
    Assert.NotNull(updated);
    Assert.Equal("Water Polo", updated!.Name);
  }

  [Fact]
  public async Task DeleteSport()
  {
    int sportId = TestEntityLookup.GetSportId(factory.Services, "Football");
    HttpResponseMessage deleteResponse = await client.DeleteAsync($"/Sports/{sportId}");

    Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

    HttpResponseMessage getResponse = await client.GetAsync($"/Sports/{sportId}");
    Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
  }
}
