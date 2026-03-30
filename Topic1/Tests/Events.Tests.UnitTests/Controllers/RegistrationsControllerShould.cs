using Events.EFModel.Models;
using Events.MVC.Controllers;
using Events.MVC.Models.Registrations;
using Events.Tests.UnitTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;

namespace Events.Tests.UnitTests.Controllers;

public class RegistrationsControllerShould
{
    [Fact]
    public async Task RedirectToEventsWhenIndexIsRequestedWithoutEvents()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        var controller = CreateController(ctx, useSieve: false);

        var result = await controller.Index(null, new SieveModel());

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Events", redirect.ControllerName);
        Assert.Equal("At least one event must be created before adding registrations.", controller.TempData["ToastMessage"]);
    }

    [Fact]
    public async Task RedirectToSportsWhenIndexIsRequestedWithoutSports()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Events.Add(new Event { Id = 1, Name = "Event 1", EventDate = new DateOnly(2026, 3, 23) });
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx, useSieve: false);

        var result = await controller.Index(1, new SieveModel());

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Sports", redirect.ControllerName);
        Assert.Equal("At least one sport must be created before adding registrations.", controller.TempData["ToastMessage"]);
    }

    [Fact]
    public async Task RedirectToPeopleWhenIndexIsRequestedWithoutPeople()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Events.Add(new Event { Id = 1, Name = "Event 1", EventDate = new DateOnly(2026, 3, 23) });
        ctx.Sports.Add(new Sport { Id = 1, Name = "Football" });
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx, useSieve: false);

        var result = await controller.Index(1, new SieveModel());

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("People", redirect.ControllerName);
        Assert.Equal("At least one person must be created before adding registrations.", controller.TempData["ToastMessage"]);
    }

    [Fact]
    public async Task CreateRegistration()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        SeedRegistrationDependencies(ctx);
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx);

        var result = await controller.Create(
            new RegistrationViewModel
            {
                EventId = 100,
                PersonId = 1,
                SportId = 10
            },
            ControllerTestContext.EmptySieveModel());

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_RegistrationsPanel", partial.ViewName);
        Assert.Single(ctx.Registrations);
        Assert.Contains("Registration was added successfully", controller.Response.Headers["HX-Trigger"].ToString());
    }

    [Fact]
    public async Task EditRegistration()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        SeedRegistrationDependencies(ctx);
        ctx.People.Add(ControllerTestContext.CreatePerson(id: 2, firstName: "Ana", lastName: "Kovac"));
        ctx.Sports.Add(ControllerTestContext.CreateSport(id: 20, name: "Volleyball"));
        ctx.Registrations.Add(new Registration { Id = 1000, EventId = 100, PersonId = 1, SportId = 10 });
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx, useSieve: false);

        var result = await controller.Edit(1000, new RegistrationViewModel
        {
            Id = 1000,
            EventId = 100,
            PersonId = 2,
            SportId = 20
        });

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_RegistrationRow", partial.ViewName);
        var registration = await ctx.Registrations.SingleAsync();
        Assert.Equal(2, registration.PersonId);
        Assert.Equal(20, registration.SportId);
    }

    [Fact]
    public async Task DeleteRegistration()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        SeedRegistrationDependencies(ctx);
        ctx.Registrations.Add(new Registration { Id = 1000, EventId = 100, PersonId = 1, SportId = 10 });
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx);

        var result = await controller.Delete(1000, 100, ControllerTestContext.EmptySieveModel());

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_RegistrationsPanel", partial.ViewName);
        Assert.Empty(ctx.Registrations);
        Assert.Contains("Registration was deleted successfully", controller.Response.Headers["HX-Trigger"].ToString());
    }

    [Fact]
    public async Task ReturnConflictWhenCreatingRegistrationWithoutDependencies()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        var controller = CreateController(ctx);

        var result = await controller.Create(new RegistrationViewModel { EventId = 100, PersonId = 1, SportId = 10 }, ControllerTestContext.EmptySieveModel());

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(409, controller.Response.StatusCode);
        Assert.Equal("At least one event, one person, and one sport are required before adding registrations.", content.Content);
    }

    private static RegistrationsController CreateController(EventsContext ctx, bool useSieve = true)
    {
        return new RegistrationsController(
            ctx,
            useSieve ? ControllerTestContext.CreateSieveProcessor() : null!,
            ControllerTestContext.CreatePagingOptions())
            .WithTempData();
    }

    private static void SeedRegistrationDependencies(EventsContext ctx)
    {
        ctx.Countries.Add(ControllerTestContext.CreateCountry());
        ctx.People.Add(ControllerTestContext.CreatePerson());
        ctx.Events.Add(ControllerTestContext.CreateEvent());
        ctx.Sports.Add(ControllerTestContext.CreateSport());
    }
}
