using System.Net;
using System.Net.Http.Json;
using Events.WebAPI.Contract.DTOs;
using Tests.ApplicationFactories.JwtBearer;

namespace Tests.SportsController.GivenJwtBearer;

public class ValidReadTokenShould : IClassFixture<JwtBearerWebApplicationFactory>
{
  private readonly HttpClient client;

  public ValidReadTokenShould(JwtBearerWebApplicationFactory factory)
  {
    factory.ResetDatabase();
    client = factory.CreateClient().WithJwtToken("events:read");
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
}
