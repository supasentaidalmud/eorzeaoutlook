using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;

using EorzeaOutlook.Localization;
using EorzeaOutlook.Models;

namespace EorzeaOutlook.Windows.Components;

public class CalendarMonthView
{
    private readonly Plugin plugin;

    private readonly EventEditorModal editorModal;

    private DateTime currentMonth =
        new(DateTime.Now.Year, DateTime.Now.Month, 1);

    private Dictionary<int, List<EventData>> currentMonthEvents =
        [];

    public CalendarMonthView(
        Plugin plugin,
        EventEditorModal editorModal)
    {
        this.plugin = plugin;

        this.editorModal = editorModal;
    }

    public void GoToToday()
    {
        currentMonth =
            new DateTime(
                DateTime.Now.Year,
                DateTime.Now.Month,
                1);
    }

    public void Draw(
        float height,
        bool compact)
    {
        DrawToolbar();

        ImGui.Spacing();

        RebuildCurrentMonthEvents();

        DrawCalendarGrid(
            height,
            compact);
    }

    private void DrawToolbar()
    {
        if (ImGui.Button(
                "<",
                new Vector2(24, 0)))
        {
            currentMonth =
                currentMonth.AddMonths(-1);
        }

        ImGui.SameLine();

        if (ImGui.Button(
                ">",
                new Vector2(24, 0)))
        {
            currentMonth =
                currentMonth.AddMonths(1);
        }

        ImGui.SameLine();

        ImGui.TextColored(
            new Vector4(1f, 0.85f, 0.3f, 1f),
            Loc.MonthYear(
                plugin.Configuration.LanguageCode,
                currentMonth));

    }

    private void DrawCalendarGrid(
        float height,
        bool compact)
    {
        var availableWidth =
            ImGui.GetContentRegionAvail().X;

        var columnWidth =
            availableWidth / 7;

        var cellHeight =
            Math.Max(
                compact ? 70 : 80,
                (height - (compact ? 70 : 76)) / 6);

        var origin =
            ImGui.GetCursorScreenPos();

        var dayNames =
            Loc.DayNames(plugin.Configuration.LanguageCode);

        for (int column = 0; column < 7; column++)
        {
            ImGui.SetCursorScreenPos(
                new Vector2(
                    origin.X + (column * columnWidth),
                    origin.Y));

            ImGui.TextColored(
                new Vector4(0.6f, 0.8f, 1f, 1f),
                dayNames[column]);
        }

        var gridTop =
            origin.Y + 22;

        var firstDay =
            new DateTime(
                currentMonth.Year,
                currentMonth.Month,
                1);

        int offset =
            (int)firstDay.DayOfWeek;

        int daysInMonth =
            DateTime.DaysInMonth(
                currentMonth.Year,
                currentMonth.Month);

        for (int row = 0; row < 6; row++)
        {
            for (int column = 0; column < 7; column++)
            {
                var day =
                    (row * 7) + column - offset + 1;

                if (day < 1 || day > daysInMonth)
                    continue;

                ImGui.SetCursorScreenPos(
                    new Vector2(
                        origin.X + (column * columnWidth),
                        gridTop + (row * cellHeight)));

                DrawDayCell(
                    day,
                    new Vector2(
                        columnWidth - (compact ? 4 : 6),
                        cellHeight - (compact ? 4 : 6)));
            }
        }

        ImGui.SetCursorScreenPos(
            new Vector2(
                origin.X,
                gridTop + (6 * cellHeight)));
    }

