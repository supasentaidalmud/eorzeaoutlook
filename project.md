# EorzeaOutlook Project Reference

Last reviewed: 2026-05-08

## Purpose

EorzeaOutlook is a Dalamud plugin for Final Fantasy XIV that provides an in-game calendar for personal events, reminder notifications, and official Lodestone event tracking. The in-game commands are `/pcalendar` and `/pcal`, which open the main Eorzea Outlook window.

This file documents the current build so future work can track what changed, where features live, and which Dalamud APIs are being used.

## External API Reference

Use the live Dalamud API documentation as the coding reference:

- Dalamud API index: https://dalamud.dev/api/
- Current index shown during this review: `15.x (API 15) [Current]`.
- The project SDK is `Dalamud.NET.Sdk/15.0.0`, so new code should prefer API 15 documentation where available.

Relevant Dalamud API areas for this plugin:

- `Dalamud.Plugin.IDalamudPlugin`: base plugin interface; plugins implement `IDisposable`.
- `Dalamud.IoC.PluginServiceAttribute`: injects Dalamud services into static plugin properties.
- `Dalamud.Configuration.IPluginConfiguration`: configuration contract with a `Version` property.
- `Dalamud.Plugin.Services.ICommandManager`: registers and removes slash commands with `AddHandler` and `RemoveHandler`.
- `Dalamud.Interface.Windowing.WindowSystem`: owns the render list for plugin windows and draws registered windows via `Draw()`.
- `Dalamud.Plugin.Services.IFramework`: exposes the framework update thread and `Update` event used here for periodic plugin work.
- `Dalamud.Plugin.Services.INotificationManager`: creates ImGui notifications through `AddNotification`.
- `Dalamud.Plugin.Services.IClientState`: provides current territory for weather lookup.
- `Dalamud.Plugin.Services.IDataManager`: provides Lumina Excel sheets used for territory/weather data.
- `Dalamud.Plugin.Services.ITextureProvider`: loads game weather icon textures by icon ID for ImGui rendering.

## Repository Layout

- `EorzeaOutlook.sln`: Visual Studio solution for the plugin.
- `EorzeaOutlook/EorzeaOutlook.csproj`: Dalamud SDK project file.
- `EorzeaOutlook/EorzeaOutlook.json`: Dalamud plugin manifest.
- `repo.json`: custom Dalamud plugin repository entry.
- `pluginmaster.json`: compatibility alias for clients expecting the older common filename.
- `images/`: custom repository icon assets.
- `EorzeaOutlook/Plugin.cs`: plugin entry point, service injection, command registration, UI draw hook, framework update hook.
- `EorzeaOutlook/Configuration.cs`: persisted plugin configuration.
- `EorzeaOutlook/Models/EventData.cs`: event data model shared by custom and official events.
- `EorzeaOutlook/Services/ReminderService.cs`: checks custom event reminders and posts notifications.
- `EorzeaOutlook/Services/EorzeaTimeWeatherService.cs`: computes current Eorzea time and resolves current territory weather.
- `EorzeaOutlook/Services/LodestoneTopicsService.cs`: fetches, parses, caches, and applies official Lodestone event data.
- `EorzeaOutlook/Localization/Loc.cs`: lightweight localization and formatting helper.
- `EorzeaOutlook/Windows/MainWindow.cs`: primary calendar window and top-level UI workflow.
- `EorzeaOutlook/Windows/Components/CalendarMonthView.cs`: month grid and event chips.
- `EorzeaOutlook/Windows/Components/EventEditorModal.cs`: edit/delete modal for custom events.
- `EorzeaOutlook/Windows/Components/ScheduleEditorControls.cs`: shared date/time and reminder controls.
- `EorzeaOutlook/Windows/Components/UiText.cs`: text fitting/ellipsis helper.
- `README.md`: short build and dev-plugin loading notes.
- `LICENSE.md`: AGPL-3.0-or-later license text.

## Build Configuration

- Project SDK: `Dalamud.NET.Sdk/15.0.0`.
- Package version: `0.0.0.6`.
- License metadata: `AGPL-3.0-or-later`.
- Project URL metadata: `https://github.com/supasentaidalmud/eorzeaoutlook`.
- Solution configurations: `Debug|x64` and `Release|x64`.
- Lock file dependencies:
  - `DalamudPackager` `15.0.0`
  - `DotNet.ReproducibleBuilds` `1.2.39`
