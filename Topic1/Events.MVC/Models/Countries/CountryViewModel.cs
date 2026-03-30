using System.ComponentModel.DataAnnotations;
using Sieve.Attributes;

namespace Events.MVC.Models.Countries;

public class CountryViewModel
{
    [Display(Name = "Code")]
    [Required]
    [StringLength(3, MinimumLength = 2)]
    [Sieve(CanSort = true, CanFilter = true)]
    public string Code { get; set; } = string.Empty;

    [Display(Name = "Alpha-3")]
    [Required]
    [StringLength(3, MinimumLength = 3)]
    [Sieve(CanSort = true, CanFilter = true)]
    public string Alpha3 { get; set; } = string.Empty;

    [Display(Name = "Name")]
    [Required]
    [StringLength(100)]
    [Sieve(CanSort = true, CanFilter = true)]
    public string Name { get; set; } = string.Empty;

    public List<CountryTranslationViewModel> Translations { get; set; } = [];
}
