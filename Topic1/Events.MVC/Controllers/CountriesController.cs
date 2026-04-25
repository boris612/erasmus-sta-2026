using System.Text.Json;
using Events.EFModel.Models;
using Events.MVC.Models;
using Events.MVC.Models.Countries;
using Events.MVC.Util.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Sieve.Models;
using Sieve.Services;

namespace Events.MVC.Controllers;

public class CountriesController : Controller
{
    private readonly EventsContext ctx;
    private readonly ISieveProcessor sieveProcessor;
    private readonly PagingSettings pagingSettings;

    public CountriesController(EventsContext ctx, ISieveProcessor sieveProcessor, IOptions<PagingSettings> pagingSettings)
    {
        this.ctx = ctx;
        this.sieveProcessor = sieveProcessor;
        this.pagingSettings = pagingSettings.Value;
    }

    public async Task<IActionResult> Index(SieveModel sieveModel)
    {
        var viewModel = await BuildCountriesListAsync(sieveModel);
        if (Request.Headers.ContainsKey("HX-Request"))
        {
            return PartialView("_CountriesList", viewModel);
        }

        return View(viewModel);
    }

    [HttpGet]
    public async Task<IActionResult> Row(string id)
    {
        var country = await ctx.Countries
            .AsNoTracking()
            .Select(c => new CountryViewModel
            {
                Code = c.Code,
                Alpha3 = c.Alpha3,
                Name = c.Name
            })
            .FirstOrDefaultAsync(c => c.Code == id);

        if (country is null)
        {
            return NotFound();
        }

        return PartialView("_CountryRow", country);
    }

    [HttpGet]
    public async Task<IActionResult> EditRow(string id)
    {
        var country = await ctx.Countries
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Code == id);

        if (country is null)
        {
            return NotFound();
        }

