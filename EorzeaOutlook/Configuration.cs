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
