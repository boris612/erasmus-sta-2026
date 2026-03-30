using System.ComponentModel.DataAnnotations;
using Sieve.Attributes;

namespace Events.MVC.Models.Sports;

public class SportViewModel
{
    [Sieve(CanSort = true)]
    public int Id { get; set; }

    [Display(Name = "Name")]
    [Required]
    [StringLength(100)]
    [Sieve(CanSort = true, CanFilter = true)]
    public string Name { get; set; } = string.Empty;
}
