using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace Tests.ApplicationFactories.JwtBearer;

public sealed class MockBackchannel : HttpMessageHandler
{
  public const string Authority = "https://mock.integration.tests";
  public const string MetadataAddress = $"{Authority}/.well-known/openid-configuration";

  protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    if (request.RequestUri?.AbsoluteUri == MetadataAddress)
    {
      const string metadata = """
        {
          "issuer": "https://mock.integration.tests/",
          "jwks_uri": "https://mock.integration.tests/.well-known/jwks.json"
        }
        """;

      return Task.FromResult(CreateJsonResponse(metadata));
    }

    if (request.RequestUri?.AbsoluteUri == $"{Authority}/.well-known/jwks.json")
    {
      const string jwks = """
        {
          "keys": []
        }
        """;

      return Task.FromResult(CreateJsonResponse(jwks));
    }

    return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)
    {
      RequestMessage = request
    });
  }

  private static HttpResponseMessage CreateJsonResponse(string content)
  {
    return new HttpResponseMessage(HttpStatusCode.OK)
    {
      Content = new StringContent(content, Encoding.UTF8, "application/json")
      {
        Headers =
        {
          ContentType = new MediaTypeHeaderValue("application/json")
        }
      }
    };
  }
}
