using Events.WebAPI.MessageConsumers;
using Events.WebAPI.Util.Settings;
using MassTransit;
using Microsoft.Extensions.Options;

namespace Events.WebAPI.Util.Startup;

public static class MassTransitSetupExtensions
{
  public static void SetupMassTransit(this IServiceCollection services, IConfiguration configuration)
  {
    services.AddOptions<RabbitMqSettings>()
      .Bind(configuration.GetSection("RabbitMq"))
      .ValidateDataAnnotations()
      .Validate(
        settings => Uri.TryCreate(settings.Host, UriKind.Absolute, out var uri) &&
                    uri.Scheme == "rabbitmq" &&
                    !string.IsNullOrWhiteSpace(uri.Host),
        "RabbitMq:Host must be a valid absolute rabbitmq:// URI.")
      .ValidateOnStart();

    services.AddMassTransit(x =>
    {
      x.AddConsumer<RegistrationNotificationsConsumer>();
      x.AddConsumer<EventRegistrationsExcelConsumer>();

      x.UsingRabbitMq((context, cfg) =>
      {
        var settings = context.GetRequiredService<IOptions<RabbitMqSettings>>().Value;

        cfg.Host(new Uri(settings.Host), h =>
        {
          h.Username(settings.Username);
          h.Password(settings.Password);
        });

        cfg.ReceiveEndpoint("events-webapi-registration-changes", e =>
        {
          e.ConfigureConsumer<RegistrationNotificationsConsumer>(context);
          e.ConfigureConsumer<EventRegistrationsExcelConsumer>(context);
        });
      });
    });
  }
}
