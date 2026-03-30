using System.ComponentModel.DataAnnotations;
using Sieve.Attributes;

namespace Events.MVC.Models.Events;

public class EventViewModel
{
    [Display(Name = "ID")]
    [Sieve(CanSort = true)]
    public int Id { get; set; }

    [Display(Name = "Name")]
    [Required]
    [StringLength(150)]
    [Sieve(CanSort = true, CanFilter = true)]
    public string Name { get; set; } = string.Empty;

    [Display(Name = "Date")]
    [DataType(DataType.Date)]
    [Sieve(CanSort = true)]
    public DateOnly EventDate { get; set; }

    [Display(Name = "Participants")]
    [Sieve(CanSort = true)]
    public int ParticipantsCount { get; set; }
}
