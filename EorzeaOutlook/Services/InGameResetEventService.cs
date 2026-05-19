using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;

using EorzeaOutlook.Localization;
using EorzeaOutlook.Models;

namespace EorzeaOutlook.Services;

public sealed class InGameResetEventService
{
    private static readonly TimeSpan ShortEventDuration =
        TimeSpan.FromMinutes(15);

    public IReadOnlyList<EventData> GetEvents(
        DateTime now,
        Configuration configuration,
        string languageCode)
    {
        var start =
            now.Date.AddDays(-7);

        var end =
            now.Date.AddDays(90);

        var events =
            new List<EventData>();

        if (configuration.ShowDailyResetEvents)
        {
            AddDailyEvents(
                events,
                start,
                end,
                languageCode);
        }

        if (configuration.ShowWeeklyResetEvents)
        {
            AddWeeklyEvents(
                events,
                start,
                end,
                languageCode);
        }

        if (configuration.ShowFashionReportEvents)
        {
            AddFashionReportEvents(
                events,
                start,
                end,
                languageCode);
        }

        if (configuration.ShowJumboCactpotEvents)
        {
            AddJumboCactpotEvents(
                events,
                start,
                end,
                languageCode);
        }

        return events;
    }

    private static void AddDailyEvents(
        List<EventData> events,
        DateTime start,
        DateTime end,
        string languageCode)
    {
        for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
        {
            AddResetEvent(
                events,
                Loc.Text(languageCode, "Reset.Daily.Title", "Daily Reset"),
                Loc.Text(languageCode, "Reset.Daily.Description", "Tribal allowances, daily roulettes, daily hunts, mini cactpot, and other daily activities reset."),
                "Daily Reset",
                UtcToLocal(day, 15));
        }
    }

    private static void AddWeeklyEvents(
        List<EventData> events,
        DateTime start,
        DateTime end,
        string languageCode)
    {
        for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
        {
            if (day.DayOfWeek != DayOfWeek.Tuesday)
            {
                continue;
            }

            AddResetEvent(
                events,
                Loc.Text(languageCode, "Reset.Weekly.Title", "Weekly Reset"),
                Loc.Text(languageCode, "Reset.Weekly.Description", "Weekly lockouts, challenge log, custom deliveries, Wondrous Tails, Faux Hollows, and capped tomestone tracking reset."),
                "Weekly Reset",
                UtcToLocal(day, 8));
        }
    }

    private static void AddFashionReportEvents(
        List<EventData> events,
        DateTime start,
        DateTime end,
        string languageCode)
    {
        for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
        {
            if (day.DayOfWeek == DayOfWeek.Friday)
            {
                AddResetEvent(
                    events,
                    Loc.Text(languageCode, "Reset.FashionJudging.Title", "Fashion Report Judging"),
                    Loc.Text(languageCode, "Reset.FashionJudging.Description", "Masked Rose judging opens for the week."),
                    "Gold Saucer",
                    UtcToLocal(day, 8));
            }

            if (day.DayOfWeek == DayOfWeek.Tuesday)
            {
                AddResetEvent(
                    events,
                    Loc.Text(languageCode, "Reset.FashionEnds.Title", "Fashion Report Ends"),
                    Loc.Text(languageCode, "Reset.FashionEnds.Description", "Current Fashion Report judging period ends."),
                    "Gold Saucer",
                    UtcToLocal(day, 8));
            }
        }
    }

    private static void AddJumboCactpotEvents(
        List<EventData> events,
        DateTime start,
        DateTime end,
        string languageCode)
    {
        for (var day = start.Date; day <= end.Date; day = day.AddDays(1))
        {
            if (day.DayOfWeek != DayOfWeek.Saturday)
            {
                continue;
            }

            AddResetEvent(
                events,
                Loc.Text(languageCode, "Reset.JumboCactpot.Title", "Jumbo Cactpot Drawing"),
                Loc.Text(languageCode, "Reset.JumboCactpot.Description", "Weekly Jumbo Cactpot drawing."),
                "Gold Saucer",
                day.Date.AddHours(21));
        }
    }

    private static DateTime UtcToLocal(
        DateTime localDate,
        int utcHour)
    {
        var utc =
            new DateTime(
                localDate.Year,
                localDate.Month,
                localDate.Day,
                utcHour,
                0,
                0,
                DateTimeKind.Utc);

        return utc.ToLocalTime();
    }

    private static void AddResetEvent(
        List<EventData> events,
        string title,
        string description,
        string category,
        DateTime startTime)
    {
        events.Add(
            new EventData
            {
                Id = StableId(
                    title,
                    startTime),
                Title = title,
                Description = description,
                Category = category,
                StartTime = startTime,
                EndTime = startTime.Add(ShortEventDuration),
                ReminderMinutesBefore = 0,
                ReminderTriggered = true,
                IsOfficial = true
            });
    }

    private static Guid StableId(
        string title,
        DateTime startTime)
    {
        var bytes =
            MD5.HashData(
                Encoding.UTF8.GetBytes(
                    $"{title}:{startTime:O}"));

        return new Guid(bytes);
    }
}