- README dev plugin path: `C:\temp\EorzeaOutlook\EorzeaOutlook\bin\x64\Debug`

## Plugin Manifest

`EorzeaOutlook/EorzeaOutlook.json` currently declares:

- `Name`: `Eorzea Outlook`
- `Punchline`: `Calendar reminders and official Lodestone event tracking.`
- `Description`: calendar for personal events, reminders, and official Lodestone in-game events; mentions `/pcalendar`.
- `IconUrl`: `https://raw.githubusercontent.com/supasentaidalmud/eorzeaoutlook/master/images/icon-128.png`
- `RepoUrl`: `https://github.com/supasentaidalmud/eorzeaoutlook`
- `ApplicableVersion`: `any`
- `Tags`: `calendar`, `events`, `reminders`

Current author: `supasentaidalmud`.

## Runtime Architecture

`Plugin` is the composition root. It:

- Receives Dalamud services via `[PluginService]` static properties:
  - `IDalamudPluginInterface`
  - `ICommandManager`
  - `IPluginLog`
  - `IFramework`
  - `INotificationManager`
  - `IClientState`
  - `IDataManager`
- Loads persisted `Configuration` with `PluginInterface.GetPluginConfig()`.
- Initializes configuration so `Configuration.Save()` can call `SavePluginConfig`.
- Creates `MainWindow`, `ReminderService`, and `LodestoneTopicsService`.
- Creates `EorzeaTimeWeatherService` for top-bar Eorzea time and weather display.
- Starts official-event cache warmup on load with `WarmLanguageCachesIfNeeded()`.
- Adds `MainWindow` to a `WindowSystem` named `EorzeaOutlook`.
- Registers `/pcalendar`.
- Hooks:
  - `PluginInterface.UiBuilder.Draw`
  - `PluginInterface.UiBuilder.OpenMainUi`
  - `PluginInterface.UiBuilder.OpenConfigUi`
  - `Framework.Update`
- On framework update:
  - Applies pending Lodestone refresh results.
  - Runs reminder checks every 15 seconds.
  - Starts Lodestone refreshes when cache timing allows.
- On dispose:
  - Unhooks framework and UI callbacks.
  - Removes all windows from `WindowSystem`.
  - Disposes `MainWindow` and `LodestoneTopicsService`.
  - Removes `/pcalendar`.

`Plugin.DisplayEvents` merges `Configuration.Events` with `LodestoneTopicsService.OfficialEvents`.

## Persisted Configuration

`Configuration` implements `IPluginConfiguration`.

Fields:

- `Version`: config schema version, currently `1`.
- `Events`: user-created custom events.
- `OfficialEvents`: legacy/current-language official event cache.
- `OfficialEventsByLanguage`: official event caches keyed by language code.
- `OfficialEventsRefreshTimesByLanguage`: refresh timestamps keyed by language code.
- `OfficialEventsLastRefresh`: legacy/current-language refresh timestamp.
- `OfficialEventsLanguageCode`: language code for `OfficialEvents`.
- `LanguageCode`: UI language choice; defaults to `Auto`.

Non-serialized runtime field:

- `IDalamudPluginInterface? pluginInterface`, assigned by `Initialize()` and used by `Save()`.

## Event Model

`EventData` is serializable and used for both custom and official events.

Fields:

- `Id`: `Guid`, defaults to `Guid.NewGuid()`.
- `Title`: event title.
- `Description`: event description.
- `StartTime`: local start time, defaulting to `DateTime.Now`.
- `EndTime`: optional local end time.
- `ReminderMinutesBefore`: custom reminder lead time, default `15`.
- `ReminderTriggered`: reminder state flag.
- `Category`: default `Custom`.
- `IsOfficial`: distinguishes Lodestone events from custom events.
- `SourceUrl`: Lodestone URL for official events.

## User Workflows

### Opening

- `/pcalendar` toggles the main window.
- `/pcal` also toggles the main window.
- Dalamud main/config UI open callbacks also toggle the main window.

### Main Window

The main window title is `Eorzea Outlook`.

Layout:

- Toolbar across the top.
- Sidebar on the left.
- Month calendar on the right.
- Responsive sizing based on viewport.
- Initial window size is `720x560`, then first draw clamps responsive size between `720x560` and `1000x760`; maximum is 96% of viewport after first draw.

Toolbar actions:

- `New Event`: opens the create-event modal.
- `Today`: returns the calendar view to the current month.
- `Manage Events` / `Cancel`: toggles deletion management mode for custom events.
- `Save Changes`: deletes selected custom events while in management mode.
- Settings gear: opens language settings modal.

