using System.Net.Http.Headers;
using Tests.ApplicationFactories.JwtBearer;
using Tests.ApplicationFactories.TestAuth;

namespace Tests;

public static class HttpClientAuthExtensions
{
  public static HttpClient WithBearerToken(this HttpClient client, string token)
  {
    client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    return client;
  }

  public static HttpClient WithScopes(this HttpClient client, params string[] scopes)
  {
    client.DefaultRequestHeaders.Remove(TestAuthHandler.ScopesHeader);
    client.DefaultRequestHeaders.Add(TestAuthHandler.ScopesHeader, string.Join(' ', scopes));
    return client;
  }

  public static HttpClient WithJwtToken(this HttpClient client, params string[] scopes)
  {
    return client.WithBearerToken(TokenGenerator.CreateToken(scopes));
  }
}
