using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using InviteStudio.Application.Entities;
using InviteStudio.Application.Enums;
using InviteStudio.Application.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace InviteStudio.Web.Pages.Events
{
    public class CreateModel : PageModel
    {
        private readonly InviteStudioDbContext _dbContext;

        public CreateModel(InviteStudioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        [BindProperty]
        public EventInput Input { get; set; } = new();

        public IEnumerable<SelectListItem> EventTypeOptions { get; } =
            Enum.GetValues<EventType>()
                .TakeWhile(x => x == EventType.Wedding)
                .Select(type => new SelectListItem
                {
                    Value = type.ToString(),
                    Text = type.ToString()
                });

        public void OnGet()
        {
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (!ModelState.IsValid) 
            { 
                return Page();
            }

            var newEvent = new Event
            {
                Id = Guid.NewGuid(),
                EventType = Input.EventType,
                Person1Name = (Input.Person1Name ?? "").Trim(),
                Person2Name = (Input.Person2Name ?? "").Trim(),
                EventDate = Input.EventDate,
                Venue = Input.Venue.Trim(),
                VenueMapLink = Input.VenueMapLink?.Trim() ?? string.Empty,
                CreatedAt = DateTime.Now,
                UpdatedAt = DateTime.Now
            };

            _dbContext.Events.Add(newEvent);
            await _dbContext.SaveChangesAsync();

            return RedirectToPage("/Events/Details", new { id = newEvent.Id });
        }

        public class EventInput : IValidatableObject
        {
            private static readonly HashSet<EventType> EventTypesWithPeople = new()
            {
                EventType.Wedding,
                EventType.Engagement,
                EventType.Anniversary
            };

            [Required]
            [Display(Name = "Event type")]
            public EventType EventType { get; set; }

            [Display(Name = "Person 1 name")]
            public string? Person1Name { get; set; } = string.Empty;

            [Display(Name = "Person 2 name")]
            public string? Person2Name { get; set; } = string.Empty;

            [Required]
            [DataType(DataType.Date)]
            [Display(Name = "Event date")]
            public DateTime EventDate { get; set; } = DateTime.Today;

            [Required]
            [Display(Name = "Venue")]
            public string Venue { get; set; } = string.Empty;

            [Display(Name = "Venue map link")]
            [DataType(DataType.Url)]
            public string? VenueMapLink { get; set; }

            public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
            {
                if (!EventTypesWithPeople.Contains(EventType))
                {
                    return Enumerable.Empty<ValidationResult>();
                }

                var results = new List<ValidationResult>();

                if (string.IsNullOrWhiteSpace(Person1Name))
                {
                    results.Add(new ValidationResult("Name is required.", new[] { nameof(Person1Name) }));
                }

                if (string.IsNullOrWhiteSpace(Person2Name) && (EventType != EventType.BabyShower || EventType != EventType.Birthday || EventType != EventType.Party))
                {
                    results.Add(new ValidationResult("Name is required.", new[] { nameof(Person2Name) }));
                }

                return results;
            }
        }
    }
}
