using System.Net;
using System.Net.Http.Json;
using Events.WebAPI.Contract.DTOs;
using Tests.ApplicationFactories.JwtBearer;

namespace Tests.SportsController.GivenJwtBearer;

public class ValidWriteTokenShould : IClassFixture<JwtBearerWebApplicationFactory>
{
  private readonly HttpClient client;

  public ValidWriteTokenShould(JwtBearerWebApplicationFactory factory)
  {
    factory.ResetDatabase();
    client = factory.CreateClient().WithJwtToken("events:read", "events:write");
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
}
