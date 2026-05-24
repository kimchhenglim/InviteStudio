using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using InviteStudio.Application.Entities;
using InviteStudio.Application.Enums;
using InviteStudio.Application.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InviteStudio.Web.Pages.Events
{
    public class DesignCardModel : PageModel
    {
        private readonly InviteStudioDbContext _dbContext;

        public DesignCardModel(InviteStudioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Event? Event { get; private set; }

        public InvitationTemplateModel TemplateModel { get; private set; } = new();

        public string TemplatePartialName { get; private set; } = "_DefaultCard";

        public IReadOnlyList<TemplateOption> TemplateOptions { get; private set; } = new List<TemplateOption>();

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Event = await _dbContext.Events.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

            if (Event == null)
            {
                return Page();
            }

            TemplateOptions = BuildTemplateOptions(Event.EventType);
            TemplatePartialName = SelectTemplate(Event.EventType);
            TemplateModel = BuildTemplateModel(Event);

            return Page();
        }

        public async Task<IActionResult> OnGetTemplateAsync(Guid id, string? template)
        {
            Event = await _dbContext.Events.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

            if (Event == null)
            {
                return NotFound();
            }

            TemplateOptions = BuildTemplateOptions(Event.EventType);
            TemplatePartialName = SelectTemplate(Event.EventType, template);
            TemplateModel = BuildTemplateModel(Event);

            return new PartialViewResult
            {
                ViewName = $"~/Pages/Events/Templates/{TemplatePartialName}.cshtml",
                ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<InvitationTemplateModel>(ViewData, TemplateModel)
            };
        }

        internal static string BuildEventTitle(Event @event)
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

        internal static string FormatEventType(EventType eventType)
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

        private static string SelectTemplate(EventType eventType, string? template = null)
        {
            var options = BuildTemplateOptions(eventType);
            var normalized = template?.Trim();
            if (!string.IsNullOrWhiteSpace(normalized) && options.Any(option => option.Value == normalized))
            {
                return normalized;
            }

            return eventType switch
            {
                EventType.Wedding => "_WeddingCard",
                _ => "_DefaultCard"
            };
        }

        private static IReadOnlyList<TemplateOption> BuildTemplateOptions(EventType eventType)
        {
            return eventType switch
            {
                EventType.Wedding => new List<TemplateOption>
                {
                    new("_WeddingCard", "Wedding - Modern"),
                    new("_WeddingClassicCard", "Wedding - Classic")
                },
                _ => new List<TemplateOption>
                {
                    new("_DefaultCard", "Default")
                }
            };
        }

        private static InvitationTemplateModel BuildTemplateModel(Event @event)
        {
            return new InvitationTemplateModel
            {
                Person1Name = @event.Person1Name,
                Person2Name = @event.Person2Name,
                EventTypeLabel = FormatEventType(@event.EventType),
                Title = BuildEventTitle(@event),
                Subtitle = $"{FormatEventType(@event.EventType)} · {@event.EventDate:MMMM dd, yyyy}",
                Message = "We would love to celebrate with you. Please join us for our special day.",
                DateText = @event.EventDate.ToString("MMMM dd, yyyy"),
                Venue = @event.Venue,
                AccentColor = "#1f8cff",
                BackgroundColor = "#ffffff",
                FontFamily = "'Segoe UI', sans-serif",
                FooterLeft = "RSVP by April 10",
                FooterRight = "invites.invite.studio"
            };
        }
    }

    public class InvitationTemplateModel
    {
        public string Person1Name { get; set; } = string.Empty;
        public string Person2Name { get; set; } = string.Empty;
        public string EventTypeLabel { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Subtitle { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string DateText { get; set; } = string.Empty;
        public string Venue { get; set; } = string.Empty;
        public string AccentColor { get; set; } = "#1f8cff";
        public string BackgroundColor { get; set; } = "#ffffff";
        public string FontFamily { get; set; } = "'Segoe UI', sans-serif";
        public string FooterLeft { get; set; } = string.Empty;
        public string FooterRight { get; set; } = string.Empty;
    }

    public record TemplateOption(string Value, string Label);
}
