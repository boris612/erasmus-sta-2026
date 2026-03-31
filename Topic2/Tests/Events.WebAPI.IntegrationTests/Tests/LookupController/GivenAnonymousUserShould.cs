using System.Net;
using System.Net.Http.Json;
using Events.WebAPI.Contract.DTOs;
using Tests.ApplicationFactories.TestAuth;

namespace Tests.LookupController;

public class GivenAnonymousUserShould : IClassFixture<TestAuthInMemoryWebApplicationFactory>
{
  private readonly HttpClient client;

  public GivenAnonymousUserShould(TestAuthInMemoryWebApplicationFactory factory)
  {
    factory.ResetDatabase();
    client = factory.CreateClient();
  }

  [Fact]
  public async Task ReturnCountries()
  {
    HttpResponseMessage response = await client.GetAsync("/Lookup/Countries");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    List<IdName<string>>? payload = await response.Content.ReadFromJsonAsync<List<IdName<string>>>();
    Assert.NotNull(payload);
    Assert.Contains(payload!, item => item.Id == "HR" && item.Name == "Croatia");
  }

  [Fact]
  public async Task FilterCountriesBySubstring()
  {
    HttpResponseMessage response = await client.GetAsync("/Lookup/Countries?text=ov");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    List<IdName<string>>? payload = await response.Content.ReadFromJsonAsync<List<IdName<string>>>();
    Assert.NotNull(payload);
    Assert.Collection(payload!, item => Assert.Equal("Slovenia", item.Name));
  }
}
