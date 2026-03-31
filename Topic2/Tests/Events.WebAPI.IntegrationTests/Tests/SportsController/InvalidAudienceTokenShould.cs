using System.Net;
using Tests.ApplicationFactories.JwtBearer;

namespace Tests.SportsController.GivenJwtBearer;

public class InvalidAudienceTokenShould : IClassFixture<JwtBearerWebApplicationFactory>
{
  private readonly HttpClient client;

  public InvalidAudienceTokenShould(JwtBearerWebApplicationFactory factory)
  {
    factory.ResetDatabase();

    string token = TokenGenerator.CreateToken(
      TokenGenerator.Issuer,
      "https://erasmus-sta-2026/invalid-audience",
      TokenGenerator.SecurityKey,
      "events:read");

    client = factory.CreateClient().WithBearerToken(token);
  }

  [Fact]
  public async Task Return401ForGetAll()
  {
    HttpResponseMessage response = await client.GetAsync("/Sports");

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }
}
