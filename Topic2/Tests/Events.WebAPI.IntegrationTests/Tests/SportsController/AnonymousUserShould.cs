using System.Net;
using Tests.ApplicationFactories.TestAuth;

namespace Tests.SportsController.Given;

public class AnonymousUserShould : IClassFixture<TestAuthWebApplicationFactory>
{
  private readonly HttpClient client;

  public AnonymousUserShould(TestAuthWebApplicationFactory factory)
  {
    factory.ResetDatabase();
    client = factory.CreateClient();
  }

  [Fact]
  public async Task Return401ForGetAll()
  {
    HttpResponseMessage response = await client.GetAsync("/Sports");

    Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
  }
}
