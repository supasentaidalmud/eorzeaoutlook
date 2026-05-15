using System;
using System.Linq;

using Dalamud.Interface.ImGuiNotification;
using Dalamud.Plugin.Services;

using EorzeaOutlook.Localization;
using EorzeaOutlook.Models;

namespace EorzeaOutlook.Services;

public class ReminderService
{
    private readonly Plugin plugin;

    private readonly IPluginLog log;

    private readonly INotificationManager notificationManager;

    public ReminderService(
        Plugin plugin,
        IPluginLog log,
        INotificationManager notificationManager)
    {
        this.plugin = plugin;
        this.log = log;
        this.notificationManager = notificationManager;
    }

    public void CheckReminders()
    {
        var now =
            DateTime.Now;

        foreach (var evt in plugin.Configuration.Events.ToList())
        {
            if (evt.ReminderTriggered)
                continue;

            var reminderTime =
                evt.StartTime.AddMinutes(
                    -evt.ReminderMinutesBefore);

            if (now < reminderTime)
                continue;

            if (now > evt.StartTime.AddMinutes(1))
                continue;

            TriggerReminder(evt);
        }
    }

    private void TriggerReminder(
        EventData evt)
    {
        evt.ReminderTriggered = true;

        plugin.Configuration.Save();

        log.Information(
            $"Reminder triggered for: {evt.Title}");

        notificationManager.AddNotification(
            new Notification
            {
                Title =
                    Loc.Text(
                        plugin.Configuration.LanguageCode,
                        "Dialog.Reminder",
                        "Reminder"),
                Content =
                    $"{evt.Title} {Loc.TimeShort(plugin.Configuration.LanguageCode, evt.StartTime)}",
                Type = NotificationType.Info
            });
    }
}
