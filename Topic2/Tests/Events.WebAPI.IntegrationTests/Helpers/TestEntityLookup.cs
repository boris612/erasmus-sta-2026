using Events.WebAPI.Handlers.EF.Models;
using Microsoft.Extensions.DependencyInjection;

namespace Tests.Helpers;

public static class TestEntityLookup
{
  public static int GetEventId(IServiceProvider services, string name)
  {
    using IServiceScope scope = services.CreateScope();
    EventsContext ctx = scope.ServiceProvider.GetRequiredService<EventsContext>();
    return GetEventId(ctx, name);
  }

  public static int GetPersonId(IServiceProvider services, string documentNumber)
  {
    using IServiceScope scope = services.CreateScope();
    EventsContext ctx = scope.ServiceProvider.GetRequiredService<EventsContext>();
    return GetPersonId(ctx, documentNumber);
  }

  public static int GetSportId(IServiceProvider services, string name)
  {
    using IServiceScope scope = services.CreateScope();
    EventsContext ctx = scope.ServiceProvider.GetRequiredService<EventsContext>();
    return GetSportId(ctx, name);
  }

  public static int GetRegistrationId(IServiceProvider services, string eventName, string documentNumber, string sportName)
  {
    using IServiceScope scope = services.CreateScope();
    EventsContext ctx = scope.ServiceProvider.GetRequiredService<EventsContext>();
    return GetRegistrationId(ctx, eventName, documentNumber, sportName);
  }

  public static int GetEventId(EventsContext ctx, string name) =>
    ctx.Events.Single(item => item.Name == name).Id;

  public static int GetPersonId(EventsContext ctx, string documentNumber) =>
    ctx.People.Single(item => item.DocumentNumber == documentNumber).Id;

  public static int GetSportId(EventsContext ctx, string name) =>
    ctx.Sports.Single(item => item.Name == name).Id;

  public static int GetRegistrationId(EventsContext ctx, string eventName, string documentNumber, string sportName) =>
    ctx.Registrations.Single(item =>
      item.Event.Name == eventName &&
      item.Person.DocumentNumber == documentNumber &&
      item.Sport.Name == sportName).Id;
}
