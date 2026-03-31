using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Tests.ApplicationFactories.Common;
using Microsoft.Extensions.DependencyInjection.Extensions;
using MassTransit;
using Moq;

namespace Tests.ApplicationFactories.TestAuth;

public class TestAuthWebApplicationFactory : PostgreSqlWebApiFactoryBase
{
  private readonly Mock<IPublishEndpoint> publishEndpointMock = new();

  protected override void ConfigureAuthentication(IServiceCollection services)
  {
    services.RemoveAll<IPublishEndpoint>();
    services.AddSingleton(publishEndpointMock.Object);

    services.AddAuthentication(options =>
    {
      options.DefaultAuthenticateScheme = TestAuthHandler.SchemeName;
      options.DefaultChallengeScheme = TestAuthHandler.SchemeName;
      options.DefaultScheme = TestAuthHandler.SchemeName;
    }).AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
  }
}
