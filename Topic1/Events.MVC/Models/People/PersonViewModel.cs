using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using Sieve.Attributes;

namespace Events.MVC.Models.People;

public class PersonViewModel
{
    [Sieve(CanSort = true)]
    public int Id { get; set; }

    [Display(Name = "First name")]
    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = string.Empty;

    [Display(Name = "Last name")]
    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = string.Empty;

    [Display(Name = "First name (transcription)")]
    [Required]
    [StringLength(100)]
    public string FirstNameTranscription { get; set; } = string.Empty;

    [Display(Name = "Last name (transcription)")]
    [Required]
    [StringLength(100)]
    public string LastNameTranscription { get; set; } = string.Empty;

    [Display(Name = "Address")]
    [Required]
    [StringLength(200)]
    public string AddressLine { get; set; } = string.Empty;

    [Display(Name = "Postal code")]
    [Required]
    [StringLength(20)]
    public string PostalCode { get; set; } = string.Empty;

    [Display(Name = "City")]
    [Required]
    [StringLength(100)]
    public string City { get; set; } = string.Empty;

    [Display(Name = "Address country")]
    [Required]
    [StringLength(100)]
    public string AddressCountry { get; set; } = string.Empty;

    [Display(Name = "E-mail")]
    [Required]
    [EmailAddress]
    [StringLength(255)]
    [Sieve(CanSort = true)]
    public string Email { get; set; } = string.Empty;

    [Display(Name = "Phone")]
    [Required]
    [StringLength(50)]
    public string ContactPhone { get; set; } = string.Empty;

    [Display(Name = "Birth date")]
    [Required]
    [Sieve(CanSort = true)]
    public DateOnly BirthDate { get; set; } = DateOnly.FromDateTime(DateTime.Today);

    [Display(Name = "Document number")]
    [Required]
    [StringLength(50)]
    public string DocumentNumber { get; set; } = string.Empty;

    [Display(Name = "Country")]
    [Required]
    [StringLength(3)]
    public string CountryCode { get; set; } = string.Empty;

    [Display(Name = "Full name")]
    [Sieve(CanSort = true, CanFilter = true)]
    public string FullName { get; set; } = string.Empty;

    [Display(Name = "Full name (transcription)")]
    [Sieve(CanFilter = true)]
    public string FullNameTranscription { get; set; } = string.Empty;

    [Display(Name = "Country")]
    [Sieve(CanSort = true)]
    public string CountryName { get; set; } = string.Empty;

    [Display(Name = "Registrations")]
    [Sieve(CanSort = true)]
    public int RegistrationsCount { get; set; }

    public List<SelectListItem> CountryOptions { get; set; } = [];
}
