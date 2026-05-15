using System;

namespace EorzeaOutlook.Models;

[Serializable]
public class EventData
{
    public Guid Id { get; set; } =
        Guid.NewGuid();

    public string Title { get; set; } =
        "";

    public string Description { get; set; } =
        "";

    public DateTime StartTime { get; set; } =
        DateTime.Now;

    public DateTime? EndTime { get; set; }

    public int ReminderMinutesBefore { get; set; } =
        15;

    public bool ReminderTriggered { get; set; } =
        false;

    public string Category { get; set; } =
        "Custom";

    public bool IsOfficial { get; set; } =
        false;

    public string SourceUrl { get; set; } =
        "";
}
