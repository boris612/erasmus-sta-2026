using System.Text.Json;
using Events.EFModel.Models;
using Events.MVC.Models;
using Events.MVC.Models.Registrations;
using Events.MVC.Util.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sieve.Models;
using Sieve.Services;

namespace Events.MVC.Controllers;

public class RegistrationsController : Controller
{
    private readonly EventsContext ctx;
    private readonly ISieveProcessor sieveProcessor;
    private readonly PagingSettings pagingSettings;

    public RegistrationsController(EventsContext ctx, ISieveProcessor sieveProcessor, IOptions<PagingSettings> pagingSettings)
    {
        this.ctx = ctx;
        this.sieveProcessor = sieveProcessor;
        this.pagingSettings = pagingSettings.Value;
    }

    public async Task<IActionResult> Index(int? eventId, SieveModel sieveModel)
    {
        var events = await GetEventOptionsAsync(eventId);
        if (events.Count == 0)
        {
            TempData["ToastVariant"] = "error";
            TempData["ToastTitle"] = "Error";
            TempData["ToastMessage"] = "At least one event must be created before adding registrations.";
            return RedirectToAction("Index", "Events");
        }

        if (!await ctx.Sports.AsNoTracking().AnyAsync())
        {
            TempData["ToastVariant"] = "error";
            TempData["ToastTitle"] = "Error";
            TempData["ToastMessage"] = "At least one sport must be created before adding registrations.";
            return RedirectToAction("Index", "Sports");
        }

        if (!await ctx.People.AsNoTracking().AnyAsync())
        {
            TempData["ToastVariant"] = "error";
            TempData["ToastTitle"] = "Error";
            TempData["ToastMessage"] = "At least one person must be created before adding registrations.";
            return RedirectToAction("Index", "People");
        }

        var selectedEventId = eventId ?? int.Parse(events[0].Value);
        MarkSelectedEvent(events, selectedEventId);
        var viewModel = await BuildPageViewModelAsync(selectedEventId, sieveModel, events);

        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return PartialView("_RegistrationsPanel", viewModel);
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Row(int id, int eventId)
    {
        var registration = await ctx.Registrations
            .AsNoTracking()
            .Where(r => r.Id == id && r.EventId == eventId)
            .Select(r => new RegistrationViewModel
            {
                Id = r.Id,
                EventId = r.EventId,
                PersonId = r.PersonId,
                SportId = r.SportId,
                PersonName = r.Person.FirstName + " " + r.Person.LastName,
                PersonTranscription = r.Person.FirstNameTranscription + " " + r.Person.LastNameTranscription,
                CountryCode = r.Person.CountryCode,
                CountryName = r.Person.CountryCodeNavigation.Name,
                SportName = r.Sport.Name,
                RegisteredAt = r.RegisteredAt
            })
            .FirstOrDefaultAsync();

        if (registration is null)
        {
            return NotFound();
        }

        return PartialView("_RegistrationRow", registration);
    }

    [HttpGet]
    public async Task<IActionResult> EditRow(int id, int eventId)
    {
        var registration = await ctx.Registrations
            .AsNoTracking()
            .Where(r => r.Id == id && r.EventId == eventId)
            .Select(r => new RegistrationViewModel
            {
                Id = r.Id,
                EventId = r.EventId,
                PersonId = r.PersonId,
                SportId = r.SportId,
                RegisteredAt = r.RegisteredAt
            })
            .FirstOrDefaultAsync();

        if (registration is null)
        {
            return NotFound();
        }

        await PopulateRegistrationOptionsAsync(registration);
        return PartialView("_RegistrationEditRow", registration);
    }

    [HttpGet]
    public async Task<IActionResult> PersonSuggestions(string? personLookup, string? countryFilter)
    {
        if (string.IsNullOrWhiteSpace(personLookup))
        {
            return PartialView("_PersonSuggestions", Array.Empty<SelectListItem>());
        }

        var searchTerm = personLookup.Trim().ToLowerInvariant();
        var query = ctx.People
            .AsNoTracking()
            .Where(p =>
                p.FirstNameTranscription.ToLower().Contains(searchTerm) ||
                p.LastNameTranscription.ToLower().Contains(searchTerm) ||
                (p.FirstNameTranscription + " " + p.LastNameTranscription).ToLower().Contains(searchTerm))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(countryFilter))
        {
            query = query.Where(p => p.CountryCode == countryFilter);
        }

        var suggestions = await query
            .OrderBy(p => p.LastName)
            .ThenBy(p => p.FirstName)
            .Take(10)
            .Select(p => new SelectListItem
            {
                Value = p.Id.ToString(),
                Text = p.FirstName + " " + p.LastName + "|" + p.FirstNameTranscription + " " + p.LastNameTranscription
            })
            .ToListAsync();

        return PartialView("_PersonSuggestions", suggestions);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(RegistrationViewModel model, SieveModel sieveModel)
    {
        if (!await CanCreateRegistrationsAsync())
        {
            Response.StatusCode = StatusCodes.Status409Conflict;
            return Content("At least one event, one person, and one sport are required before adding registrations.");
        }

        if (!ModelState.IsValid)
        {
            await PopulateRegistrationOptionsAsync(model);
            Response.Headers["HX-Retarget"] = "#create-registration-form";
            Response.Headers["HX-Reswap"] = "outerHTML";
            return PartialView("_CreateRegistrationForm", model);
        }

        var registration = new Registration
        {
            EventId = model.EventId,
            PersonId = model.PersonId,
            SportId = model.SportId
        };

        ctx.Registrations.Add(registration);
        await ctx.SaveChangesAsync();

        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["registration-created"] = true,
            ["show-toast"] = new
            {
                variant = "success",
                title = "Success",
                message = "Registration was added successfully."
            }
        });