Top status:

- Shows centered Eorzea time in the top toolbar area.
- When current territory weather data resolves, also shows current weather with a small text icon and weather-colored label.
- Weather display uses the game's weather icon texture after a separator when available, with a text-symbol fallback.
- Weather tooltip includes current place name.
- At narrow widths, status falls back from `ET HH:mm | Weather | icon` to `ET HH:mm | icon`, and hides only if the compact form cannot fit.

Settings:

- Language selector.

Sidebar modes:

- Normal mode: shows upcoming events from custom plus official sources.
- Manage mode: shows custom events only with checkboxes for deletion.
- Manage mode includes a select-all checkbox that selects every custom event for deletion, or clears all selections when unchecked.
- Official events are not editable or selectable for deletion.
- Upcoming/manage lists fill the remaining sidebar height and scroll only when their content exceeds the available space.

### Creating Events

The create modal collects:

- Title, required for save.
- Category: `Raid`, `Maps`, `FC`, `Ocean Fishing`, `Custom`.
- Start date/time.
- Reminder minutes before event.
- Description.

Create behavior:

- New events are appended to `Configuration.Events`.
- `ReminderMinutesBefore` is clamped based on time until the event.
- `EndTime` is not currently exposed for custom event creation.
- Save persists configuration immediately.

### Editing Events

Custom events can be opened from the sidebar or calendar chips. Official events cannot be edited.

The edit modal supports:

- Title.
- Description.
- Category.
- Start date/time.
- Reminder minutes before event, clamped from 1 to 180.
- Save changes.
- Delete event.
- Cancel.

Save behavior:

- Updates the existing `EventData` instance.
- Resets `ReminderTriggered` to `false`.
- Persists configuration.

### Calendar Month View

`CalendarMonthView` owns:

- Current month state.
- `showOfficialEvents` toggle.
- Per-month event grouping cache.

Calendar behavior:

- Previous/next month buttons change `currentMonth`.
- `GoToToday()` resets to current local month.
- Day headers are localized.
- Current day number is green.
- Events are shown as colored ImGui buttons/chips.
- Events spanning multiple days appear on each visible day in the current month.
- Clicking a custom event chip opens the editor.
- Clicking an official event chip does nothing, but hover shows details.

Category colors:

- `Raid`: red.
- `Maps`: yellow.
- `FC`: blue.
- `Ocean Fishing`: cyan.
- `Official`: purple.
- Other/custom: gray.

## Reminder Service

`ReminderService.CheckReminders()`:

- Uses `DateTime.Now`.
- Scans a snapshot of `plugin.Configuration.Events`, so official Lodestone events do not trigger reminders and UI edits cannot modify the collection while it is being enumerated.
- Skips events where `ReminderTriggered` is already true.
- Computes reminder time as `StartTime - ReminderMinutesBefore`.
- Triggers if current time is after the reminder time but not more than 1 minute after event start.
- Marks reminder as triggered and saves configuration.
- Logs the reminder.
- Adds a Dalamud ImGui notification with localized title and event start time.

Important behavior:

- Reminder checks are scheduled by `Plugin.OnFrameworkUpdate()` every 15 seconds.

## Eorzea Time and Weather

`EorzeaTimeWeatherService` provides the centered top-bar status.

Behavior:

- Computes Eorzea time locally from UTC time using the standard Eorzea time multiplier.
- Uses `IClientState.TerritoryType` to detect the current zone.
- Uses `IDataManager.GetExcelSheet<TerritoryType>(ClientLanguage?)` and `IDataManager.GetExcelSheet<Weather>(ClientLanguage?)` to resolve localized place name and weather text.
- Calculates the current weather bucket from real time using the FFXIV weather chance algorithm and accumulated `WeatherRate` probabilities.
- Returns a small display model containing Eorzea time, place name, weather name, weather icon ID, fallback text icon, and color.
- If territory or weather data cannot be resolved, the UI still shows Eorzea time and omits weather.
- Weather and place names use the plugin's effective language setting, including `Auto`, to match the rest of the UI.

Weather icons are intentionally lightweight text symbols:

- `o`: clear/fair weather.
- `~`: rain/showers.
- `*`: clouds/fog.
- `!`: thunder.
- `+`: snow/blizzard.

## Lodestone Official Events

`LodestoneTopicsService` fetches official event data from Lodestone topic pages.

Supported language codes:

