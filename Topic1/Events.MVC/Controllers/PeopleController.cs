using System.Text.Json;
using Events.EFModel.Models;
using Events.MVC.Models;
using Events.MVC.Models.People;
using Events.MVC.Util.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sieve.Models;
using Sieve.Services;

namespace Events.MVC.Controllers;

public class PeopleController : Controller
{
    private readonly EventsContext ctx;
    private readonly ISieveProcessor sieveProcessor;
    private readonly PagingSettings pagingSettings;

    public PeopleController(EventsContext ctx, ISieveProcessor sieveProcessor, IOptions<PagingSettings> pagingSettings)
    {
        this.ctx = ctx;
        this.sieveProcessor = sieveProcessor;
        this.pagingSettings = pagingSettings.Value;
    }

    public async Task<IActionResult> Index(SieveModel sieveModel)
    {
        if (!await ctx.Countries.AsNoTracking().AnyAsync())
        {
            TempData["ToastVariant"] = "error";
            TempData["ToastTitle"] = "Error";
            TempData["ToastMessage"] = "At least one country must be created before adding people.";
            return RedirectToAction("Index", "Countries");
        }

        var viewModel = await BuildPeopleListAsync(sieveModel);
        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return PartialView("_PeopleList", viewModel);
        }

        ViewData["CreatePersonModel"] = new PersonViewModel
        {
            BirthDate = DateOnly.FromDateTime(DateTime.Today),
            CountryOptions = await GetCountryOptionsAsync()
        };
        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Row(int id)
    {
        var person = await ctx.People
            .AsNoTracking()
            .Select(p => new PersonViewModel
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                FullName = p.FirstName + " " + p.LastName,
                FullNameTranscription = p.FirstNameTranscription + " " + p.LastNameTranscription,
                Email = p.Email,
                BirthDate = p.BirthDate,
                CountryName = p.CountryCodeNavigation.Name,
                RegistrationsCount = p.Registrations.Count
            })
            .FirstOrDefaultAsync(p => p.Id == id);

        if (person is null)
        {
            return NotFound();
        }

