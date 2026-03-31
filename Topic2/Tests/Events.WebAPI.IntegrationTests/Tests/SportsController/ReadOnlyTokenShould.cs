using System.Net;
using System.Net.Http.Json;
using Events.WebAPI.Contract.DTOs;
using Tests.ApplicationFactories.JwtBearer;

namespace Tests.SportsController.GivenJwtBearer;

public class ReadOnlyTokenShould : IClassFixture<JwtBearerWebApplicationFactory>
{
  private readonly HttpClient client;

  public ReadOnlyTokenShould(JwtBearerWebApplicationFactory factory)
  {
    factory.ResetDatabase();
    client = factory.CreateClient().WithJwtToken("events:read");
  }

  [Fact]
  public async Task Return403ForCreate()
  {
    HttpResponseMessage response = await client.PostAsJsonAsync("/Sports", new SportDTO { Name = "Volleyball" });

    Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
  }
}