- `en`: `https://na.finalfantasyxiv.com/lodestone/topics/`
- `ja`: `https://jp.finalfantasyxiv.com/lodestone/topics/`
- `de`: `https://de.finalfantasyxiv.com/lodestone/topics/`
- `fr`: `https://fr.finalfantasyxiv.com/lodestone/topics/`

Fetch/parsing flow:

- Builds the language-specific topics URI.
- Fetches HTML with `HttpClient.GetStringAsync`.
- Parses `<li>` blocks with `TopicListItemRegex`.
- Looks for localized event period text with language-specific regexes.
- Extracts source URL and title from the first anchor when available.
- Falls back to title extraction from text before the event-period marker.
- Converts source event times into local time.
- Marks parsed events as:
  - `Category = "Official"`
  - `IsOfficial = true`
  - `ReminderMinutesBefore = 0`
  - `ReminderTriggered = true`
  - `SourceUrl = sourceUrl`
- Uses an MD5 hash of source URL plus language code as a stable GUID.
- Groups by `SourceUrl` to avoid duplicate entries.

Refresh/caching behavior:

- `WarmLanguageCachesIfNeeded()` tries to populate all supported language caches on startup.
- `RefreshIfNeeded(force: false)` loads cached events for the active effective language first.
- Fresh caches are reused for 1 day.
- Official event caches retain active/future events and recently-ended events, trimming entries that ended more than 14 days ago.
- Successful refreshes set the next attempt at 15 minutes.
- Empty or failed refreshes retry after 5 minutes.
- Network refresh runs asynchronously.
- Refresh tasks capture a local `CancellationTokenSource` before starting, avoiding races with later disposal or refresh attempts.
- Parsed results are stored as pending data under a lock.
- `ApplyPendingRefresh()` is called on framework update to update configuration and current `OfficialEvents`.

Language fallback:

- If a non-English fetch parses no events, the service falls back to English topics.

Time zones:

- English topics are treated as Pacific time.
- German and French topics are treated as Central European time.
- Japanese topics are treated as Tokyo time.
- Time zone lookup supports Windows IDs first and IANA IDs as fallback.

## Localization

`Loc` provides a small in-code localization layer.

Configured translated dictionaries:

- `de`
- `fr`
- `ja`

English is represented by fallback strings passed by callers.

Language behavior:

- `LanguageCode = "Auto"` uses `CultureInfo.CurrentUICulture.TwoLetterISOLanguageName`.
- If the current UI culture is not translated, it falls back to `en`.
- Explicit language codes are lower-cased and used directly.

Helpers:

- `Text`: localized text with fallback.
- `Label`: localized ImGui label plus ID suffix.
- `Format`: localized format string.
- `Culture`: maps effective language to culture.
- `MonthNames`, `DayNames`, `CategoryLabels`: cached localized arrays.
- Date/time formatting helpers for long date-times, short times, and ranges.

## UI Components

### MainWindow

Owns:

- Top-level window layout.
- Toolbar.
- Centered Eorzea time/weather status.
- Sidebar.
- Settings modal.
- Create-event modal.
- Management mode and selected delete IDs.
- `CalendarMonthView`.
- `EventEditorModal`.

### CalendarMonthView

Owns:

- Month navigation.
- Official-event visibility toggle.
- Month grid drawing.
- Event chip drawing.
- Multi-day event projection into visible days.

### EventEditorModal

Owns:

- Editing state for one custom event.
- Save/delete/cancel actions.
- Uses `ScheduleEditorControls`.

### ScheduleEditorControls

Draws:

- Month combo.
- Day slider.
- Year input.
- Hour/minute sliders.
- AM/PM radio buttons.
- Reminder-minute slider.

### UiText

Provides `FitToAvailableWidth()` for ellipsis truncation based on current ImGui content width.

## Current Data Flow

Custom event creation/edit:

1. UI modifies or creates `EventData`.
2. Data is stored in `Configuration.Events`.
3. `Configuration.Save()` writes via Dalamud plugin interface.
4. `Plugin.DisplayEvents` exposes the merged list to UI.

Reminder flow:

1. `Framework.Update` calls reminder checks every 15 seconds.
2. `ReminderService` scans custom events.
3. Matching event is marked triggered.
4. Config is saved.
5. Notification is posted through Dalamud.

Official event flow:

