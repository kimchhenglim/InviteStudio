using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InviteStudio.Application.Entities;
using InviteStudio.Application.Enums;
using InviteStudio.Application.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InviteStudio.Web.Pages.Events
{
    public class DetailsModel : PageModel
    {
        private readonly InviteStudioDbContext _dbContext;

        public DetailsModel(InviteStudioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Event? Event { get; private set; }

        public IReadOnlyList<TagSummary> TagSummaries { get; private set; } = Array.Empty<TagSummary>();

        public int TotalGuests { get; private set; }

        public int TotalTags => TagSummaries.Count;

        public string EventTitle => Event == null ? "Event details" : BuildEventTitle(Event);

        public string EventSubtitle => Event == null ? string.Empty : $"{FormatEventType(Event.EventType)} · {Event.EventDate:MMMM dd, yyyy}";

        public string FormattedEventType => Event == null ? string.Empty : FormatEventType(Event.EventType);

        public string Person1Label => GetPersonLabels().Label1;

        public string Person2Label => GetPersonLabels().Label2;

        public bool ShowPerson2 => !string.IsNullOrWhiteSpace(Person2Label);

        public bool HasMapLink => Event != null && !string.IsNullOrWhiteSpace(Event.VenueMapLink);

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Event = await _dbContext.Events.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

            if (Event == null)
            {
                return Page();
            }

            var guestRows = await _dbContext.Guests
                .AsNoTracking()
                .Include(guest => guest.GuestTag)
                .Select(guest => new
                {
                    Tag = guest.GuestTag.Name
                })
                .ToListAsync();

            TotalGuests = guestRows.Count;
            TagSummaries = guestRows
                .GroupBy(guest => guest.Tag)
                .Select(group => new TagSummary(group.Key, group.Count()))
                .OrderByDescending(summary => summary.Count)
                .ThenBy(summary => summary.Tag)
                .ToList();

            return Page();
        }

        private static (string Label1, string Label2) GetLabels(EventType eventType)
        {
            return eventType switch
            {
                EventType.Wedding => ("Bride", "Groom"),
                EventType.Engagement => ("Fiancée", "Fiancé"),
                EventType.Anniversary => ("Husband", "Wife"),
                EventType.BabyShower => ("Guest of honor", string.Empty),
                EventType.Birthday => ("Celebrant", string.Empty),
                EventType.Party => ("Event title", string.Empty),
                _ => ("Person 1", "Person 2")
            };
        }

        private (string Label1, string Label2) GetPersonLabels()
        {
            return Event == null ? ("Person 1", "Person 2") : GetLabels(Event.EventType);
        }

        private static string BuildEventTitle(Event @event)
        {
            if (!string.IsNullOrWhiteSpace(@event.Person1Name) && !string.IsNullOrWhiteSpace(@event.Person2Name))
            {
                return $"{@event.Person1Name} & {@event.Person2Name}";
            }

            if (!string.IsNullOrWhiteSpace(@event.Person1Name))
            {
                return @event.Person1Name;
            }

            return FormatEventType(@event.EventType);
        }

        private static string FormatEventType(EventType eventType)
        {
            var value = eventType.ToString();
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var parts = new List<char>(value.Length * 2);
            for (var i = 0; i < value.Length; i++)
            {
                var current = value[i];
                if (i > 0 && char.IsUpper(current) && !char.IsWhiteSpace(value[i - 1]))
                {
                    parts.Add(' ');
                }

                parts.Add(current);
            }

            return new string(parts.ToArray());
        }

        public record TagSummary(string Tag, int Count);
    }
}
