using System;
using System.Numerics;

using Dalamud.Bindings.ImGui;

using EorzeaOutlook.Localization;

namespace EorzeaOutlook.Windows.Components;

public static class ScheduleEditorControls
{
    public static void DrawDateTime(
        string id,
        string languageCode,
        ref DateTime value)
    {
        ImGui.PushID(id);

        int year = Math.Clamp(value.Year, 1, 9999);

        int monthIndex =
            value.Month - 1;

        var months =
            Loc.MonthNames(languageCode);

        ImGui.SetNextItemWidth(180);

        ImGui.Combo(
            Loc.Label(languageCode, "Schedule.Month", "Month", "month"),
            ref monthIndex,
            months,
            months.Length);

        int month =
            Math.Clamp(monthIndex + 1, 1, 12);

        int maxDays =
            DateTime.DaysInMonth(year, month);

        int day =
            Math.Clamp(value.Day, 1, maxDays);

        ImGui.SameLine();

        ImGui.SetNextItemWidth(90);

        ImGui.SliderInt(
            Loc.Label(languageCode, "Schedule.Day", "Day", "day"),
            ref day,
            1,
            maxDays);

        ImGui.SameLine();

        ImGui.SetNextItemWidth(120);

        ImGui.InputInt(
            Loc.Label(languageCode, "Schedule.Year", "Year", "year"),
            ref year);

        year = Math.Clamp(year, 1, 9999);

        maxDays =
            DateTime.DaysInMonth(year, month);

        day = Math.Clamp(day, 1, maxDays);

        ImGui.Spacing();

        int hour24 =
            value.Hour;

        int minute =
            value.Minute;

        bool isPm =
            hour24 >= 12;

        int displayHour =
            hour24 % 12;

        if (displayHour == 0)
        {
            displayHour = 12;
        }

        ImGui.SetNextItemWidth(90);

        ImGui.SliderInt(
            Loc.Label(languageCode, "Schedule.Hour", "Hour", "hour"),
            ref displayHour,
            1,
            12);

        ImGui.SameLine();

        ImGui.SetNextItemWidth(90);

        ImGui.SliderInt(
            Loc.Label(languageCode, "Schedule.Minute", "Minute", "minute"),
            ref minute,
            0,
            59);

        ImGui.SameLine();

        if (ImGui.RadioButton("AM", !isPm))
        {
            isPm = false;
        }

        ImGui.SameLine();

        if (ImGui.RadioButton("PM", isPm))
        {
            isPm = true;
        }

        hour24 =
            displayHour % 12;

        if (isPm)
        {
            hour24 += 12;
        }

        value =
            new DateTime(
                year,
                month,
                day,
                hour24,
                minute,
                0);

        ImGui.PopID();
    }

    public static bool DrawReminderMinutes(
        string id,
        string languageCode,
        ref int reminderMinutes,
        int minimumMinutes,
        int maximumMinutes,
        Vector2? width = null)
    {
        ImGui.PushID(id);

        reminderMinutes =
            Math.Clamp(
                reminderMinutes,
                minimumMinutes,
                maximumMinutes);

        ImGui.SetNextItemWidth(
            width?.X ?? 250);

        var changed =
            ImGui.SliderInt(
                Loc.Label(languageCode, "Schedule.MinutesBefore", "Minutes Before", "minutes_before"),
                ref reminderMinutes,
                minimumMinutes,
                maximumMinutes);

        ImGui.PopID();

        return changed;
    }
}
