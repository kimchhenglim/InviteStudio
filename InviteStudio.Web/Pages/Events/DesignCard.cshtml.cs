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

        [BindProperty]
        public DesignCardInputModel Input { get; set; } = new();

        public InvitationTemplateModel TemplateModel { get; private set; } = new();

        public string TemplatePartialName { get; private set; } = "_DefaultCard";

        public IReadOnlyList<TemplateOption> TemplateOptions { get; private set; } = new List<TemplateOption>();

        public IReadOnlyList<string> TemplateAssets { get; private set; } = Array.Empty<string>();

        public async Task<IActionResult> OnGetAsync(Guid id)
        {
            Event = await _dbContext.Events.AsNoTracking().FirstOrDefaultAsync(item => item.Id == id);

            if (Event == null)
            {
                return Page();
            }

            TemplateOptions = BuildTemplateOptions(Event.EventType);
            TemplatePartialName = SelectTemplate(Event.EventType);
            TemplateAssets = BuildTemplateAssets(TemplateOptions);
            TemplateModel = BuildTemplateModel(Event);
            Input = new DesignCardInputModel
            {
                EventId = Event.Id,
                Person1Name = Event.Person1Name,
                Person2Name = Event.Person2Name,
                Person1Phone = Event.Person1Phone,
                Person2Phone = Event.Person2Phone,
                EventDate = Event.EventDate,
                Venue = Event.Venue,
                VenueMapLink = Event.VenueMapLink,
                VideoLink = Event.VideoLink,
                MusicLink = Event.MusicLink,
                TimelineJson = Event.TimelineJson
            };

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
            TemplateAssets = BuildTemplateAssets(TemplateOptions);
            TemplateModel = BuildTemplateModel(Event);

            return new PartialViewResult
            {
                ViewName = $"~/Pages/Events/Templates/{TemplatePartialName}.cshtml",
                ViewData = new Microsoft.AspNetCore.Mvc.ViewFeatures.ViewDataDictionary<InvitationTemplateModel>(ViewData, TemplateModel)
            };
        }

        public async Task<IActionResult> OnPostAsync()
        {
            if (Input.EventId == Guid.Empty)
            {
                return Page();
            }

            var @event = await _dbContext.Events.FirstOrDefaultAsync(item => item.Id == Input.EventId);
            if (@event == null)
            {
                return NotFound();
            }

            @event.Person1Name = Input.Person1Name?.Trim() ?? string.Empty;
            @event.Person2Name = Input.Person2Name?.Trim() ?? string.Empty;
            @event.Person1Phone = Input.Person1Phone?.Trim() ?? string.Empty;
            @event.Person2Phone = Input.Person2Phone?.Trim() ?? string.Empty;
            @event.EventDate = Input.EventDate == default ? @event.EventDate : Input.EventDate;
            @event.Venue = Input.Venue?.Trim() ?? string.Empty;
            @event.VenueMapLink = Input.VenueMapLink?.Trim() ?? string.Empty;
            @event.VideoLink = Input.VideoLink?.Trim() ?? string.Empty;
            @event.MusicLink = Input.MusicLink?.Trim() ?? string.Empty;
            @event.TimelineJson = Input.TimelineJson?.Trim() ?? string.Empty;

            await _dbContext.SaveChangesAsync();

            return RedirectToPage("/Events/DesignCard", new { id = Input.EventId });
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
                EventType.Birthday => "_BirthdayCard",
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
                EventType.Birthday => new List<TemplateOption>
                {
                    new("_BirthdayCard", "Birthday - Party")
                },
                _ => new List<TemplateOption>
                {
                    new("_DefaultCard", "Default")
                }
            };
        }

        public static string GetTemplateAssetKey(string templatePartialName)
        {
            return templatePartialName switch
            {
                "_WeddingCard" => "wedding-modern",
                "_WeddingClassicCard" => "wedding-classic",
                "_BirthdayCard" => "birthday-party",
                _ => "default"
            };
        }

        private static IReadOnlyList<string> BuildTemplateAssets(IReadOnlyList<TemplateOption> templateOptions)
        {
            var assets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var option in templateOptions)
            {
                assets.Add(GetTemplateAssetKey(option.Value));
            }

            return assets.ToList();
        }

        private static InvitationTemplateModel BuildTemplateModel(Event @event)
        {
            var timeline = TimelineSchedule.FromJson(@event.TimelineJson);
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
                VenueMapLink = @event.VenueMapLink,
                Person1Phone = @event.Person1Phone,
                Person2Phone = @event.Person2Phone,
                VideoLink = @event.VideoLink,
                MusicLink = @event.MusicLink,
                VideoEmbedLink = BuildEmbedLink(@event.VideoLink),
                MusicEmbedLinkMuted = BuildMusicEmbedLink(@event.MusicLink, true),
                MusicEmbedLink = BuildMusicEmbedLink(@event.MusicLink, false),
                MusicEmbedType = GetMusicEmbedType(@event.MusicLink),
                AccentColor = "#1f8cff",
                BackgroundColor = "#ffffff",
                FontFamily = "'Segoe UI', sans-serif",
                FooterLeft = string.Empty,
                FooterRight = string.Empty,
                Timeline = timeline
            };
        }

        public static string BuildEmbedLink(string? link)
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                return string.Empty;
            }

            if (Uri.TryCreate(link, UriKind.Absolute, out var uri))
            {
                if (uri.Host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
                {
                    var id = ExtractYouTubeId(uri);
                    return string.IsNullOrWhiteSpace(id) ? link : $"https://www.youtube.com/embed/{id}";
                }

                if (uri.Host.Contains("drive.google.com", StringComparison.OrdinalIgnoreCase))
                {
                    var id = ExtractDriveId(uri);
                    return string.IsNullOrWhiteSpace(id) ? link : $"https://drive.google.com/file/d/{id}/preview";
                }
            }

            return link;
        }

        public static string BuildMusicEmbedLink(string? link, bool muted)
        {
            var embed = BuildEmbedLink(link);
            if (string.IsNullOrWhiteSpace(embed) || !embed.Contains("youtube.com/embed", StringComparison.OrdinalIgnoreCase))
            {
                return embed;
            }

            var separator = embed.Contains("?") ? "&" : "?";
            var mutedValue = muted ? "1" : "0";
            return $"{embed}{separator}autoplay=1&mute={mutedValue}&controls=0&loop=1&playlist={ExtractYouTubeId(new Uri(embed))}&enablejsapi=1&playsinline=1";
        }

        public static string GetMusicEmbedType(string? link)
        {
            if (string.IsNullOrWhiteSpace(link))
            {
                return string.Empty;
            }

            if (Uri.TryCreate(link, UriKind.Absolute, out var uri))
            {
                if (uri.Host.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
                    uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
                {
                    return "youtube";
                }
            }

            return "audio";
        }

        private static string? ExtractYouTubeId(Uri uri)
        {
            if (uri.Host.Contains("youtu.be", StringComparison.OrdinalIgnoreCase))
            {
                return uri.AbsolutePath.Trim('/');
            }

            var query = System.Web.HttpUtility.ParseQueryString(uri.Query);
            var id = query.Get("v");
            if (!string.IsNullOrWhiteSpace(id))
            {
                return id;
            }

            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var embedIndex = Array.IndexOf(segments, "embed");
            if (embedIndex >= 0 && segments.Length > embedIndex + 1)
            {
                return segments[embedIndex + 1];
            }

            return null;
        }

        private static string? ExtractDriveId(Uri uri)
        {
            var segments = uri.AbsolutePath.Split('/', StringSplitOptions.RemoveEmptyEntries);
            var fileIndex = Array.IndexOf(segments, "d");
            if (fileIndex >= 0 && segments.Length > fileIndex + 1)
            {
                return segments[fileIndex + 1];
            }

            return null;
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
        public string VenueMapLink { get; set; } = string.Empty;
        public string AccentColor { get; set; } = "#1f8cff";
        public string BackgroundColor { get; set; } = "#ffffff";
        public string FontFamily { get; set; } = "'Segoe UI', sans-serif";
        public string FooterLeft { get; set; } = string.Empty;
        public string FooterRight { get; set; } = string.Empty;
        public string Person1Phone { get; set; } = string.Empty;
        public string Person2Phone { get; set; } = string.Empty;
        public string VideoLink { get; set; } = string.Empty;
        public string MusicLink { get; set; } = string.Empty;
        public string VideoEmbedLink { get; set; } = string.Empty;
        public string MusicEmbedLinkMuted { get; set; } = string.Empty;
        public string MusicEmbedLink { get; set; } = string.Empty;
        public string MusicEmbedType { get; set; } = string.Empty;
        public TimelineSchedule Timeline { get; set; } = new();
    }

    public record TemplateOption(string Value, string Label);

    public class TimelineSchedule
    {
        private static readonly System.Text.Json.JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public List<TimelineScheduleGroup> Groups { get; set; } = new();

        public static TimelineSchedule FromJson(string? json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new TimelineSchedule();
            }

            try
            {
                var groups = System.Text.Json.JsonSerializer.Deserialize<List<TimelineScheduleGroup>>(json, JsonOptions);
                return new TimelineSchedule { Groups = groups ?? new List<TimelineScheduleGroup>() };
            }
            catch
            {
                return new TimelineSchedule();
            }
        }
    }

    public class TimelineScheduleGroup
    {
        public string Id { get; set; } = string.Empty;
        public string Date { get; set; } = string.Empty;
        public List<TimelineScheduleItem> Items { get; set; } = new();
    }

    public class TimelineScheduleItem
    {
        public string Id { get; set; } = string.Empty;
        public string Time { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
    }

    public class DesignCardInputModel
    {
        public Guid EventId { get; set; }
        public string Person1Name { get; set; } = string.Empty;
        public string Person2Name { get; set; } = string.Empty;
        public string Person1Phone { get; set; } = string.Empty;
        public string Person2Phone { get; set; } = string.Empty;
        public DateTime EventDate { get; set; }
        public string Venue { get; set; } = string.Empty;
        public string VenueMapLink { get; set; } = string.Empty;
        public string VideoLink { get; set; } = string.Empty;
        public string MusicLink { get; set; } = string.Empty;
        public string TimelineJson { get; set; } = string.Empty;
    }
}
