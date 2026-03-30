using System.ComponentModel.DataAnnotations;

namespace Events.MVC.Models.Countries;

public class CountryTranslationViewModel
{
    [Display(Name = "Language")]
    [StringLength(10)]
    public string LanguageCode { get; set; } = string.Empty;

    [Display(Name = "Translation")]
    [StringLength(200)]
    public string Name { get; set; } = string.Empty;
}
