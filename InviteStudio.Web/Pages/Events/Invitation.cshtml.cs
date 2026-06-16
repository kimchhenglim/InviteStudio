using System;
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

            TemplatePartialName = SelectTemplate(Event.EventType, Event.TemplateName);
            TemplateAssetKey = DesignCardModel.GetTemplateAssetKey(TemplatePartialName);
            TemplateModel = DesignCardModel.BuildTemplateModel(Event);

            return Page();
        }

        private static string SelectTemplate(EventType eventType, string? template = null)
        {
            var options = eventType switch
            {
                EventType.Wedding => new[] { "_WeddingCard", "_WeddingClassicCard" },
                EventType.Birthday => new[] { "_BirthdayCard" },
                _ => new[] { "_DefaultCard" }
            };

            var normalized = template?.Trim();
            if (!string.IsNullOrWhiteSpace(normalized) && options.Contains(normalized))
            {
                return normalized;
            }

            return options[0];
        }
    }
}
