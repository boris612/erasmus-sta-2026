using System.Net;
using System.Net.Http.Json;
using Events.WebAPI.Contract.DTOs;
using Tests.ApplicationFactories.TestAuth;
using Tests.Helpers;

namespace Tests.SportsController.Given;

public class UserWithReadScopeShould : IClassFixture<TestAuthWebApplicationFactory>
{
  private readonly TestAuthWebApplicationFactory factory;
  private readonly HttpClient client;

  public UserWithReadScopeShould(TestAuthWebApplicationFactory factory)
  {
    this.factory = factory;
    factory.ResetDatabase();
    client = factory.CreateClient().WithScopes("events:read");
  }

  [Fact]
  public async Task ReturnFilteredItems()
  {
    HttpResponseMessage response = await client.GetAsync("/Sports?filters=Name@=*ball");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    Items<SportDTO>? payload = await response.Content.ReadFromJsonAsync<Items<SportDTO>>();
    Assert.NotNull(payload);
    Assert.Equal(1, payload!.Count);
    Assert.Single(payload.Data!);
    Assert.Equal("Football", payload.Data![0].Name);
  }

  [Fact]
  public async Task ReturnSingleItem()
  {
    int sportId = TestEntityLookup.GetSportId(factory.Services, "Athletics");
    HttpResponseMessage response = await client.GetAsync($"/Sports/{sportId}");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    SportDTO? payload = await response.Content.ReadFromJsonAsync<SportDTO>();
    Assert.NotNull(payload);
    Assert.Equal(sportId, payload!.Id);
    Assert.Equal("Athletics", payload.Name);
  }

  [Fact]
  public async Task Return403ForCreate()
  {
    HttpResponseMessage response = await client.PostAsJsonAsync("/Sports", new SportDTO { Name = "Volleyball" });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }
}
