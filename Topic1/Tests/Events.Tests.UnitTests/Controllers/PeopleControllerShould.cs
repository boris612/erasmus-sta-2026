using Events.EFModel.Models;
using Events.MVC.Controllers;
using Events.MVC.Models;
using Events.MVC.Models.People;
using Events.Tests.UnitTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Sieve.Models;

namespace Events.Tests.UnitTests.Controllers;

public class PeopleControllerShould
{
    [Fact]
    public async Task RedirectToCountriesAndSetToastWhenIndexIsRequestedWithoutCountries()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        var controller = CreateController(ctx, useSieve: false);

        var result = await controller.Index(new SieveModel());

        var redirect = Assert.IsType<RedirectToActionResult>(result);
        Assert.Equal("Index", redirect.ActionName);
        Assert.Equal("Countries", redirect.ControllerName);
        Assert.Equal("At least one country must be created before adding people.", controller.TempData["ToastMessage"]);
    }

    [Fact]
    public async Task CreatePerson()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Countries.Add(ControllerTestContext.CreateCountry());
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx);

        var result = await controller.Create(
            new PersonViewModel
            {
                FirstName = "Ana",
                LastName = "Kovac",
                FirstNameTranscription = "Ana",
                LastNameTranscription = "Kovac",
                AddressLine = "Main Street 1",
                PostalCode = "10000",
                City = "Zagreb",
                AddressCountry = "Croatia",
                Email = "ana.kovac@example.com",
                ContactPhone = "+38591123456",
                BirthDate = new DateOnly(1995, 1, 1),
                DocumentNumber = "DOC-2",
                CountryCode = "hr"
            },
            ControllerTestContext.EmptySieveModel());

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_PeopleList", partial.ViewName);
        var model = Assert.IsType<PagedList<PersonViewModel>>(partial.Model);
        Assert.Contains(model.Data, p => p.FullName == "Ana Kovac" && p.CountryName == "Croatia");
        Assert.Contains("was added successfully", controller.Response.Headers["HX-Trigger"].ToString());
    }

    [Fact]
    public async Task ReturnPartialViewWithExpectedPeopleListForPersonAddedBeforeIndexRead()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Countries.Add(ControllerTestContext.CreateCountry());
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx);

        await controller.Create(
            new PersonViewModel
            {
                FirstName = "Ana",
                LastName = "Kovac",
                FirstNameTranscription = "Ana",
                LastNameTranscription = "Kovac",
                AddressLine = "Main Street 1",
                PostalCode = "10000",
                City = "Zagreb",
                AddressCountry = "Croatia",
                Email = "ana.kovac@example.com",
                ContactPhone = "+38591123456",
                BirthDate = new DateOnly(1995, 1, 1),
                DocumentNumber = "DOC-2",
                CountryCode = "hr"
            },
            ControllerTestContext.EmptySieveModel());

        controller.Request.Headers["HX-Request"] = "true";

        var result = await controller.Index(new SieveModel
        {
            Filters = "FirstName==Ana"
        });

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_PeopleList", partial.ViewName);
        var model = Assert.IsType<PagedList<PersonViewModel>>(partial.Model);
        var person = Assert.Single(model.Data);
        Assert.Equal("Ana Kovac", person.FullName);
        Assert.Equal("Croatia", person.CountryName);
    }

    [Fact]
    public async Task EditPerson()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Countries.Add(ControllerTestContext.CreateCountry());
        ctx.People.Add(ControllerTestContext.CreatePerson());
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx, useSieve: false);

        var result = await controller.Edit(1, new PersonViewModel
        {
            Id = 1,
            FirstName = "Ivan",
            LastName = "Kovac",
            FirstNameTranscription = "Ivan",
            LastNameTranscription = "Kovac",
            AddressLine = "Updated Street 2",
            PostalCode = "10000",
            City = "Zagreb",
            AddressCountry = "Croatia",
            Email = "ivan.kovac@example.com",
            ContactPhone = "+38591111222",
            BirthDate = new DateOnly(1990, 5, 1),
            DocumentNumber = "DOC-1",
            CountryCode = "HR"
        });

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_PersonRow", partial.ViewName);
        Assert.Equal("Kovac", (await ctx.People.SingleAsync()).LastName);
    }

    [Fact]
    public async Task DeletePerson()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Countries.Add(ControllerTestContext.CreateCountry());
        ctx.People.Add(ControllerTestContext.CreatePerson());
        await ctx.SaveChangesAsync();
        var controller = CreateController(ctx);

        var result = await controller.Delete(1, ControllerTestContext.EmptySieveModel());

        var partial = Assert.IsType<PartialViewResult>(result);
        Assert.Equal("_PeopleList", partial.ViewName);
        Assert.Empty(ctx.People);
        Assert.Contains("was deleted successfully", controller.Response.Headers["HX-Trigger"].ToString());
    }

    [Fact]
    public async Task FilterPeopleByTranscribedFullName()
    {
        await using var ctx = ControllerTestContext.CreateContext();
        ctx.Countries.Add(ControllerTestContext.CreateCountry());
        ctx.People.AddRange(
            ControllerTestContext.CreatePerson(id: 1, firstName: "Ђорђе", lastName: "Петровић"),
            ControllerTestContext.CreatePerson(id: 2, firstName: "Ana", lastName: "Kovac"));
        await ctx.SaveChangesAsync();

        var person = await ctx.People.SingleAsync(p => p.Id == 1);
        person.FirstNameTranscription = "Djordje";
        person.LastNameTranscription = "Petrovic";
        await ctx.SaveChangesAsync();

        var controller = CreateController(ctx);

        var result = await controller.Index(new SieveModel
        {
            Filters = "FullNameTranscription@=*Petrovic"
        });

        var view = Assert.IsType<ViewResult>(result);
        var model = Assert.IsType<PagedList<PersonViewModel>>(view.Model);
        var people = Assert.Single(model.Data);
        Assert.Equal(1, people.Id);
        Assert.Equal("Ђорђе Петровић", people.FullName);
    }

    [Fact]
    public async Task ReturnConflictWhenDeletingPersonWithRegistrations()
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

        var result = await controller.Delete(1, ControllerTestContext.EmptySieveModel());

        var content = Assert.IsType<ContentResult>(result);
        Assert.Equal(409, controller.Response.StatusCode);
        Assert.Equal("The person cannot be deleted because registrations exist.", content.Content);
    }

    private static PeopleController CreateController(EventsContext ctx, bool useSieve = true)
    {
        return new PeopleController(
            ctx,
            useSieve ? ControllerTestContext.CreateSieveProcessor() : null!,
            ControllerTestContext.CreatePagingOptions())
            .WithTempData();
    }
}
