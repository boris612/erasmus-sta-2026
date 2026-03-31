using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Tests.ApplicationFactories.TestAuth;

public class TestAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
  public const string SchemeName = "Test";
  public const string ScopesHeader = "X-Test-Scopes";

  public TestAuthHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder)
    : base(options, logger, encoder)
  {
  }

  protected override Task<AuthenticateResult> HandleAuthenticateAsync()
  {
    if (!Request.Headers.TryGetValue(ScopesHeader, out var scopesHeaderValues))
    {
      return Task.FromResult(AuthenticateResult.NoResult());
    }

    string scopesValue = scopesHeaderValues.ToString();
    if (string.IsNullOrWhiteSpace(scopesValue))
    {
      return Task.FromResult(AuthenticateResult.NoResult());
    }

    var claims = new List<Claim>
    {
      new(ClaimTypes.NameIdentifier, "integration-test-user"),
      new(ClaimTypes.Name, "Integration Test User"),
      new("scope", scopesValue)
    };

    foreach (string scope in scopesValue.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
      claims.Add(new Claim("scope", scope));
    }

    var identity = new ClaimsIdentity(claims, SchemeName);
    var principal = new ClaimsPrincipal(identity);
    var ticket = new AuthenticationTicket(principal, SchemeName);

    return Task.FromResult(AuthenticateResult.Success(ticket));
  }
}