        var viewModel = await BuildPageViewModelAsync(model.EventId, sieveModel);
        return PartialView("_RegistrationsPanel", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, RegistrationViewModel model)
    {
        if (id != model.Id)
        {
            return BadRequest();
        }

        if (!ModelState.IsValid)
        {
            await PopulateRegistrationOptionsAsync(model);
            return PartialView("_RegistrationEditRow", model);
        }

        var registration = await ctx.Registrations.FirstOrDefaultAsync(r => r.Id == id && r.EventId == model.EventId);
        if (registration is null)
        {
            return NotFound();
        }

        registration.PersonId = model.PersonId;
        registration.SportId = model.SportId;
        await ctx.SaveChangesAsync();

        var rowModel = await ctx.Registrations
            .AsNoTracking()
            .Where(r => r.Id == id)
            .Select(r => new RegistrationViewModel
            {
                Id = r.Id,
                EventId = r.EventId,
                PersonId = r.PersonId,
                SportId = r.SportId,
                PersonName = r.Person.FirstName + " " + r.Person.LastName,
                PersonTranscription = r.Person.FirstNameTranscription + " " + r.Person.LastNameTranscription,
                CountryCode = r.Person.CountryCode,
                CountryName = r.Person.CountryCodeNavigation.Name,
                SportName = r.Sport.Name,
                RegisteredAt = r.RegisteredAt
            })
            .FirstAsync();

        return PartialView("_RegistrationRow", rowModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, int eventId, SieveModel sieveModel)
    {
        var registration = await ctx.Registrations.FirstOrDefaultAsync(r => r.Id == id && r.EventId == eventId);
        if (registration is null)
        {
            return NotFound();
        }

        ctx.Registrations.Remove(registration);
        await ctx.SaveChangesAsync();

        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["show-toast"] = new
            {
                variant = "success",
                title = "Success",
                message = "Registration was deleted successfully."
            }
        });

