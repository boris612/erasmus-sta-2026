using Events.EFModel.Models;
using Events.MVC.Controllers;
using Events.MVC.Models.Countries;
using Events.Tests.UnitTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Events.Tests.UnitTests.Controllers;

public class CountriesControllerShould
{
    [Fact]
    public async Task ReturnPartialViewWithExpectedViewModelForExistingRow()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Countries.Add(ControllerTestContext.CreateCountry());
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx, useSieve: false);

        var result = await controller.Row("HR");

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_CountryRow", partial.ViewName);
        Assert.Equal("Croatia", Assert.IsType<CountryViewModel>(partial.Model).Name);
    }

    [Fact]
    public async Task CreateCountry()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        var controller = CreateController(ctx);

        var result = await controller.Create(
            new CountryViewModel
            {
                Code = "de",
                Alpha3 = "deu",
                Name = "Germany",
                Translations =
                [
                    new CountryTranslationViewModel { LanguageCode = "hr", Name = "Germany" }
                ]
            },
            ControllerTestContext.EmptySieveModel());

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_CountriesList", partial.ViewName);
        var country = await ctx.Countries.SingleAsync();
        Assert.Equal("DE", country.Code);
        Assert.Equal("Germany", country.Name);
        Assert.Contains("was added successfully", controller.Response.Headers["HX-Trigger"].ToString());
    }

    [Fact]
    public async Task EditCountry()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Countries.Add(ControllerTestContext.CreateCountry());
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx, useSieve: false);

        var result = await controller.Edit("HR", new CountryViewModel
        {
            Code = "HR",
            Alpha3 = "HRV",
            Name = "Republic of Croatia",
            Translations = []
        });

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_CountryRow", partial.ViewName);
        Assert.Equal("Republic of Croatia", (await ctx.Countries.SingleAsync()).Name);
    }

    [Fact]
    public async Task DeleteCountry()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Countries.Add(ControllerTestContext.CreateCountry());
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx);

        var result = await controller.Delete("HR", ControllerTestContext.EmptySieveModel());

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_CountriesList", partial.ViewName);
        Assert.Empty(ctx.Countries);
        Assert.Contains("was deleted successfully", controller.Response.Headers["HX-Trigger"].ToString());
    }

    [Fact]
    public async Task ReturnConflictWhenDeletingCountryWithRelatedPeople()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Countries.Add(ControllerTestContext.CreateCountry());
        ctx.People.Add(ControllerTestContext.CreatePerson());
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx);

        var result = await controller.Delete("HR", ControllerTestContext.EmptySieveModel());

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(409, controller.Response.StatusCode);
        Assert.Equal("The country cannot be deleted because related people exist.", content.Content);
    }

    private static CountriesController CreateController(EventsContext ctx, bool useSieve = true)
    {
        return new CountriesController(
            ctx,
            useSieve ? ControllerTestContext.CreateSieveProcessor() : null!,
            ControllerTestContext.CreatePagingOptions())
            .WithTempData();
    }
}
