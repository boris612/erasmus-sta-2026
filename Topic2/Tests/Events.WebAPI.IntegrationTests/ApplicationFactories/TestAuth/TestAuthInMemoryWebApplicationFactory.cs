using MassTransit;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;
using Tests.ApplicationFactories.Common;

namespace Tests.ApplicationFactories.TestAuth;

public class TestAuthInMemoryWebApplicationFactory : WebApiFactoryBase
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
