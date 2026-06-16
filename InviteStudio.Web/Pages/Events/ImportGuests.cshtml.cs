using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using InviteStudio.Application.Entities;
using InviteStudio.Application.Persistence;
using InviteStudio.Web.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InviteStudio.Web.Pages.Events
{
    public class ImportGuestsModel : PageModel
    {
        private readonly InviteStudioDbContext _dbContext;

        public ImportGuestsModel(InviteStudioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Event? Event { get; private set; }

        [BindProperty]
        public IFormFile? ImportFile { get; set; }

        [TempData]
        public string ImportStatusMessage { get; set; } = string.Empty;

        public string EventTitle => Event == null ? "Import guests" : BuildEventTitle(Event);

        public string EventSubtitle => Event == null ? string.Empty : $"{FormatEventType(Event.EventType)} · {Event.EventDate:MMMM dd, yyyy}";

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            if (!await LoadPageDataAsync(id))
            {
                return Page();
            }

            return Page();
        }

        public async Task<IActionResult> OnPostAsync(Guid id)
        {
            if (!await LoadPageDataAsync(id))
            {
                return Page();
            }

            if (ImportFile == null || ImportFile.Length == 0)
            {
                ModelState.AddModelError(nameof(ImportFile), "Select an Excel file to import.");
                return Page();
            }

            if (!string.Equals(Path.GetExtension(ImportFile.FileName), ".xlsx", StringComparison.OrdinalIgnoreCase))
            {
                ModelState.AddModelError(nameof(ImportFile), "Only .xlsx Excel files are supported.");
                return Page();
            }

            GuestImportSpreadsheetResult importData;
            try
            {
                await using var stream = ImportFile.OpenReadStream();
                importData = GuestImportSpreadsheetReader.Read(stream);
            }
            catch (InvalidDataException exception)
            {
                ModelState.AddModelError(nameof(ImportFile), exception.Message);
                return Page();
            }
            catch
            {
                ModelState.AddModelError(nameof(ImportFile), "The Excel file could not be read.");
                return Page();
            }

            if (importData.Rows.Count == 0)
            {
                ModelState.AddModelError(nameof(ImportFile), "No valid guest rows were found in the Excel file.");
                return Page();
            }

            var existingTags = await _dbContext.GuestTags
                .ToDictionaryAsync(tag => tag.Name.ToLower(), tag => tag);

            var importedGuests = new List<Guest>(importData.Rows.Count);
            foreach (var row in importData.Rows)
            {
                var tagName = string.IsNullOrWhiteSpace(row.Tag) ? "Imported" : row.Tag.Trim();
                var normalizedTag = tagName.ToLower();

                if (!existingTags.TryGetValue(normalizedTag, out var tag))
                {
                    tag = new GuestTag
                    {
                        Id = Guid.NewGuid(),
                        Name = tagName
                    };

                    existingTags[normalizedTag] = tag;
                    _dbContext.GuestTags.Add(tag);
                }

                importedGuests.Add(new Guest
                {
                    Id = Guid.NewGuid(),
                    Name = row.Name.Trim(),
                    PhoneNumber = row.PhoneNumber.Trim(),
                    Notes = row.Notes.Trim(),
                    GuestTagId = tag.Id
                });
            }

            _dbContext.Guests.AddRange(importedGuests);
            await _dbContext.SaveChangesAsync();

            ImportStatusMessage = importData.SkippedRowCount > 0
                ? $"Imported {importedGuests.Count} guests. Skipped {importData.SkippedRowCount} empty or invalid rows."
                : $"Imported {importedGuests.Count} guests.";

            return RedirectToPage("/Events/Guests", new { id });
        }

        private async Task<bool> LoadPageDataAsync(Guid id)
        {
            Event = await _dbContext.Events.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);
            return Event != null;
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
    }
}