1. Startup begins language cache warmup.
2. `Framework.Update` calls `ApplyPendingRefresh()`.
3. Official events are cached by language in configuration.
4. Active language cache becomes `LodestoneTopicsService.OfficialEvents`.
5. `Plugin.DisplayEvents` merges official events with custom events.

## Current Limitations and Review Notes

- No tests are present.
- Custom events do not expose `EndTime` in create/edit UI.
- Official-event parsing depends on Lodestone HTML structure and localized event-period wording.
- HTML parsing is regex-based rather than DOM-based, with one-second regex timeouts for hardening.
- `HttpClient` is created directly inside `LodestoneTopicsService`.
- Official events are cached in both legacy/current-language fields and per-language dictionaries.
- The repository is currently in a sample-renamed state in git status: sample plugin files are deleted, `EorzeaOutlook` files are untracked, and `README.md` is modified.

## Change Tracking Suggestions

When updating this project, record changes here under a dated note with:

- Feature or fix summary.
- Files changed.
- User-facing behavior changed.
- Configuration/schema impact.
- Dalamud API areas touched.
- Manual verification performed.

Suggested format:

```text
YYYY-MM-DD
- Changed:
- Files:
- Behavior:
- Config impact:
- Dalamud API:
- Verification:
```

## Change Log

2026-05-08

- Changed: Added `/pcal` as a secondary command for opening Eorzea Outlook.
- Changed: Expanded the Upcoming/manage sidebar list to fill remaining sidebar height before scrolling.
- Changed: Reduced the initial main window target to start around `720x560`, with responsive first-draw sizing capped at `1000x760`.
- Changed: Centered the language settings popup over the main Eorzea Outlook window instead of the game viewport.
- Changed: Removed the experimental audible reminder alarm; reminders are back to notification-only.
- Changed: Adjusted reminder check cadence from every 5 seconds to every 15 seconds.
- Changed: Set plugin manifest author and removed unused `RecurringWeekly` from `EventData`.
- Changed: Added a manage-mode select-all checkbox for deletion selections.
- Changed: Added centered Eorzea time and current territory weather display with game weather icons/colors.
- Changed: Added text-symbol fallback for weather icons when the game icon texture is unavailable.
- Changed: Added a separator before the weather icon and nudged icon alignment down to better match the text baseline.
- Changed: Right-aligned the top Eorzea time/weather status just left of the settings button.
- Changed: Localized weather/place names using the plugin language setting instead of the game-data default language.
- Fixed: Weather text now resolves from the explicitly localized `Weather` sheet instead of relying on row-reference default language resolution.
- Changed: Made the top Eorzea time/weather status responsive, falling back to compact text plus icon at narrow widths.
- Fixed: Weather lookup now accumulates `WeatherRate` probabilities and uses 32-bit unsigned overflow semantics for the FFXIV weather target calculation.
- Removed: stale unused `ConfigWindow`, orphaned old sample project solution entries, and unused `Data/goat.png` content copy.
- Changed: Reminder scanning snapshots custom events before enumeration to avoid collection-modified errors during UI edits.
- Changed: Lodestone refresh startup now captures a local cancellation token source for async refresh tasks.
- Changed: Added one-second regex timeouts to Lodestone topic parsing and HTML cleanup regexes.
- Changed: Trim stale official event cache entries that ended more than 14 days ago when saving refreshed Lodestone caches.
- Files: `EorzeaOutlook/Plugin.cs`, `EorzeaOutlook/Configuration.cs`, `EorzeaOutlook/EorzeaOutlook.json`, `EorzeaOutlook/Models/EventData.cs`, `EorzeaOutlook/Services/EorzeaTimeWeatherService.cs`, `EorzeaOutlook/Services/ReminderService.cs`, `EorzeaOutlook/Localization/Loc.cs`, `EorzeaOutlook/Windows/MainWindow.cs`, `project.md`.
- Dalamud API: `ICommandManager.AddHandler`, `ICommandManager.RemoveHandler`, `INotificationManager.AddNotification`, `IClientState.TerritoryType`, `IDataManager.GetExcelSheet`, `ITextureProvider.GetFromGameIcon`, `WindowSystem` draw path unchanged.

## Verification During This Review

- Read the repository structure and all current source files.
- Checked current git status to avoid overwriting existing uncommitted work.
- Reviewed the live Dalamud API documentation at https://dalamud.dev/api/.
- Confirmed the API index identifies API 15 as current while some linked generated pages still display API 14 labels.
- Ran `dotnet build EorzeaOutlook.sln`; build succeeded with 0 warnings and 0 errors.
