using Microsoft.AspNetCore.Mvc.Rendering;

namespace Events.MVC.Models.Registrations;

public class RegistrationsPageViewModel
{
    public int SelectedEventId { get; set; }

    public string SelectedEventName { get; set; } = string.Empty;

    public List<SelectListItem> EventOptions { get; set; } = [];

    public List<SelectListItem> CountryOptions { get; set; } = [];

    public string CountryFilter { get; set; } = string.Empty;

    public PagedList<RegistrationViewModel> Registrations { get; set; } = new([], new PagingInfo
    {
        ItemsPerPage = 10,
        CurrentPage = 1
    });

    public RegistrationViewModel CreateModel { get; set; } = new();

    public bool CanCreate { get; set; }

    public string? CreateDisabledMessage { get; set; }
}