        var viewModel = await BuildPageViewModelAsync(eventId, sieveModel);
        return PartialView("_RegistrationsPanel", viewModel);
    }

    private async Task<RegistrationsPageViewModel> BuildPageViewModelAsync(int selectedEventId, SieveModel sieveModel, List<SelectListItem>? eventOptions = null)
    {
        sieveModel.SetDefaultPagingAndSorting(pagingSettings.PageSize, "RegisteredAt");
        var normalizedFilters = sieveModel.Filters?.Trim() ?? string.Empty;
        var nameFilter = SieveModelExtensions.ExtractFilterValue(normalizedFilters, "PersonTranscription");
        var countryFilter = SieveModelExtensions.ExtractFilterValue(normalizedFilters, "CountryCode", "==");

        var baseQuery = ctx.Registrations
            .AsNoTracking()
            .Where(r => r.EventId == selectedEventId)
            .Select(r => new RegistrationViewModel
            {
                Id = r.Id,
                EventId = r.EventId,
                PersonId = r.PersonId,
                SportId = r.SportId,
                PersonName = r.Person.FirstName + " " + r.Person.LastName,
                PersonTranscription = r.Person.FirstNameTranscription + " " + r.Person.LastNameTranscription,
                CountryCode = r.Person.CountryCode,
                CountryName = r.Person.CountryCodeNavigation.Name,
                SportName = r.Sport.Name,
                RegisteredAt = r.RegisteredAt
            });

        var totalCount = await baseQuery.CountAsync();
        var filteredQuery = sieveProcessor.Apply(sieveModel, baseQuery, applyFiltering: true, applySorting: false, applyPagination: false);
        var filteredCount = await filteredQuery.CountAsync();

        var pagingInfo = new PagingInfo
        {
            FilteredItemsCount = filteredCount,
            TotalItemsCount = totalCount,
            ItemsPerPage = sieveModel.PageSize!.Value,
            CurrentPage = sieveModel.Page!.Value,
            Sorts = sieveModel.Sorts ?? "RegisteredAt",
            Filters = normalizedFilters,
            NameFilter = nameFilter
        };

        if (pagingInfo.CurrentPage > pagingInfo.TotalPages)
        {
            pagingInfo.CurrentPage = pagingInfo.TotalPages;
            sieveModel.Page = pagingInfo.CurrentPage;
        }

        var sortedQuery = sieveProcessor.Apply(sieveModel, baseQuery, applyFiltering: true, applySorting: true, applyPagination: false);
        var registrations = await PagedList<RegistrationViewModel>.CreateAsync(sortedQuery, pagingInfo);

        eventOptions ??= await GetEventOptionsAsync(selectedEventId);
        MarkSelectedEvent(eventOptions, selectedEventId);
        var selectedEventName = eventOptions.FirstOrDefault(e => e.Selected)?.Text ?? string.Empty;
        var canCreate = await CanCreateRegistrationsAsync();
        var countryOptions = await GetCountryOptionsAsync(countryFilter);

        var createModel = new RegistrationViewModel
        {
            EventId = selectedEventId
        };
        await PopulateRegistrationOptionsAsync(createModel);

        return new RegistrationsPageViewModel
        {
            SelectedEventId = selectedEventId,
            SelectedEventName = selectedEventName,
            EventOptions = eventOptions,
            CountryOptions = countryOptions,
            CountryFilter = countryFilter,
            Registrations = registrations,
            CreateModel = createModel,
            CanCreate = canCreate,
            CreateDisabledMessage = canCreate ? null : "At least one event, one person, and one sport are required before adding registrations."
        };
    }

    private async Task PopulateRegistrationOptionsAsync(RegistrationViewModel model)
    {
        if (model.PersonId > 0)
        {
            model.PersonLookup = await ctx.People
                .AsNoTracking()
                .Where(p => p.Id == model.PersonId)
                .Select(p => p.FirstName + " " + p.LastName + " (" + p.FirstNameTranscription + " " + p.LastNameTranscription + ")")
                .FirstOrDefaultAsync() ?? string.Empty;
        }
        else
        {
            model.PersonLookup = string.Empty;
        }

        model.SportOptions = await ctx.Sports
            .AsNoTracking()
            .OrderBy(s => s.Name)
            .Select(s => new SelectListItem
            {
                Value = s.Id.ToString(),
                Text = s.Name,
                Selected = s.Id == model.SportId
            })
            .ToListAsync();
    }

    private async Task<List<SelectListItem>> GetEventOptionsAsync(int? selectedEventId)
    {
        var events = await ctx.Events
            .AsNoTracking()
            .OrderBy(e => e.EventDate)
            .ThenBy(e => e.Name)
            .ToListAsync();

        return events
            .Select(e => new SelectListItem
            {
                Value = e.Id.ToString(),
                Text = $"{e.Name} ({e.EventDate:dd.MM.yyyy.})",
                Selected = e.Id == selectedEventId
            })
            .ToList();
    }

    private async Task<bool> CanCreateRegistrationsAsync()
    {
        return await ctx.Events.AsNoTracking().AnyAsync()
            && await ctx.People.AsNoTracking().AnyAsync()
            && await ctx.Sports.AsNoTracking().AnyAsync();
    }

    private async Task<List<SelectListItem>> GetCountryOptionsAsync(string? selectedCountryCode)
    {
        return await ctx.Countries
            .AsNoTracking()
            .OrderBy(c => c.Name)
            .Select(c => new SelectListItem
            {
                Value = c.Code,
                Text = c.Name,
                Selected = c.Code == selectedCountryCode
            })
            .ToListAsync();
    }

    private static void MarkSelectedEvent(IEnumerable<SelectListItem> events, int selectedEventId)
    {
        foreach (var eventOption in events)
        {
            eventOption.Selected = string.Equals(eventOption.Value, selectedEventId.ToString(), StringComparison.Ordinal);
        }
    }
}
