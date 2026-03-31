using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace Events.WebAPI.Util.Startup;

public static class AuthSetupExtensions
{
  public static void SetupAuthenticationAndAuthorization(this IServiceCollection services, IConfiguration configuration)
  {
    Microsoft.IdentityModel.JsonWebTokens.JsonWebTokenHandler.DefaultInboundClaimTypeMap.Clear();

    services.AddScoped<IClaimsTransformation, ScopeClaimsTransformation>();

    services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
      .AddJwtBearer(opt =>
      {
        opt.Authority = configuration["Auth:Authority"];
        opt.Audience = configuration["Auth:Audience"];
        opt.TokenValidationParameters = new TokenValidationParameters
        {
          ValidateAudience = true,
          ValidateIssuerSigningKey = true,
          NameClaimType = ClaimTypes.NameIdentifier
        };
        opt.Events = new JwtBearerEvents
        {
          OnAuthenticationFailed = context =>
          {
            if (context.Exception.GetType() == typeof(SecurityTokenExpiredException))
            {
              context.Response.Headers.Append("Token-Expired", "true");
            }

            return Task.CompletedTask;
          }
        };
      });

    services.AddAuthorization(options =>
    {
      foreach (var policy in Policies.All)
      {
        options.AddPolicy(policy.Key, policy.Value);
      }
    });
  }
}
