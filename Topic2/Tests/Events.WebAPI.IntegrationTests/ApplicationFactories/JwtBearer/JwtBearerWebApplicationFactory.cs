using System.Security.Claims;
using Tests.ApplicationFactories.Common;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Events.WebAPI.Util.Startup;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.IdentityModel.Tokens;

namespace Tests.ApplicationFactories.JwtBearer;

public class JwtBearerWebApplicationFactory : WebApiFactoryBase
{
  protected override void ConfigureAuthentication(IServiceCollection services)
  {
    services.RemoveAll<IClaimsTransformation>();
    services.AddScoped<IClaimsTransformation, ScopeClaimsTransformation>();

    services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
    {
      options.Authority = TokenGenerator.Issuer;
      options.Audience = TokenGenerator.Audience;
      options.RequireHttpsMetadata = false;
      options.MetadataAddress = MockBackchannel.MetadataAddress;
      options.BackchannelHttpHandler = new MockBackchannel();
      options.TokenValidationParameters = new TokenValidationParameters
      {
        ValidateIssuer = true,
        ValidIssuer = TokenGenerator.Issuer,
        ValidateAudience = true,
        ValidAudience = TokenGenerator.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = TokenGenerator.SecurityKey,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero,
        NameClaimType = ClaimTypes.NameIdentifier
      };
    });
  }
}