    private void DrawDayCell(
        int day,
        Vector2 cellSize)
    {
        var cellDate =
            new DateTime(
                currentMonth.Year,
                currentMonth.Month,
                day);

        var events =
            currentMonthEvents.TryGetValue(day, out var dayEvents)
                ? dayEvents
                : [];

        ImGui.BeginChild(
            $"day_{day}",
            cellSize,
            true,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        DrawDayHeader(
            day,
            cellDate);

        var eventsHeight =
            Math.Max(
                24,
                cellSize.Y - 34);

        var needsScroll =
            events.Count * 24 > eventsHeight;

        ImGui.BeginChild(
            $"day_events_{day}",
            new Vector2(0, eventsHeight),
            false,
            needsScroll
                ? ImGuiWindowFlags.None
                : ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        foreach (var evt in events)
        {
            DrawEventChip(evt);
        }

        ImGui.EndChild();

        ImGui.EndChild();
    }

    private static void DrawDayHeader(
        int day,
        DateTime cellDate)
    {
        bool today =
            cellDate.Date == DateTime.Now.Date;

        if (today)
        {
            ImGui.TextColored(
                new Vector4(0.4f, 1f, 0.4f, 1f),
                day.ToString());
        }
        else
        {
            ImGui.Text(day.ToString());
        }

        ImGui.Separator();
    }

    private void DrawEventChip(
        EventData evt)
    {
        var color =
            GetCategoryColor(evt.Category);

        ImGui.PushStyleColor(
            ImGuiCol.Button,
            color);

        ImGui.PushStyleColor(
            ImGuiCol.ButtonHovered,
            color + new Vector4(0.1f, 0.1f, 0.1f, 0));

        ImGui.PushStyleColor(
            ImGuiCol.ButtonActive,
            color);

        var timeText =
            Loc.TimeRangeShort(
                plugin.Configuration.LanguageCode,
                evt.StartTime,
                evt.EndTime);

        var label =
            UiText.FitToAvailableWidth(
                $"{DisplayTitle(evt)} {timeText}",
                8);

        if (ImGui.Button(
                label,
                new Vector2(-1, 22)))
        {
            if (!evt.IsOfficial)
            {
                editorModal.Open(evt);
            }
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(
                $"{DisplayTitle(evt)}\n{Loc.DateTimeRangeLong(
                    plugin.Configuration.LanguageCode,
                    evt.StartTime,
                    evt.EndTime)}");
        }

        ImGui.PopStyleColor(3);
    }

    private void RebuildCurrentMonthEvents()
    {
        currentMonthEvents =
            plugin.DisplayEvents
                .Where(x =>
                    plugin.Configuration.ShowOfficialEvents || !IsLodestoneEvent(x))
                .SelectMany(GetVisibleDaysForEvent)
                .GroupBy(x => x.Day)
                .ToDictionary(
                    x => x.Key,
                    x => x
                        .Select(y => y.Event)
                        .OrderBy(y => y.StartTime)
                        .ToList());
    }

    private IEnumerable<(int Day, EventData Event)> GetVisibleDaysForEvent(
        EventData evt)
    {
        var monthStart =
            currentMonth.Date;

        var monthEnd =
            monthStart.AddMonths(1).AddDays(-1);

        var eventStart =
            evt.StartTime.Date;

        var eventEnd =
            (evt.EndTime ?? evt.StartTime).Date;

        if (eventEnd < monthStart || eventStart > monthEnd)
            yield break;

        var visibleStart =
            eventStart < monthStart
                ? monthStart
                : eventStart;

        var visibleEnd =
            eventEnd > monthEnd
                ? monthEnd
                : eventEnd;

        for (var date = visibleStart; date <= visibleEnd; date = date.AddDays(1))
        {
            yield return (date.Day, evt);
        }
    }

    private static bool IsLodestoneEvent(
        EventData evt)
    {
        return evt.IsOfficial
            && string.Equals(
                evt.Category,
                "Official",
                StringComparison.OrdinalIgnoreCase);
    }

    private Vector4 GetCategoryColor(
        string category)
    {
        return category.ToLower() switch
        {
            "raid" =>
                new Vector4(0.8f, 0.2f, 0.2f, 1f),

            "maps" =>
                new Vector4(0.9f, 0.7f, 0.2f, 1f),

            "fc" =>
                new Vector4(0.2f, 0.5f, 0.9f, 1f),

            "ocean fishing" =>
                new Vector4(0.2f, 0.8f, 0.9f, 1f),

            "official" =>
                new Vector4(0.55f, 0.35f, 0.9f, 1f),

            "daily reset" =>
                new Vector4(0.22f, 0.42f, 0.48f, 1f),

            "weekly reset" =>
                new Vector4(0.35f, 0.48f, 0.25f, 1f),

            "gold saucer" =>
                new Vector4(0.58f, 0.46f, 0.16f, 1f),

            _ =>
                new Vector4(0.35f, 0.35f, 0.35f, 1f)
        };
    }

    private string DisplayTitle(
        EventData evt)
    {
        return evt.IsOfficial
            ? evt.Title
            : evt.Title;
    }
}
