using Events.EFModel.Models;
using Events.MVC.Controllers;
using Events.MVC.Models.Events;
using Events.Tests.UnitTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Events.Tests.UnitTests.Controllers;

public class EventsControllerShould
{
    [Fact]
    public async Task ReturnPartialViewWithExpectedViewModelForExistingRow()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Events.Add(ControllerTestContext.CreateEvent());
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx, useSieve: false);

        var result = await controller.Row(100);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_EventRow", partial.ViewName);
        Assert.Equal("Spring Games", Assert.IsType<EventViewModel>(partial.Model).Name);
    }

    [Fact]
    public async Task CreateEvent()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        var controller = CreateController(ctx);

        var result = await controller.Create(
            new EventViewModel { Name = "Autumn Cup", EventDate = new DateOnly(2026, 9, 10) },
            ControllerTestContext.EmptySieveModel());

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_EventsList", partial.ViewName);
        Assert.Contains(ctx.Events, e => e.Name == "Autumn Cup");
        Assert.Contains("was added successfully", controller.Response.Headers["HX-Trigger"].ToString());
    }

    [Fact]
    public async Task EditEvent()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Events.Add(ControllerTestContext.CreateEvent());
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx, useSieve: false);

        var result = await controller.Edit(100, new EventViewModel { Id = 100, Name = "Updated Games", EventDate = new DateOnly(2026, 5, 20) });

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_EventRow", partial.ViewName);
        Assert.Equal("Updated Games", (await ctx.Events.SingleAsync()).Name);
    }

    [Fact]
    public async Task DeleteEvent()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Events.Add(ControllerTestContext.CreateEvent());
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx);

        var result = await controller.Delete(100, ControllerTestContext.EmptySieveModel());

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_EventsList", partial.ViewName);
        Assert.Empty(ctx.Events);
        Assert.Contains("was deleted successfully", controller.Response.Headers["HX-Trigger"].ToString());
    }

    [Fact]
    public async Task ReturnConflictWhenDeletingEventWithRegistrations()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Countries.Add(ControllerTestContext.CreateCountry());
        ctx.People.Add(ControllerTestContext.CreatePerson());
        ctx.Events.Add(ControllerTestContext.CreateEvent());
        ctx.Sports.Add(ControllerTestContext.CreateSport());
        ctx.Registrations.Add(new Registration { Id = 1000, EventId = 100, PersonId = 1, SportId = 10 });
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx);

        var result = await controller.Delete(100, ControllerTestContext.EmptySieveModel());

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(409, controller.Response.StatusCode);
        Assert.Equal("The event cannot be deleted because registrations exist.", content.Content);
    }

    private static EventsController CreateController(EventsContext ctx, bool useSieve = true)
    {
        return new EventsController(
            ctx,
            useSieve ? ControllerTestContext.CreateSieveProcessor() : null!,
            ControllerTestContext.CreatePagingOptions())
            .WithTempData();
    }
}
