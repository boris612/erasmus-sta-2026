using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sieve.Attributes;

namespace Events.MVC.Models.Registrations;

public class RegistrationViewModel
{
    [Sieve(CanSort = true)]
    public int Id { get; set; }

    [Required]
    [Range(1, int.MaxValue)]
    public int EventId { get; set; }

    [Display(Name = "Person")]
    [Required]
    [Range(1, int.MaxValue)]
    public int PersonId { get; set; }

    [Display(Name = "Sport")]
    [Required]
    [Range(1, int.MaxValue)]
    public int SportId { get; set; }

    [Display(Name = "Person")]
    public string PersonLookup { get; set; } = string.Empty;

    [Display(Name = "Person")]
    [Sieve(CanSort = true, CanFilter = true)]
    public string PersonName { get; set; } = string.Empty;

    [Display(Name = "Transcription")]
    [Sieve(CanFilter = true)]
    public string PersonTranscription { get; set; } = string.Empty;

    [Display(Name = "Country")]
    [Sieve(CanFilter = true)]
    public string CountryCode { get; set; } = string.Empty;

    public string CountryName { get; set; } = string.Empty;

    [Display(Name = "Sport")]
    [Sieve(CanSort = true)]
    public string SportName { get; set; } = string.Empty;

    [Display(Name = "Registered at")]
    [Sieve(CanSort = true)]
    public DateTime RegisteredAt { get; set; }

    public List<SelectListItem> SportOptions { get; set; } = [];
}
