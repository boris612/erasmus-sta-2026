using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;

namespace Tests.ApplicationFactories.JwtBearer;

public static class TokenGenerator
{
  public const string Issuer = "https://mock.integration.tests/";
  public const string Audience = "https://erasmus-sta-2026/events-api";

  private static readonly byte[] KeyBytes = "integration-tests-signing-key-2026-32-bytes!!"u8.ToArray();
  public static readonly SymmetricSecurityKey SecurityKey = new(KeyBytes);

  public static string CreateToken(params string[] scopes)
  {
    return CreateToken(Issuer, Audience, SecurityKey, scopes);
  }

  public static string CreateToken(
    string issuer,
    string audience,
    SecurityKey securityKey,
    params string[] scopes)
  {
    var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);
    string scopeValue = string.Join(' ', scopes);

    var claims = new List<Claim>
    {
      new(ClaimTypes.NameIdentifier, "integration-test-user"),
      new(ClaimTypes.Name, "Integration Test User"),
      new("scope", scopeValue)
    };

    var token = new JwtSecurityToken(
      issuer: issuer,
      audience: audience,
      claims: claims,
      notBefore: DateTime.UtcNow.AddMinutes(-1),
      expires: DateTime.UtcNow.AddMinutes(30),
      signingCredentials: credentials);

    return new JwtSecurityTokenHandler().WriteToken(token);
  }
}
