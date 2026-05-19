using System;
using System.Collections.Generic;

using Dalamud.Configuration;
using Dalamud.Plugin;

using EorzeaOutlook.Models;

namespace EorzeaOutlook;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;

    public List<EventData> Events { get; set; } =
        [];

    public List<EventData> OfficialEvents { get; set; } =
        [];

    public Dictionary<string, List<EventData>> OfficialEventsByLanguage { get; set; } =
        [];

    public Dictionary<string, DateTime> OfficialEventsRefreshTimesByLanguage { get; set; } =
        [];

    public DateTime OfficialEventsLastRefresh { get; set; } =
        DateTime.MinValue;

    public string OfficialEventsLanguageCode { get; set; } =
        "";

    public string LanguageCode { get; set; } =
        "Auto";

    public bool ShowInGameResetEvents { get; set; } =
        true;

    public bool ShowOfficialEvents { get; set; } =
        true;

    public bool ShowDailyResetEvents { get; set; } =
        true;

    public bool ShowWeeklyResetEvents { get; set; } =
        true;

    public bool ShowFashionReportEvents { get; set; } =
        true;

    public bool ShowJumboCactpotEvents { get; set; } =
        true;

    [NonSerialized]
    private IDalamudPluginInterface? pluginInterface;

    public void Initialize(
        IDalamudPluginInterface pluginInterface)
    {
        this.pluginInterface = pluginInterface;
    }

    public void Save()
    {
        pluginInterface!.SavePluginConfig(this);
    }
}
