using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;

namespace Events.WebAPI.Util.Startup;

public sealed class ScopeClaimsTransformation : IClaimsTransformation
{
  public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
  {
    if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
    {
      return Task.FromResult(principal);
    }

    Claim[] combinedScopeClaims = identity
      .FindAll("scope")
      .Where(claim => claim.Value.Contains(' '))
      .ToArray();

    if (combinedScopeClaims.Length == 0)
    {
      return Task.FromResult(principal);
    }

    var additionalIdentity = new ClaimsIdentity();

    foreach (Claim combinedClaim in combinedScopeClaims)
    {
      foreach (string scope in combinedClaim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
      {
        if (identity.HasClaim("scope", scope) || additionalIdentity.HasClaim("scope", scope))
        {
          continue;
        }

        additionalIdentity.AddClaim(new Claim("scope", scope, combinedClaim.ValueType, combinedClaim.Issuer));
      }
    }

    if (additionalIdentity.Claims.Any())
    {
      principal.AddIdentity(additionalIdentity);
    }

    return Task.FromResult(principal);
  }
}
