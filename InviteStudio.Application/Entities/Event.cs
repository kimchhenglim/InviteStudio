using System;
using System.Collections.Generic;
using System.Text;
using InviteStudio.Application.Enums;

namespace InviteStudio.Application.Entities;

public class Event : BaseEntity
{
    public string TemplateName { get; set; } = string.Empty;
    public string AccentColor { get; set; } = "#1f8cff";
    public string BackgroundColor { get; set; } = "#ffffff";
    public string FontFamily { get; set; } = "'Segoe UI', sans-serif";
    public string LayoutStyle { get; set; } = "center";
    public EventType EventType { get; set; }
    public string Person1Name { get; set; } = string.Empty;
    public string Person2Name { get; set; } = string.Empty;
    public string Person1Phone { get; set; } = string.Empty;
    public string Person2Phone { get; set; } = string.Empty;
    public DateTime EventDate { get; set; } = DateTime.Now;
    public string Venue { get; set; } = string.Empty;
    public string VenueMapLink { get; set; } = string.Empty;
    public string VideoLink { get; set; } = string.Empty;
    public string MusicLink { get; set; } = string.Empty;
    public string TimelineJson { get; set; } = string.Empty;
}
