using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using InviteStudio.Application.Entities;
using InviteStudio.Application.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InviteStudio.Web.Pages.Events
{
    public class GuestsModel : PageModel
    {
        private readonly InviteStudioDbContext _dbContext;

        public GuestsModel(InviteStudioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Event? Event { get; private set; }

        public IReadOnlyList<GuestRow> Guests { get; private set; } = Array.Empty<GuestRow>();

        [BindProperty]
        public List<Guid> SelectedGuestIds { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public int PageNumber { get; set; } = 1;

        [BindProperty(SupportsGet = true)]
        public int PageSize { get; set; } = 20;

        [BindProperty(SupportsGet = true)]
        public string SearchTerm { get; set; } = string.Empty;

        public int TotalGuests { get; private set; }

        public int TotalPages => PageSize <= 0 ? 1 : Math.Max(1, (int)Math.Ceiling(TotalGuests / (double)PageSize));

        public string EventTitle => Event == null ? "Guests" : BuildEventTitle(Event);

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
            if (pageNumber.HasValue)
            {
                PageNumber = pageNumber.Value;
            }

            if (pageSize.HasValue)
            {
                PageSize = pageSize.Value;
            }

            if (!await LoadPageDataAsync(id))
            {
                return Page();
            }

            return RedirectToPage("/Events/Guests", new { id, pageNumber = PageNumber, pageSize = PageSize, searchTerm = SearchTerm });
        }

        public async Task<IActionResult> OnPostDeleteAsync(Guid id, Guid guestId)
        {
            if (!await LoadPageDataAsync(id))
            {
                return Page();
            }

            var guest = await _dbContext.Guests.FirstOrDefaultAsync(item => item.Id == guestId);
            if (guest == null)
            {
                return RedirectToPage("/Events/Guests", new { id, searchTerm = SearchTerm });
            }

            _dbContext.Guests.Remove(guest);
            await _dbContext.SaveChangesAsync();

            return RedirectToPage("/Events/Guests", new { id, pageNumber = PageNumber, pageSize = PageSize, searchTerm = SearchTerm });
        }

        public async Task<IActionResult> OnPostBulkDeleteAsync(Guid id)
        {
            if (!await LoadPageDataAsync(id))
            {
                return Page();
            }

            if (SelectedGuestIds.Count == 0)
            {
                return RedirectToPage("/Events/Guests", new { id, pageNumber = PageNumber, pageSize = PageSize, searchTerm = SearchTerm });
            }

            var guests = await _dbContext.Guests
                .Where(guest => SelectedGuestIds.Contains(guest.Id))
                .ToListAsync();

            if (guests.Count == 0)
            {
                return RedirectToPage("/Events/Guests", new { id, pageNumber = PageNumber, pageSize = PageSize, searchTerm = SearchTerm });
            }

            _dbContext.Guests.RemoveRange(guests);
            await _dbContext.SaveChangesAsync();

            return RedirectToPage("/Events/Guests", new { id, pageNumber = PageNumber, pageSize = PageSize, searchTerm = SearchTerm });
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

        private static string FormatEventType(InviteStudio.Application.Enums.EventType eventType)
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

        public class GuestRow
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string PhoneNumber { get; set; } = string.Empty;
            public string Tag { get; set; } = string.Empty;
        }

        private async Task<bool> LoadPageDataAsync(Guid id)
        {
            Event = await _dbContext.Events.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
            if (Event == null)
            {
                return false;
            }

            PageSize = NormalizePageSize(PageSize);
            PageNumber = Math.Max(1, PageNumber);
            SearchTerm = SearchTerm?.Trim() ?? string.Empty;

            var guestsQuery = _dbContext.Guests
                .AsNoTracking()
                .Include(guest => guest.GuestTag)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(SearchTerm))
            {
                guestsQuery = guestsQuery.Where(guest =>
                    EF.Functions.Like(guest.Name, $"%{SearchTerm}%") ||
                    (guest.PhoneNumber != null && EF.Functions.Like(guest.PhoneNumber, $"%{SearchTerm}%")) ||
                    EF.Functions.Like(guest.GuestTag.Name, $"%{SearchTerm}%"));
            }

            TotalGuests = await guestsQuery.CountAsync();
            var totalPages = TotalPages;
            if (PageNumber > totalPages)
            {
                PageNumber = totalPages;
            }

            var skip = (PageNumber - 1) * PageSize;

            Guests = await guestsQuery
                .OrderBy(guest => guest.Name)
                .Skip(skip)
                .Take(PageSize)
                .Select(guest => new GuestRow
                {
                    Id = guest.Id,
                    Name = guest.Name,
                    PhoneNumber = guest.PhoneNumber,
                    Tag = guest.GuestTag.Name
                })
                .ToListAsync();

            return true;
        }

        private static int NormalizePageSize(int pageSize)
        {
            return pageSize switch
            {
                20 or 50 or 100 or 200 => pageSize,
                _ => 20
            };
        }
    }
}
