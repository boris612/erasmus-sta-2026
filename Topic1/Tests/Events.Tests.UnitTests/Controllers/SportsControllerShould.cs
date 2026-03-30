using Events.EFModel.Models;
using Events.MVC.Controllers;
using Events.MVC.Models;
using Events.MVC.Models.Sports;
using Events.Tests.UnitTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;
using Sieve.Models;

namespace Events.Tests.UnitTests.Controllers;

public class SportsControllerShould
{
    [Fact]
    public async Task ReturnPartialViewWithExpectedViewModelForExistingRow()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Sports.Add(new Sport { Id = 5, Name = "Basketball" });
        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx, useSieve: false);

        var result = await controller.Row(5);

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_SportRow", partial.ViewName);
        var model = Assert.IsType<SportViewModel>(partial.Model);
        Assert.Equal(5, model.Id);
        Assert.Equal("Basketball", model.Name);
    }

    [Fact]
    public async Task ReturnNotFoundForMissingRow()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        var controller = CreateController(ctx, useSieve: false);

        var result = await controller.Row(404);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task CreateSport()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        var controller = CreateController(ctx);

        var result = await controller.Create(new SportViewModel { Name = "Volleyball" }, ControllerTestContext.EmptySieveModel());

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_SportsList", partial.ViewName);
        Assert.Contains(ctx.Sports, s => s.Name == "Volleyball");
        Assert.Contains("was added successfully", controller.Response.Headers["HX-Trigger"].ToString());
    }

    [Fact]
    public async Task ReturnPagedSportsWhenIndexIsRequestedUsingMockedPagingOptions()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Sports.AddRange(
            new Sport { Id = 1, Name = "Athletics" },
            new Sport { Id = 2, Name = "Basketball" },
            new Sport { Id = 3, Name = "Cycling" });
        await ctx.SaveChangesAsync();

        var optionsMock = new Mock<IOptions<PagingSettings>>();
        optionsMock
            .SetupGet(options => options.Value)
            .Returns(new PagingSettings { PageSize = 2 });

        var controller = new SportsController(
            ctx,
            ControllerTestContext.CreateSieveProcessor(),
            optionsMock.Object)
            .WithTempData();

        var result = await controller.Index(new SieveModel());

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PagedList<SportViewModel>>(view.Model);
        Assert.Equal(2, model.Data.Count);
        Assert.Equal(2, model.PagingInfo.ItemsPerPage);
        Assert.Equal(3, model.PagingInfo.TotalItemsCount);
        Assert.Equal(3, model.PagingInfo.FilteredItemsCount);
    }

    [Fact]
    public async Task PopulateModelStateValidationErrorsForMissingName()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        var controller = CreateController(ctx);
        var invalidModel = new SportViewModel { Name = string.Empty };

        var result = await controller.Create(invalidModel, ControllerTestContext.EmptySieveModel());

        Assert.False(
            controller.ModelState.IsValid,
            "This assertion intentionally demonstrates an incorrect expectation: unit tests do not run the MVC validation pipeline automatically.");
        Assert.True(
            controller.ModelState.ContainsKey(nameof(SportViewModel.Name)),
            "This assertion intentionally demonstrates an incorrect expectation: without MVC model validation, ModelState should not contain a validation entry for Name.");
        Assert.Contains(
            controller.ModelState[nameof(SportViewModel.Name)]!.Errors,
            error => error.ErrorMessage == "The Name field is required.");

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_CreateSportForm", partial.ViewName);
    }

    [Fact]
    public async Task EditSport()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Sports.Add(ControllerTestContext.CreateSport());
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx, useSieve: false);

        var result = await controller.Edit(10, new SportViewModel { Id = 10, Name = "Volleyball" });

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_SportRow", partial.ViewName);
        Assert.Equal("Volleyball", (await ctx.Sports.SingleAsync()).Name);
    }

    [Fact]
    public async Task DeleteSport()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Sports.Add(ControllerTestContext.CreateSport());
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx);

        var result = await controller.Delete(10, ControllerTestContext.EmptySieveModel());

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_SportsList", partial.ViewName);
        Assert.Empty(ctx.Sports);
        Assert.Contains("was deleted successfully", controller.Response.Headers["HX-Trigger"].ToString());
    }

    [Fact]
    public async Task ReturnConflictWhenDeletingSportWithRegistrations()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Countries.Add(ControllerTestContext.CreateCountry());
        ctx.People.Add(ControllerTestContext.CreatePerson());
        ctx.Events.Add(ControllerTestContext.CreateEvent());
        ctx.Sports.Add(ControllerTestContext.CreateSport());
        ctx.Registrations.Add(new Registration
        {
            Id = 1000,
            EventId = 100,
            PersonId = 1,
            SportId = 10
        });
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx);

        var result = await controller.Delete(10, ControllerTestContext.EmptySieveModel());

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(409, controller.Response.StatusCode);
        Assert.Equal("The sport cannot be deleted because registrations exist.", content.Content);
    }

    private static SportsController CreateController(EventsContext ctx, bool useSieve = true)
    {
        var controller = new SportsController(
            ctx,
            useSieve ? ControllerTestContext.CreateSieveProcessor() : null!,
            ControllerTestContext.CreatePagingOptions())
            .WithTempData();

        return controller;
    }
}