        return PartialView("_CountryEditRow", MapCountryToViewModel(country, includeTranslations: true));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CountryViewModel model, SieveModel sieveModel)
    {
        NormalizeTranslations(model);
        ValidateTranslations(model);

        if (!ModelState.IsValid)
        {
            Response.Headers["HX-Retarget"] = "#create-country-form";
            Response.Headers["HX-Reswap"] = "outerHTML";
            return PartialView("_CreateCountryForm", model);
        }

        var country = new Country
        {
            Code = model.Code.Trim().ToUpperInvariant(),
            Alpha3 = model.Alpha3.Trim().ToUpperInvariant(),
            Name = model.Name.Trim(),
            Translations = SerializeTranslations(model.Translations)
        };

        ctx.Countries.Add(country);
        await ctx.SaveChangesAsync();

        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["country-created"] = true,
            ["show-toast"] = new
            {
                variant = "success",
                title = "Success",
                message = $"Country '{country.Name}' was added successfully."
            }
        });

        var viewModel = await BuildCountriesListAsync(sieveModel);
        return PartialView("_CountriesList", viewModel);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, CountryViewModel model)
    {
        if (!string.Equals(id, model.Code, StringComparison.OrdinalIgnoreCase))
        {
            return BadRequest();
        }

        NormalizeTranslations(model);
        ValidateTranslations(model);

        if (!ModelState.IsValid)
        {
            return PartialView("_CountryEditRow", model);
        }

        var country = await ctx.Countries.FirstOrDefaultAsync(c => c.Code == id);
        if (country is null)
        {
            return NotFound();
        }

        country.Alpha3 = model.Alpha3.Trim().ToUpperInvariant();
        country.Name = model.Name.Trim();
        country.Translations = SerializeTranslations(model.Translations);
        await ctx.SaveChangesAsync();

        return PartialView("_CountryRow", new CountryViewModel
        {
            Code = country.Code,
            Alpha3 = country.Alpha3,
            Name = country.Name
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id, SieveModel sieveModel)
    {
        var country = await ctx.Countries
            .Include(c => c.People)
            .FirstOrDefaultAsync(c => c.Code == id);

        if (country is null)
        {
            return NotFound();
        }

        if (country.People.Count > 0)
        {
            Response.StatusCode = StatusCodes.Status409Conflict;
            return Content("The country cannot be deleted because related people exist.");
        }

        ctx.Countries.Remove(country);
        var deletedName = country.Name;
        await ctx.SaveChangesAsync();

        Response.Headers["HX-Trigger"] = JsonSerializer.Serialize(new Dictionary<string, object?>
        {
            ["show-toast"] = new
            {
                variant = "success",
                title = "Success",
                message = $"Country '{deletedName}' was deleted successfully."
            }
        });

        var viewModel = await BuildCountriesListAsync(sieveModel);
        return PartialView("_CountriesList", viewModel);
    }

    private async Task<PagedList<CountryViewModel>> BuildCountriesListAsync(SieveModel sieveModel)
    {
        sieveModel.SetDefaultPagingAndSorting(pagingSettings.PageSize, "Name");
        var normalizedFilters = sieveModel.Filters?.Trim() ?? string.Empty;
        var nameFilter = SieveModelExtensions.ExtractFilterValue(normalizedFilters, "Name");

        var baseQuery = ctx.Countries
            .AsNoTracking()
            .Select(c => new CountryViewModel
            {
                Code = c.Code,
                Alpha3 = c.Alpha3,
                Name = c.Name
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
            Sorts = sieveModel.Sorts ?? "Name",
            Filters = normalizedFilters,
            NameFilter = nameFilter
        };

        if (pagingInfo.CurrentPage > pagingInfo.TotalPages)
        {
            pagingInfo.CurrentPage = pagingInfo.TotalPages;
            sieveModel.Page = pagingInfo.CurrentPage;
        }

        var sortedQuery = sieveProcessor.Apply(sieveModel, baseQuery, applyFiltering: true, applySorting: true, applyPagination: false);
        return await PagedList<CountryViewModel>.CreateAsync(sortedQuery, pagingInfo);
    }

    private void ValidateTranslations(CountryViewModel model)
    {
        var languages = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasErrors = false;

        for (var i = 0; i < model.Translations.Count; i++)
        {
            var translation = model.Translations[i] ?? new CountryTranslationViewModel();
            var language = translation.LanguageCode?.Trim() ?? string.Empty;
            var name = translation.Name?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(language) && string.IsNullOrEmpty(name))
            {
                continue;
            }

            if (string.IsNullOrEmpty(language))
            {
                ModelState.AddModelError($"Translations[{i}].LanguageCode", "Enter a language code.");
                hasErrors = true;
            }

            if (string.IsNullOrEmpty(name))
            {
                ModelState.AddModelError($"Translations[{i}].Name", "Enter a translation.");
                hasErrors = true;
            }

            if (!string.IsNullOrEmpty(language) && !languages.Add(language))
            {
                ModelState.AddModelError($"Translations[{i}].LanguageCode", "The language code has already been entered.");
                hasErrors = true;
            }
        }

        if (hasErrors)
        {
            ModelState.AddModelError(string.Empty, "Check the translations. Every row must have both a language code and a translation, and language codes must be unique.");
        }
    }

    private static void NormalizeTranslations(CountryViewModel model)
    {
        model.Translations ??= [];

        model.Translations = model.Translations
            .Where(t => t is not null)
            .Select(t => new CountryTranslationViewModel
            {
                LanguageCode = t!.LanguageCode?.Trim().ToLowerInvariant() ?? string.Empty,
                Name = t.Name?.Trim() ?? string.Empty
            })
            .ToList();
    }

    private static CountryViewModel MapCountryToViewModel(Country country, bool includeTranslations)
    {
        return new CountryViewModel
        {
            Code = country.Code,
            Alpha3 = country.Alpha3,
            Name = country.Name,
            Translations = includeTranslations ? ParseTranslations(country.Translations) : []
        };
    }

    private static List<CountryTranslationViewModel> ParseTranslations(string? translationsJson)
    {
        if (string.IsNullOrWhiteSpace(translationsJson))
        {
            return [];
        }

        try
        {
            var translations = JsonSerializer.Deserialize<Dictionary<string, string>>(translationsJson);
            if (translations is null)
            {
                return [];
            }

            return translations
                .OrderBy(t => t.Key, StringComparer.OrdinalIgnoreCase)
                .Select(t => new CountryTranslationViewModel
                {
                    LanguageCode = t.Key,
                    Name = t.Value
                })
                .ToList();
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static string? SerializeTranslations(IEnumerable<CountryTranslationViewModel> translations)
    {
        var dictionary = translations
            .Where(t => t is not null && !string.IsNullOrWhiteSpace(t.LanguageCode) && !string.IsNullOrWhiteSpace(t.Name))
            .ToDictionary(t => t.LanguageCode.Trim().ToLowerInvariant(), t => t.Name.Trim(), StringComparer.OrdinalIgnoreCase);

        return dictionary.Count == 0 ? null : JsonSerializer.Serialize(dictionary);
    }
}
