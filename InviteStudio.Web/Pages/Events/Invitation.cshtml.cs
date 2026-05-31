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

        public string TemplateAssetKey { get; private set; } = "default";

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Event = await _dbContext.Events.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

            if (Event == null)
            {
                return Page();
            }

            TemplatePartialName = SelectTemplate(Event.EventType);
            TemplateAssetKey = DesignCardModel.GetTemplateAssetKey(TemplatePartialName);
            TemplateModel = BuildTemplateModel(Event);

            return Page();
        }

        private static string SelectTemplate(EventType eventType)
        {
            return eventType switch
            {
                EventType.Wedding => "_WeddingCard",
                EventType.Birthday => "_BirthdayCard",
                _ => "_DefaultCard"
            };
        }

        private static InvitationTemplateModel BuildTemplateModel(Event @event)
        {
            var timeline = TimelineSchedule.FromJson(@event.TimelineJson);
            return new InvitationTemplateModel
            {
                Title = DesignCardModel.BuildEventTitle(@event),
                Subtitle = $"{DesignCardModel.FormatEventType(@event.EventType)} · {@event.EventDate:MMMM dd, yyyy}",
                Message = "We would love to celebrate with you. Please join us for our special day.",
                DateText = @event.EventDate.ToString("MMMM dd, yyyy"),
                Venue = @event.Venue,
                Person1Phone = @event.Person1Phone,
                Person2Phone = @event.Person2Phone,
                VideoLink = @event.VideoLink,
                MusicLink = @event.MusicLink,
                VideoEmbedLink = DesignCardModel.BuildEmbedLink(@event.VideoLink),
                MusicEmbedLinkMuted = DesignCardModel.BuildMusicEmbedLink(@event.MusicLink, true),
                MusicEmbedLink = DesignCardModel.BuildMusicEmbedLink(@event.MusicLink, false),
                MusicEmbedType = DesignCardModel.GetMusicEmbedType(@event.MusicLink),
                AccentColor = "#1f8cff",
                BackgroundColor = "#ffffff",
                FontFamily = "'Segoe UI', sans-serif",
                FooterLeft = string.Empty,
                FooterRight = string.Empty,
                Timeline = timeline
            };
        }
    }
}
