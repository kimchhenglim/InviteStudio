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
    public class AddGuestModel : PageModel
    {
        private readonly InviteStudioDbContext _dbContext;

        public AddGuestModel(InviteStudioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Event? Event { get; private set; }

        public IReadOnlyList<GuestTagOption> GuestTags { get; private set; } = Array.Empty<GuestTagOption>();

        [BindProperty]
        public AddGuestInputModel Input { get; set; } = new();

        public string EventTitle => Event == null ? "Add guest" : BuildEventTitle(Event);

        public string EventSubtitle => Event == null ? string.Empty : $"{FormatEventType(Event.EventType)} · {Event.EventDate:MMMM dd, yyyy}";

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            if (!await LoadPageDataAsync(id))
            {
                return Page();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id, int? pageNumber, int? pageSize)
        {
            Event = await _dbContext.Events.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
            if (Event == null)
            {
                return Page();
            }

            var name = Input.Name?.Trim() ?? string.Empty;
            var phoneNumber = Input.PhoneNumber?.Trim() ?? string.Empty;
            var notes = Input.Notes?.Trim() ?? string.Empty;
            var tagName = Input.TagName?.Trim() ?? string.Empty;

            if (string.IsNullOrWhiteSpace(name))
            {
                ModelState.AddModelError(nameof(Input.Name), "Guest name is required.");
            }

            if (Input.TagId == null && string.IsNullOrWhiteSpace(tagName))
            {
                ModelState.AddModelError(nameof(Input.TagId), "Select a tag or create a new one.");
            }

            if (!ModelState.IsValid)
            {
                await LoadPageDataAsync(id);
                return Page();
            }

            var resolvedTagId = await ResolveGuestTagIdAsync(Input.TagId, tagName);
            if (resolvedTagId == Guid.Empty)
            {
                ModelState.AddModelError(nameof(Input.TagId), "Unable to resolve the guest tag.");
                await LoadPageDataAsync(id);
                return Page();
            }

            var guest = new Guest
            {
                Name = name,
                PhoneNumber = phoneNumber,
                Notes = notes,
                GuestTagId = resolvedTagId
            };

            _dbContext.Guests.Add(guest);
            await _dbContext.SaveChangesAsync();

            return RedirectToPage("/Events/Guests", new { id, pageNumber, pageSize });
        }

        private async Task<bool> LoadPageDataAsync(Guid id)
        {
            Event = await _dbContext.Events.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
            if (Event == null)
            {
                return false;
            }

            GuestTags = await _dbContext.GuestTags
                .AsNoTracking()
                .OrderBy(tag => tag.Name)
                .Select(tag => new GuestTagOption(tag.Id, tag.Name))
                .ToListAsync();

            return true;
        }

        private async Task<Guid> ResolveGuestTagIdAsync(Guid? tagId, string tagName)
        {
            if (tagId.HasValue && tagId.Value != Guid.Empty)
            {
                var existing = await _dbContext.GuestTags.AsNoTracking().FirstOrDefaultAsync(tag => tag.Id == tagId.Value);
                return existing?.Id ?? Guid.Empty;
            }

            if (string.IsNullOrWhiteSpace(tagName))
            {
                return Guid.Empty;
            }

            var normalizedTag = tagName.Trim();
            var existingTag = await _dbContext.GuestTags
                .FirstOrDefaultAsync(tag => tag.Name.ToLower() == normalizedTag.ToLower());

            if (existingTag != null)
            {
                return existingTag.Id;
            }

            var newTag = new GuestTag
            {
                Name = normalizedTag
            };

            _dbContext.GuestTags.Add(newTag);
            await _dbContext.SaveChangesAsync();
            return newTag.Id;
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

        public record GuestTagOption(Guid Id, string Name);

        public class AddGuestInputModel
        {
            public string Name { get; set; } = string.Empty;
            public string? PhoneNumber { get; set; } = string.Empty;
            public string? Notes { get; set; } = string.Empty;
            public Guid? TagId { get; set; }
            public string TagName { get; set; } = string.Empty;
        }
    }
}
