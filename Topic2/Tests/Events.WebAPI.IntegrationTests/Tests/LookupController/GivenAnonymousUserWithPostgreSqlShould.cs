using System.Net;
using System.Net.Http.Json;
using Events.WebAPI.Contract.DTOs;
using Tests.ApplicationFactories.TestAuth;

namespace Tests.LookupController;

public class GivenAnonymousUserWithPostgreSqlShould : IClassFixture<TestAuthWebApplicationFactory>
{
  private readonly HttpClient client;

  public GivenAnonymousUserWithPostgreSqlShould(TestAuthWebApplicationFactory factory)
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

  [Theory]
  [InlineData("ov", "Slovenia")]
  [InlineData("ati", "Croatia")]
  [InlineData("edo", "Macedonia")]
  public async Task FilterCountriesBySubstring(string text, string expectedCountryName)
  {
    HttpResponseMessage response = await client.GetAsync($"/Lookup/Countries?text={text}");

    Assert.Equal(HttpStatusCode.OK, response.StatusCode);

    List<IdName<string>>? payload = await response.Content.ReadFromJsonAsync<List<IdName<string>>>();
    Assert.NotNull(payload);
    Assert.Collection(payload!, item => Assert.Equal(expectedCountryName, item.Name));
  }
}
