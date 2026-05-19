using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

using Dalamud.Bindings.ImGui;

using Dalamud.Interface.Textures;
using Dalamud.Interface.Windowing;

using EorzeaOutlook.Localization;
using EorzeaOutlook.Models;
using EorzeaOutlook.Services;
using EorzeaOutlook.Windows.Components;

namespace EorzeaOutlook.Windows;

public class MainWindow : Window, IDisposable
{
    private readonly Plugin plugin;

    private readonly CalendarMonthView calendarView;

    private readonly EventEditorModal editorModal;

    private readonly HashSet<Guid> selectedDeleteEventIds =
        [];

    private bool showCreateModal = false;

    private bool manageEvents = false;

    private string newTitle = "";

    private string newDescription = "";

    private string newCategory = "Custom";

    private int reminderMinutes = 15;

    private bool useDefaultReminder = true;

    private DateTime newEventTime =
        DateTime.Now;

    private bool initializedWindowSize = false;

    private static readonly string[] LanguageCodes =
    [
        "Auto",
        "en",
        "ja",
        "de",
        "fr"
    ];

    private static readonly string[] LanguageNames =
    [
        "Auto",
        "English",
        "Japanese",
        "German",
        "French"
    ];

    private static readonly string[] CategoryCodes =
    [
        "Raid",
        "Maps",
        "FC",
        "Ocean Fishing",
        "Custom"
    ];

    private const string CreateEventPopupId =
        "CreateEvent";

    private const string SettingsPopupId =
        "Eorzea Outlook##SettingsModal";

    public MainWindow(Plugin plugin)
        : base(
            "Eorzea Outlook",
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        this.plugin = plugin;

        editorModal =
            new EventEditorModal(plugin);

        calendarView =
            new CalendarMonthView(
                plugin,
                editorModal);

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new(720, 560),
            MaximumSize = new(2200, 2000)
        };

