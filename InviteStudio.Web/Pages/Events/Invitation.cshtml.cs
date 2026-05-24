using System;
using System.Threading.Tasks;
using InviteStudio.Application.Entities;
using InviteStudio.Application.Enums;
using InviteStudio.Application.Persistence;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;

namespace InviteStudio.Web.Pages.Events
{
    public class InvitationModel : PageModel
    {
        private readonly InviteStudioDbContext _dbContext;

        public InvitationModel(InviteStudioDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public Event? Event { get; private set; }

        public InvitationTemplateModel TemplateModel { get; private set; } = new();

        public string TemplatePartialName { get; private set; } = "_DefaultCard";

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Event = await _dbContext.Events.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

            if (Event == null)
            {
                return Page();
            }

            TemplatePartialName = SelectTemplate(Event.EventType);
            TemplateModel = BuildTemplateModel(Event);

            return Page();
        }

        private static string SelectTemplate(EventType eventType)
        {
            return eventType switch
            {
                EventType.Wedding => "_WeddingCard",
                _ => "_DefaultCard"
            };
        }

        private static InvitationTemplateModel BuildTemplateModel(Event @event)
        {
            return new InvitationTemplateModel
            {
                Title = DesignCardModel.BuildEventTitle(@event),
                Subtitle = $"{DesignCardModel.FormatEventType(@event.EventType)} · {@event.EventDate:MMMM dd, yyyy}",
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
}
