using System;
using System.Numerics;

using Dalamud.Bindings.ImGui;

using EorzeaOutlook.Localization;
using EorzeaOutlook.Models;

namespace EorzeaOutlook.Windows.Components;

public class EventEditorModal
{
    private readonly Plugin plugin;

    private bool isOpen = false;

    private EventData? editingEvent;

    private string title = "";

    private string description = "";

    private string category = "Custom";

    private int reminderMinutes = 15;

    private DateTime startTime =
        DateTime.Now;

    private static readonly string[] CategoryCodes =
    [
        "Raid",
        "Maps",
        "FC",
        "Ocean Fishing",
        "Custom"
    ];

    private const string EditEventPopupId =
        "EditEvent";

    public EventEditorModal(Plugin plugin)
    {
        this.plugin = plugin;
    }

    public void Open(EventData evt)
    {
        if (evt.IsOfficial)
            return;

        editingEvent = evt;

        title = evt.Title;

        description = evt.Description;

        category = evt.Category;

        reminderMinutes =
            evt.ReminderMinutesBefore;

        startTime = evt.StartTime;

        isOpen = true;
    }

    public void Draw()
    {
        if (!isOpen || editingEvent == null)
            return;

        ImGui.OpenPopup(EditEventPopupId);

        if (ImGui.BeginPopupModal(
                EditEventPopupId,
                ref isOpen,
                ImGuiWindowFlags.AlwaysAutoResize))
        {
            DrawEditor();

            ImGui.EndPopup();
        }
    }

    private void DrawEditor()
    {
        ImGui.PushStyleVar(
            ImGuiStyleVar.FramePadding,
            new Vector2(10, 10));

        ImGui.TextColored(
            new Vector4(0.4f, 0.8f, 1f, 1f),
            Loc.Text(plugin.Configuration.LanguageCode, "Dialog.EditEvent", "Edit Event"));

        ImGui.Spacing();

        ImGui.Separator();

        ImGui.Spacing();

        ImGui.SetNextItemWidth(600);

        ImGui.InputTextWithHint(
            "##title",
            Loc.Text(plugin.Configuration.LanguageCode, "Dialog.AddTitle", "Add title"),
            ref title,
            100);

        ImGui.Spacing();

        string[] categories =
            Loc.CategoryLabels(plugin.Configuration.LanguageCode);

        int categoryIndex =
            Array.IndexOf(CategoryCodes, category);

        if (categoryIndex < 0)
        {
            categoryIndex = 0;
        }

        ImGui.SetNextItemWidth(220);

        if (ImGui.Combo(
                Loc.Label(plugin.Configuration.LanguageCode, "Dialog.Category", "Category", "edit_category"),
                ref categoryIndex,
                categories,
                categories.Length))
        {
            category =
                CategoryCodes[categoryIndex];
        }

        ImGui.Spacing();

        ImGui.TextColored(
            new Vector4(0.7f, 0.7f, 0.7f, 1f),
            Loc.Text(plugin.Configuration.LanguageCode, "Dialog.Schedule", "Schedule"));

        ImGui.Separator();

        ScheduleEditorControls.DrawDateTime(
            "editSchedule",
            plugin.Configuration.LanguageCode,
            ref startTime);

        ImGui.Spacing();

        ImGui.TextColored(
            new Vector4(0.7f, 0.7f, 0.7f, 1f),
            Loc.Text(plugin.Configuration.LanguageCode, "Dialog.Reminder", "Reminder"));

        ImGui.Separator();

        ScheduleEditorControls.DrawReminderMinutes(
            "editReminder",
            plugin.Configuration.LanguageCode,
            ref reminderMinutes,
            1,
            180);

        ImGui.Spacing();

        ImGui.TextColored(
            new Vector4(0.7f, 0.7f, 0.7f, 1f),
            Loc.Text(plugin.Configuration.LanguageCode, "Dialog.Description", "Description"));

        ImGui.Separator();

        ImGui.InputTextMultiline(
            "##description",
            ref description,
            1000,
            new Vector2(650, 140));

        ImGui.Spacing();

        ImGui.Separator();

        ImGui.Spacing();

        if (ImGui.Button(
                Loc.Text(plugin.Configuration.LanguageCode, "Toolbar.SaveChanges", "Save Changes"),
                new Vector2(180, 40)))
        {
            SaveChanges();
        }

        ImGui.SameLine();

        if (ImGui.Button(
                Loc.Text(plugin.Configuration.LanguageCode, "Dialog.DeleteEvent", "Delete Event"),
                new Vector2(180, 40)))
        {
            DeleteEvent();
        }

        ImGui.SameLine();

        if (ImGui.Button(
                Loc.Text(plugin.Configuration.LanguageCode, "Toolbar.Cancel", "Cancel"),
                new Vector2(140, 40)))
        {
            isOpen = false;
        }

        ImGui.PopStyleVar();
    }

    private void SaveChanges()
    {
        if (editingEvent == null)
            return;

        editingEvent.Title = title;

        editingEvent.Description = description;

        editingEvent.Category = category;

        editingEvent.StartTime = startTime;

        editingEvent.ReminderMinutesBefore =
            reminderMinutes;

        editingEvent.ReminderTriggered = false;

        plugin.Configuration.Save();

        isOpen = false;
    }

    private void DeleteEvent()
    {
        if (editingEvent == null)
            return;

        plugin.Configuration.Events
            .Remove(editingEvent);

        plugin.Configuration.Save();

        isOpen = false;
    }

}