        Size = new Vector2(720, 560);
    }

    public override void Draw()
    {
        ApplyResponsiveWindowSize();

        var compact =
            ImGui.GetWindowSize().X < 1150;

        DrawToolbar();

        ImGui.Separator();

        DrawLayout(compact);

        DrawCreateEventModal();

        editorModal.Draw();
    }

    private void DrawToolbar()
    {
        var languageCode =
            plugin.Configuration.LanguageCode;

        var compact =
            ImGui.GetWindowSize().X < 1150;

        var buttonHeight =
            compact ? 30 : 35;

        if (ImGui.Button(
                Loc.Text(languageCode, "Toolbar.NewEvent", "+ New Event"),
                new Vector2(compact ? 112 : 140, buttonHeight)))
        {
            ResetInputs();

            showCreateModal = true;
        }

        ImGui.SameLine();

        if (ImGui.Button(
                Loc.Text(languageCode, "Toolbar.Today", "Today"),
                new Vector2(compact ? 72 : 90, buttonHeight)))
        {
            calendarView.GoToToday();
        }

        ImGui.SameLine();

        if (ImGui.Button(
                manageEvents
                    ? Loc.Text(languageCode, "Toolbar.Cancel", "Cancel")
                    : Loc.Text(languageCode, "Toolbar.ManageEvents", "Manage Events"),
                new Vector2(compact ? 120 : 150, buttonHeight)))
        {
            manageEvents =
                !manageEvents;

            selectedDeleteEventIds.Clear();
        }

        if (manageEvents)
        {
            ImGui.SameLine();

            if (ImGui.Button(
                    Loc.Text(languageCode, "Toolbar.SaveChanges", "Save Changes"),
                    new Vector2(compact ? 130 : 160, buttonHeight)))
            {
                SaveManagedChanges();
            }
        }

        DrawEorzeaStatus(
            buttonHeight);

        DrawSettingsButton(
            buttonHeight);
    }

    private void DrawEorzeaStatus(
        int buttonHeight)
    {
        var status =
            plugin.EorzeaTimeWeatherService.GetStatus();

        var eorzeaTimeText =
            $"ET {status.EorzeaTime:HH:mm}";

        var windowWidth =
            ImGui.GetWindowWidth();

        var style =
            ImGui.GetStyle();

        var settingsButtonWidth =
            buttonHeight;

        var settingsButtonLeft =
            windowWidth
            - style.WindowPadding.X
            - settingsButtonWidth;

        var iconSize =
            status.WeatherIconId > 0
                ? ImGui.GetTextLineHeight()
                : 0;

        var openSpaceEnd =
            settingsButtonLeft
            - style.ItemSpacing.X;

        var minimumX =
            ImGui.GetCursorPosX()
            + style.ItemSpacing.X;

        var availableWidth =
            openSpaceEnd - minimumX;

        if (availableWidth <= 0)
            return;

        var statusText =
            GetStatusText(
                status,
                eorzeaTimeText,
                availableWidth,
                iconSize,
                out var statusWidth);

        if (string.IsNullOrWhiteSpace(statusText))
            return;

        var statusX =
            openSpaceEnd - statusWidth;

        ImGui.SameLine();

        ImGui.SetCursorPosX(statusX);

        ImGui.TextColored(
            string.IsNullOrWhiteSpace(status.WeatherName)
                ? new Vector4(0.65f, 0.85f, 1f, 1f)
                : status.WeatherColor,
            statusText);

        DrawWeatherIcon(status);

        if (ImGui.IsItemHovered()
            && !string.IsNullOrWhiteSpace(status.PlaceName))
        {
            ImGui.SetTooltip(
                $"{status.PlaceName}\n{eorzeaTimeText} | {status.WeatherName}");
        }
    }

    private static string GetStatusText(
        EorzeaStatus status,
        string eorzeaTimeText,
        float availableWidth,
        float iconSize,
        out float statusWidth)
    {
        var fullText =
            string.IsNullOrWhiteSpace(status.WeatherName)
                ? eorzeaTimeText
                : $"{eorzeaTimeText}  |  {status.WeatherName}";

        if (FitsStatusWidth(
                fullText,
                iconSize,
                availableWidth,
                out statusWidth))
        {
            return fullText;
        }

        var compactText =
            iconSize > 0
                ? eorzeaTimeText
                : fullText;

        if (FitsStatusWidth(
                compactText,
                iconSize,
                availableWidth,
                out statusWidth))
        {
            return compactText;
        }

        statusWidth = 0;

        return "";
    }

    private static bool FitsStatusWidth(
        string text,
        float iconSize,
        float availableWidth,
        out float statusWidth)
    {
        var iconSeparatorText =
            iconSize > 0
                ? "  |"
                : "";

        statusWidth =
            ImGui.CalcTextSize(text + iconSeparatorText).X
            + (iconSize > 0
                ? iconSize + ImGui.GetStyle().ItemInnerSpacing.X
                : 0);

        return statusWidth <= availableWidth;
    }

    private static void DrawWeatherIcon(
        EorzeaStatus status)
    {
        if (status.WeatherIconId == 0)
            return;

        var icon =
            Plugin.TextureProvider.GetFromGameIcon(
                new GameIconLookup
                {
                    IconId = status.WeatherIconId
                });

        using var wrap =
            icon.GetWrapOrDefault(null);

        if (wrap == null)
        {
            ImGui.SameLine();

            ImGui.TextColored(
                status.WeatherColor,
                status.WeatherIcon);

            return;
        }

        var size =
            new Vector2(
                ImGui.GetTextLineHeight(),
                ImGui.GetTextLineHeight());

        ImGui.SameLine();

        ImGui.TextColored(
            status.WeatherColor,
            "|");

        ImGui.SameLine(
            0,
            ImGui.GetStyle().ItemInnerSpacing.X);

        var cursorY =
            ImGui.GetCursorPosY();

        ImGui.SetCursorPosY(
            cursorY + 1);

        ImGui.Image(
            wrap.Handle,
            size);

        ImGui.SetCursorPosY(cursorY);
    }

    private void DrawSettingsButton(
        int buttonHeight)
    {
        var style =
            ImGui.GetStyle();

        var buttonWidth =
            buttonHeight;

        var targetX =
            ImGui.GetWindowWidth()
            - style.WindowPadding.X
            - buttonWidth;

        var nextX =
            ImGui.GetCursorPosX()
            + style.ItemSpacing.X;

        if (targetX > nextX)
        {
            ImGui.SameLine();

            ImGui.SetCursorPosX(targetX);
        }

        if (ImGui.Button(
                "⚙##settings",
                new Vector2(buttonWidth, buttonHeight)))
        {
            ImGui.OpenPopup(SettingsPopupId);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(
                Loc.Text(
                    plugin.Configuration.LanguageCode,
                    "Settings.Tooltip",
                    "Settings"));
        }

        DrawSettingsPopup();
    }

    private void DrawSettingsPopup()
    {
        var windowCenter =
            ImGui.GetWindowPos()
            + (ImGui.GetWindowSize() * 0.5f);

        ImGui.SetNextWindowPos(
            windowCenter,
            ImGuiCond.Appearing,
            new Vector2(0.5f, 0.5f));

        ImGui.SetNextWindowSize(
            new Vector2(360, 0),
            ImGuiCond.Appearing);

        if (!ImGui.BeginPopupModal(
                SettingsPopupId,
                ImGuiWindowFlags.AlwaysAutoResize))
            return;

        ImGui.TextColored(
            new Vector4(1f, 0.85f, 0.3f, 1f),
            Loc.Text(
                plugin.Configuration.LanguageCode,
                "Settings.Title",
                "Settings"));

        ImGui.Separator();

        ImGui.Spacing();

        var languageIndex =
            Array.IndexOf(
                LanguageCodes,
                plugin.Configuration.LanguageCode);

        if (languageIndex < 0)
        {
            languageIndex = 0;
        }

        ImGui.SetNextItemWidth(180);

        if (ImGui.Combo(
                Loc.Label(
                    plugin.Configuration.LanguageCode,
                    "Settings.Language",
                    "Language",
                    "settings_language"),
                ref languageIndex,
                LanguageNames,
                LanguageNames.Length))
        {
            plugin.Configuration.LanguageCode =
                LanguageCodes[languageIndex];

            plugin.Configuration.Save();

            plugin.RefreshOfficialEvents(false);
        }

        ImGui.TextDisabled(
            Loc.Text(
                plugin.Configuration.LanguageCode,
                "Settings.Note",
                "Applies to plugin text."));

        ImGui.Spacing();

        var showInGameResetEvents =
            plugin.Configuration.ShowInGameResetEvents;

        if (ImGui.Checkbox(
                Loc.Label(
                    plugin.Configuration.LanguageCode,
                    "Settings.ShowResets",
                    "Show reset events",
                    "settings_show_resets"),
                ref showInGameResetEvents))
        {
            plugin.Configuration.ShowInGameResetEvents =
                showInGameResetEvents;

            plugin.Configuration.Save();
        }

        ImGui.TextDisabled(
            Loc.Text(
                plugin.Configuration.LanguageCode,
                "Settings.ShowResetsNote",
                "Choose which recurring in-game reset entries appear."));

        if (plugin.Configuration.ShowInGameResetEvents)
        {
            ImGui.Spacing();

            DrawResetEventOptions();
        }

        ImGui.Spacing();

        var showOfficialEvents =
            plugin.Configuration.ShowOfficialEvents;

        if (ImGui.Checkbox(
                Loc.Label(
                    plugin.Configuration.LanguageCode,
                    "Settings.ShowOfficialEvents",
                    "Show Lodestone events",
                    "settings_show_official_events"),
                ref showOfficialEvents))
        {
            plugin.Configuration.ShowOfficialEvents =
                showOfficialEvents;

            plugin.Configuration.Save();
        }

        ImGui.Spacing();

        ImGui.Separator();

        ImGui.Spacing();

        if (ImGui.Button(
                Loc.Text(
                    plugin.Configuration.LanguageCode,
                    "Toolbar.Ok",
                    "OK"),
                new Vector2(120, 32)))
        {
            ImGui.CloseCurrentPopup();
        }

        ImGui.EndPopup();
    }

    private void DrawResetEventOptions()
    {
        var firstColumnWidth =
            170f;

        plugin.Configuration.ShowDailyResetEvents =
            DrawResetEventCheckbox(
                "Settings.ResetDaily",
                "Daily",
                "settings_reset_daily",
                plugin.Configuration.ShowDailyResetEvents);

        ImGui.SameLine(firstColumnWidth);

        plugin.Configuration.ShowWeeklyResetEvents =
            DrawResetEventCheckbox(
                "Settings.ResetWeekly",
                "Weekly",
                "settings_reset_weekly",
                plugin.Configuration.ShowWeeklyResetEvents);

        plugin.Configuration.ShowFashionReportEvents =
            DrawResetEventCheckbox(
                "Settings.ResetFashionReport",
                "Fashion Report",
                "settings_reset_fashion",
                plugin.Configuration.ShowFashionReportEvents);

        ImGui.SameLine(firstColumnWidth);

        plugin.Configuration.ShowJumboCactpotEvents =
            DrawResetEventCheckbox(
                "Settings.ResetJumboCactpot",
                "Jumbo Cactpot",
                "settings_reset_cactpot",
                plugin.Configuration.ShowJumboCactpotEvents);
    }

    private bool DrawResetEventCheckbox(
        string labelKey,
        string fallback,
        string id,
        bool value)
    {
        if (ImGui.Checkbox(
                Loc.Label(
                    plugin.Configuration.LanguageCode,
                    labelKey,
                    fallback,
                    id),
                ref value))
        {
            plugin.Configuration.Save();
        }

        return value;
    }

    private void DrawLayout(
        bool compact)
    {
        var layoutHeight =
            Math.Max(
                480,
                ImGui.GetWindowSize().Y
                - ImGui.GetCursorPosY()
                - ImGui.GetStyle().WindowPadding.Y);

        var availableWidth =
            ImGui.GetContentRegionAvail().X;

        var sidebarWidth =
            compact
                ? Math.Clamp(availableWidth * 0.24f, 220, 280)
                : Math.Clamp(availableWidth * 0.26f, 280, 340);

        ImGui.BeginChild(
            "sidebar",
            new Vector2(sidebarWidth, layoutHeight),
            false,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        DrawSidebar(compact);

        ImGui.EndChild();

        ImGui.SameLine();

        ImGui.BeginChild(
            "calendar",
            new Vector2(0, layoutHeight),
            false,
            ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);

        calendarView.Draw(
            layoutHeight,
            compact);

        ImGui.EndChild();
    }

    private void DrawSidebar(
        bool compact)
    {
        ImGui.TextColored(
            new Vector4(1f, 0.85f, 0.3f, 1f),
            manageEvents
                ? Loc.Text(plugin.Configuration.LanguageCode, "Sidebar.Events", "Events")
                : Loc.Text(plugin.Configuration.LanguageCode, "Sidebar.Upcoming", "Upcoming"));

        if (manageEvents)
        {
            DrawSelectAllForDeletion();

            ImGui.TextDisabled(
                Loc.Format(
                    plugin.Configuration.LanguageCode,
                    "Sidebar.SelectedForDeletion",
                    "{0} selected for deletion",
                    selectedDeleteEventIds.Count));
        }

        ImGui.Spacing();

        var ordered =
            manageEvents
                ? plugin.Configuration.Events
                    .OrderBy(x => x.StartTime)
                    .ToList()
                : plugin.DisplayEvents
                    .Where(x => (x.EndTime ?? x.StartTime) >= DateTime.Now)
                    .Where(x => !IsResetEvent(x))
                    .Where(x => plugin.Configuration.ShowOfficialEvents || !IsLodestoneEvent(x))
                    .OrderBy(x => x.StartTime)
                    .ToList();

        var childHeight =
            ImGui.GetContentRegionAvail().Y;

        ImGui.BeginChild(
            "upcomingEvents",
            new Vector2(0, childHeight),
            false);

        if (ordered.Count == 0)
        {
            ImGui.TextDisabled(
                manageEvents
                    ? Loc.Text(plugin.Configuration.LanguageCode, "Sidebar.NoEventsToManage", "No events to manage.")
                    : Loc.Text(plugin.Configuration.LanguageCode, "Sidebar.NoUpcomingEvents", "No upcoming events."));

            ImGui.EndChild();

            return;
        }

        foreach (var evt in ordered)
        {
            DrawSidebarEvent(
                evt,
                compact);
        }

        ImGui.EndChild();
    }

    private void DrawSelectAllForDeletion()
    {
        var selectableEvents =
            plugin.Configuration.Events;

        var allSelected =
            selectableEvents.Count > 0
            && selectedDeleteEventIds.Count >= selectableEvents.Count
            && selectableEvents.All(x =>
                selectedDeleteEventIds.Contains(x.Id));

        if (ImGui.Checkbox(
                Loc.Label(
                    plugin.Configuration.LanguageCode,
                    "Sidebar.SelectAll",
                    "Select all",
                    "delete_all"),
                ref allSelected))
        {
            selectedDeleteEventIds.Clear();

            if (allSelected)
            {
                foreach (var evt in selectableEvents)
                {
                    selectedDeleteEventIds.Add(evt.Id);
                }
            }
        }
    }

    private void DrawSidebarEvent(
        EventData evt,
        bool compact)
    {
        ImGui.PushID(evt.Id.ToString());

        ImGui.BeginChild(
            $"sidebar_{evt.Id}",
            new Vector2(0, compact ? 60 : 72),
            true);

        var title =
            UiText.FitToAvailableWidth(evt.Title);

        if (manageEvents && !evt.IsOfficial)
        {
            var selected =
                selectedDeleteEventIds.Contains(evt.Id);

            if (ImGui.Checkbox(
                    "##delete",
                    ref selected))
            {
                if (selected)
                {
                    selectedDeleteEventIds.Add(evt.Id);
                }
                else
                {
                    selectedDeleteEventIds.Remove(evt.Id);
                }
            }

            ImGui.SameLine();

            ImGui.Text(title);
        }
        else if (evt.IsOfficial)
        {
            ImGui.Text(title);
        }
        else if (ImGui.Selectable(
                     title,
                     false,
                     ImGuiSelectableFlags.AllowDoubleClick))
        {
            editorModal.Open(evt);
        }

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(evt.Title);
        }

        ImGui.Spacing();

        var timeText =
            Loc.DateTimeRangeLong(
                plugin.Configuration.LanguageCode,
                evt.StartTime,
                evt.EndTime);

        ImGui.Text(
            UiText.FitToAvailableWidth(timeText));

        if (ImGui.IsItemHovered())
        {
            ImGui.SetTooltip(
                $"{evt.Title}\n{Loc.DateTimeRangeLong(
                    plugin.Configuration.LanguageCode,
                    evt.StartTime,
                    evt.EndTime)}");
        }

        ImGui.EndChild();

        ImGui.PopID();

        ImGui.Spacing();
    }

    private static bool IsResetEvent(
        EventData evt)
    {
        return evt.Category is "Daily Reset" or "Weekly Reset" or "Gold Saucer";
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

    private void ApplyResponsiveWindowSize()
    {
        if (initializedWindowSize)
            return;

        var viewport =
            ImGui.GetMainViewport();

        var targetSize =
            new Vector2(
                Math.Clamp(viewport.Size.X * 0.45f, 720, 1000),
                Math.Clamp(viewport.Size.Y * 0.58f, 560, 760));

        Size =
            targetSize;

        SizeConstraints =
            new WindowSizeConstraints
            {
                MinimumSize = new(720, 560),
                MaximumSize = viewport.Size * 0.96f
            };

        initializedWindowSize = true;
    }

    private void SaveManagedChanges()
    {
        if (selectedDeleteEventIds.Count > 0)
        {
            plugin.Configuration.Events
                .RemoveAll(x =>
                    selectedDeleteEventIds.Contains(x.Id));

            plugin.Configuration.Save();
        }

        selectedDeleteEventIds.Clear();

        manageEvents = false;
    }

    private void DrawCreateEventModal()
{
    if (!showCreateModal)
        return;

    ImGui.OpenPopup(CreateEventPopupId);

    if (ImGui.BeginPopupModal(
            CreateEventPopupId,
            ref showCreateModal,
            ImGuiWindowFlags.AlwaysAutoResize))
    {
        ImGui.PushStyleVar(
            ImGuiStyleVar.FramePadding,
            new Vector2(10, 10));

        ImGui.PushFont(ImGui.GetFont());

        ImGui.TextColored(
            new Vector4(0.4f, 0.8f, 1f, 1f),
            Loc.Text(plugin.Configuration.LanguageCode, "Dialog.NewEvent", "New Event"));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        ImGui.SetNextItemWidth(600);

        ImGui.InputTextWithHint(
            "##title",
            Loc.Text(plugin.Configuration.LanguageCode, "Dialog.AddTitle", "Add title"),
            ref newTitle,
            100);

        ImGui.Spacing();

        string[] categories =
            Loc.CategoryLabels(plugin.Configuration.LanguageCode);

        int categoryIndex =
            Array.IndexOf(CategoryCodes, newCategory);

        if (categoryIndex < 0)
        {
            categoryIndex = 0;
        }

        ImGui.SetNextItemWidth(220);

        if (ImGui.Combo(
                Loc.Label(plugin.Configuration.LanguageCode, "Dialog.Category", "Category", "create_category"),
                ref categoryIndex,
                categories,
                categories.Length))
        {
            newCategory =
                CategoryCodes[categoryIndex];
        }

        ImGui.Spacing();

        ImGui.TextColored(
            new Vector4(0.7f, 0.7f, 0.7f, 1f),
            Loc.Text(plugin.Configuration.LanguageCode, "Dialog.Schedule", "Schedule"));

        ImGui.Separator();

        ScheduleEditorControls.DrawDateTime(
            "createSchedule",
            plugin.Configuration.LanguageCode,
            ref newEventTime);

        ImGui.Spacing();

        ImGui.TextColored(
            new Vector4(0.7f, 0.7f, 0.7f, 1f),
            Loc.Text(plugin.Configuration.LanguageCode, "Dialog.Reminder", "Reminder"));

        ImGui.Separator();

        var minutesUntilEvent =
            (int)Math.Floor(
                (newEventTime - DateTime.Now).TotalMinutes);

        var maxReminderMinutes =
            Math.Clamp(minutesUntilEvent, 0, 180);

        if (useDefaultReminder)
        {
            reminderMinutes =
                Math.Min(15, maxReminderMinutes);
        }
        else
        {
            reminderMinutes =
                Math.Clamp(reminderMinutes, 0, maxReminderMinutes);
        }

        if (ScheduleEditorControls.DrawReminderMinutes(
                "createReminder",
                plugin.Configuration.LanguageCode,
                ref reminderMinutes,
                0,
                maxReminderMinutes))
        {
            useDefaultReminder = false;
        }

        ImGui.Spacing();

        ImGui.TextColored(
            new Vector4(0.7f, 0.7f, 0.7f, 1f),
            Loc.Text(plugin.Configuration.LanguageCode, "Dialog.Description", "Description"));

        ImGui.Separator();

        ImGui.InputTextMultiline(
            "##description",
            ref newDescription,
            1000,
            new Vector2(650, 140));

        ImGui.Spacing();
        ImGui.Separator();
        ImGui.Spacing();

        if (ImGui.Button(
                Loc.Text(plugin.Configuration.LanguageCode, "Dialog.SaveEvent", "Save Event"),
                new Vector2(160, 40)))
        {
            if (!string.IsNullOrWhiteSpace(newTitle))
            {
                plugin.Configuration.Events.Add(
                    new EventData
                    {
                        Title = newTitle,
                        Description = newDescription,
                        Category = newCategory,
                        StartTime = newEventTime,
                        ReminderMinutesBefore =
                            reminderMinutes
                    });

                plugin.Configuration.Save();

                ResetInputs();

                showCreateModal = false;
            }
        }

        ImGui.SameLine();

        if (ImGui.Button(
                Loc.Text(plugin.Configuration.LanguageCode, "Toolbar.Cancel", "Cancel"),
                new Vector2(140, 40)))
        {
            showCreateModal = false;
        }

        ImGui.PopStyleVar();

        ImGui.PopFont();

        ImGui.EndPopup();
    }
}

    private void ResetInputs()
    {
        newTitle = "";

        newDescription = "";

        newCategory = "Custom";

        reminderMinutes = 15;

        useDefaultReminder = true;

        newEventTime =
            DateTime.Now;
    }

    public void Dispose()
    {
    }
}
