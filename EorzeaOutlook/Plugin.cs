using System;
using System.Collections.Generic;
using System.Linq;
using Dalamud.Game.Command;

using Dalamud.Interface.ImGuiNotification;
using Dalamud.Interface.Windowing;

using Dalamud.IoC;

using Dalamud.Plugin;
using Dalamud.Plugin.Services;

using EorzeaOutlook.Models;
using EorzeaOutlook.Services;
using EorzeaOutlook.Windows;

namespace EorzeaOutlook;

public sealed class Plugin : IDalamudPlugin
{
    [PluginService]
    internal static IDalamudPluginInterface PluginInterface { get; private set; } = null!;

    [PluginService]
    internal static ICommandManager CommandManager { get; private set; } = null!;

    [PluginService]
    internal static IPluginLog Log { get; private set; } = null!;

    [PluginService]
    internal static IFramework Framework { get; private set; } = null!;

    [PluginService]
    internal static INotificationManager NotificationManager { get; private set; } = null!;

    [PluginService]
    internal static IClientState ClientState { get; private set; } = null!;

    [PluginService]
    internal static IDataManager DataManager { get; private set; } = null!;

    [PluginService]
    internal static ITextureProvider TextureProvider { get; private set; } = null!;

    private const string PrimaryCommandName = "/pcalendar";

    private const string SecondaryCommandName = "/pcal";

    private const int ReminderCheckIntervalSeconds = 15;

    private static readonly string[] CommandNames =
    [
        PrimaryCommandName,
        SecondaryCommandName
    ];

    public Configuration Configuration { get; init; }

    public readonly WindowSystem WindowSystem =
        new("EorzeaOutlook");

    private MainWindow MainWindow { get; init; }

    private ReminderService ReminderService { get; init; }

    private LodestoneTopicsService LodestoneTopicsService { get; init; }

    private InGameResetEventService InGameResetEventService { get; init; }

    public EorzeaTimeWeatherService EorzeaTimeWeatherService { get; init; }

    private DateTime lastReminderCheck =
        DateTime.MinValue;

    public IEnumerable<EventData> DisplayEvents =>
        Configuration.Events
            .Concat(LodestoneTopicsService.OfficialEvents)
            .Concat(
                InGameResetEventService.GetEvents(
                    DateTime.Now,
                    Configuration,
                    Configuration.LanguageCode));

    public Plugin()
    {
        Configuration =
            PluginInterface.GetPluginConfig()
            as Configuration
            ?? new Configuration();

        Configuration.Initialize(
            PluginInterface);

        MainWindow =
            new MainWindow(this);

        ReminderService =
            new ReminderService(
                this,
                Log,
                NotificationManager);

        LodestoneTopicsService =
            new LodestoneTopicsService(
                Configuration,
                Log);

        InGameResetEventService =
            new InGameResetEventService();

        EorzeaTimeWeatherService =
            new EorzeaTimeWeatherService(
                ClientState,
                DataManager,
                Configuration,
                Log);

        LodestoneTopicsService.WarmLanguageCachesIfNeeded();

        WindowSystem.AddWindow(MainWindow);

        foreach (var commandName in CommandNames)
        {
            CommandManager.AddHandler(
                commandName,
                new CommandInfo(OnCommand)
                {
                    HelpMessage =
                        "Open Eorzea Outlook."
                });
        }

        PluginInterface.UiBuilder.Draw += DrawUI;

        PluginInterface.UiBuilder.OpenMainUi += ToggleMainUi;

        PluginInterface.UiBuilder.OpenConfigUi += ToggleMainUi;

        Framework.Update += OnFrameworkUpdate;

        Log.Information(
            "Eorzea Outlook loaded.");
    }

    public void Dispose()
    {
        Framework.Update -= OnFrameworkUpdate;

        PluginInterface.UiBuilder.Draw -= DrawUI;

        PluginInterface.UiBuilder.OpenMainUi -= ToggleMainUi;

        PluginInterface.UiBuilder.OpenConfigUi -= ToggleMainUi;

        WindowSystem.RemoveAllWindows();

        MainWindow.Dispose();

        LodestoneTopicsService.Dispose();

        foreach (var commandName in CommandNames)
        {
            CommandManager.RemoveHandler(commandName);
        }
    }

    private void OnFrameworkUpdate(
        IFramework framework)
    {
        LodestoneTopicsService.ApplyPendingRefresh();

        if ((DateTime.Now - lastReminderCheck)
                .TotalSeconds < ReminderCheckIntervalSeconds)
        {
            return;
        }

        lastReminderCheck =
            DateTime.Now;

        ReminderService.CheckReminders();

        LodestoneTopicsService.RefreshIfNeeded();
    }

    private void DrawUI()
    {
        WindowSystem.Draw();
    }

    private void OnCommand(
        string command,
        string args)
    {
        ToggleMainUi();
    }

    public void ToggleMainUi()
    {
        MainWindow.Toggle();
    }

    public void RefreshOfficialEvents(
        bool force)
    {
        LodestoneTopicsService.RefreshIfNeeded(force);
    }
}