        return PartialView("_PersonRow", person);
    }

    [HttpGet]
    public async Task<IActionResult> EditRow(int id)
    {
        var person = await ctx.People
            .AsNoTracking()
            .Select(p => new PersonViewModel
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                FirstNameTranscription = p.FirstNameTranscription,
                LastNameTranscription = p.LastNameTranscription,
                AddressLine = p.AddressLine,
                PostalCode = p.PostalCode,
                City = p.City,
                AddressCountry = p.AddressCountry,
                Email = p.Email,
                ContactPhone = p.ContactPhone,
                BirthDate = p.BirthDate,
                DocumentNumber = p.DocumentNumber,
                CountryCode = p.CountryCode
            })
            .FirstOrDefaultAsync(p => p.Id == id);

        if (person is null)
        {
            return NotFound();
        }

        person.CountryOptions = await GetCountryOptionsAsync(person.CountryCode);
        return PartialView("_PersonEditRow", person);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(PersonViewModel model, SieveModel sieveModel)
    {
        if (!ModelState.IsValid)
        {
            model.CountryOptions = await GetCountryOptionsAsync(model.CountryCode);
            Response.Headers["HX-Retarget"] = "#create-person-form";
            Response.Headers["HX-Reswap"] = "outerHTML";
            return PartialView("_CreatePersonForm", model);
        }

        var person = new Person
        {
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            FirstNameTranscription = model.FirstNameTranscription.Trim(),
            LastNameTranscription = model.LastNameTranscription.Trim(),
            AddressLine = model.AddressLine.Trim(),
            PostalCode = model.PostalCode.Trim(),
            City = model.City.Trim(),
            AddressCountry = model.AddressCountry.Trim(),
            Email = model.Email.Trim(),
            ContactPhone = model.ContactPhone.Trim(),
            BirthDate = model.BirthDate,
            DocumentNumber = model.DocumentNumber.Trim(),
            CountryCode = model.CountryCode.Trim().ToUpperInvariant()
        };

        ctx.People.Add(person);
        await ctx.SaveChangesAsync();

        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["person-created"] = true,
            ["show-toast"] = new
            {
                variant = "success",
                title = "Success",
                message = $"Person '{person.FirstName} {person.LastName}' was added successfully."
            }
        });

        var viewModel = await BuildPeopleListAsync(sieveModel);
        return PartialView("_PeopleList", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, PersonViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            model.CountryOptions = await GetCountryOptionsAsync(model.CountryCode);
            return PartialView("_PersonEditRow", model);
        }

        var person = await ctx.People.FirstOrDefaultAsync(p => p.Id == id);
        if (person is null)
        {
            return NotFound();
        }

        person.FirstName = model.FirstName.Trim();
        person.LastName = model.LastName.Trim();
        person.FirstNameTranscription = model.FirstNameTranscription.Trim();
        person.LastNameTranscription = model.LastNameTranscription.Trim();
        person.AddressLine = model.AddressLine.Trim();
        person.PostalCode = model.PostalCode.Trim();
        person.City = model.City.Trim();
        person.AddressCountry = model.AddressCountry.Trim();
        person.Email = model.Email.Trim();
        person.ContactPhone = model.ContactPhone.Trim();
        person.BirthDate = model.BirthDate;
        person.DocumentNumber = model.DocumentNumber.Trim();
        person.CountryCode = model.CountryCode.Trim().ToUpperInvariant();
        await ctx.SaveChangesAsync();

        var rowModel = await ctx.People
            .AsNoTracking()
            .Where(p => p.Id == id)
            .Select(p => new PersonViewModel
            {
                Id = p.Id,
                FirstName = p.FirstName,
                LastName = p.LastName,
                FullName = p.FirstName + " " + p.LastName,
                FullNameTranscription = p.FirstNameTranscription + " " + p.LastNameTranscription,
                Email = p.Email,
                BirthDate = p.BirthDate,
                CountryName = p.CountryCodeNavigation.Name,
                RegistrationsCount = p.Registrations.Count
            })
            .FirstAsync();

        return PartialView("_PersonRow", rowModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, SieveModel sieveModel)
    {
        var person = await ctx.People
            .Include(p => p.Registrations)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (person is null)
        {
            return NotFound();
        }

        if (person.Registrations.Count > 0)
        {
            Response.StatusCode = StatusCodes.Status409Conflict;
            return Content("The person cannot be deleted because registrations exist.");
        }

        ctx.People.Remove(person);
        var deletedName = $"{person.FirstName} {person.LastName}";
        await ctx.SaveChangesAsync();

        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["show-toast"] = new
            {
                variant = "success",
                title = "Success",
                message = $"Person '{deletedName}' was deleted successfully."
            }
        });

        var viewModel = await BuildPeopleListAsync(sieveModel);
        return PartialView("_PeopleList", viewModel);
    }

    private async Task<PagedList<PersonViewModel>> BuildPeopleListAsync(SieveModel sieveModel)
    {
        sieveModel.SetDefaultPagingAndSorting(pagingSettings.PageSize, "FullName");
        var normalizedFilters = sieveModel.Filters?.Trim() ?? string.Empty;
        var nameFilter = SieveModelExtensions.ExtractFilterValue(normalizedFilters, "FullNameTranscription");

        var baseQuery = ctx.People
            .AsNoTracking()
            .Select(p => new PersonViewModel
            {
                Id = p.Id,
                FullName = p.FirstName + " " + p.LastName,
                FullNameTranscription = p.FirstNameTranscription + " " + p.LastNameTranscription,
                Email = p.Email,
                BirthDate = p.BirthDate,
                CountryName = p.CountryCodeNavigation.Name,
                RegistrationsCount = p.Registrations.Count
            });

        var totalCount = await baseQuery.CountAsync();
        var filteredQuery = sieveProcessor.Apply(
            sieveModel,
            baseQuery,
            applyFiltering: true,
            applySorting: false,
            applyPagination: false);
        var filteredCount = await filteredQuery.CountAsync();

        var pagingInfo = new PagingInfo
        {
            FilteredItemsCount = filteredCount,
            TotalItemsCount = totalCount,
            ItemsPerPage = sieveModel.PageSize!.Value,
            CurrentPage = sieveModel.Page!.Value,
            Sorts = sieveModel.Sorts ?? "FullName",
            Filters = normalizedFilters,
            NameFilter = nameFilter
        };

        if (pagingInfo.CurrentPage > pagingInfo.TotalPages)
        {
            pagingInfo.CurrentPage = pagingInfo.TotalPages;
            sieveModel.Page = pagingInfo.CurrentPage;
        }

        var sortedQuery = sieveProcessor.Apply(sieveModel, baseQuery, applyFiltering: true, applySorting: true, applyPagination: false);
        return await PagedList<PersonViewModel>.CreateAsync(sortedQuery, pagingInfo);
    }

    private async Task<List<SelectListItem>> GetCountryOptionsAsync(string? selectedCode = null)
    {
        return await ctx.Countries
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Code,
                Text = c.Name,
                Selected = c.Code == selectedCode
            })
            .ToListAsync();
    }

}
