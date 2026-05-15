using System;

using Dalamud.Game;
using Dalamud.Plugin.Services;

using EorzeaOutlook.Localization;

using Lumina.Excel.Sheets;

namespace EorzeaOutlook.Services;

public sealed class EorzeaTimeWeatherService
{
    private const double EorzeaTimeMultiplier =
        20.571428571428573;

    private readonly IClientState clientState;

    private readonly IDataManager dataManager;

    private readonly Configuration configuration;

    private readonly IPluginLog log;

    private uint cachedTerritoryType;

    private ClientLanguage cachedLanguage;

    private string cachedPlaceName =
        "";

    private WeatherRate? cachedWeatherRate;

    private bool loggedWeatherFailure;

    private uint currentWeatherIconId;

    public EorzeaTimeWeatherService(
        IClientState clientState,
        IDataManager dataManager,
        Configuration configuration,
        IPluginLog log)
    {
        this.clientState = clientState;
        this.dataManager = dataManager;
        this.configuration = configuration;
        this.log = log;
    }

    public EorzeaStatus GetStatus()
    {
        var now =
            DateTimeOffset.UtcNow;

        var eorzeaTime =
            GetEorzeaTime(now);

        var weatherName =
            TryGetCurrentWeatherName(
                now,
                out var currentWeatherName)
                ? currentWeatherName
                : "";

        return new EorzeaStatus(
            eorzeaTime,
            cachedPlaceName,
            weatherName,
            GetWeatherIcon(weatherName),
            currentWeatherIconId,
            GetWeatherColor(weatherName));
    }

    private DateTime GetEorzeaTime(
        DateTimeOffset now)
    {
        var eorzeaTicks =
            (long)(now.ToUnixTimeMilliseconds()
                * TimeSpan.TicksPerMillisecond
                * EorzeaTimeMultiplier);

        return new DateTime(eorzeaTicks);
    }

    private bool TryGetCurrentWeatherName(
        DateTimeOffset now,
        out string weatherName)
    {
        weatherName = "";

        currentWeatherIconId = 0;

        if (!TryLoadTerritoryWeather())
            return false;

        if (cachedWeatherRate == null)
            return false;

        var weatherChance =
            CalculateWeatherChance(now);

        var rates =
            cachedWeatherRate.Value.Rate;

        var weathers =
            cachedWeatherRate.Value.Weather;

        var weatherSheet =
            dataManager.GetExcelSheet<Weather>(cachedLanguage);

        var rateAccumulator =
            0;

        for (var index = 0; index < rates.Count; index++)
        {
            rateAccumulator += rates[index];

            if (weatherChance >= rateAccumulator)
                continue;

            var weather =
                weatherSheet.GetRow(
                    weathers[index].RowId);

            weatherName =
                weather.Name.ExtractText();

            currentWeatherIconId =
                (uint)weather.Icon;

            return !string.IsNullOrWhiteSpace(weatherName);
        }

        return false;
    }

    private bool TryLoadTerritoryWeather()
    {
        var territoryType =
            clientState.TerritoryType;

        var language =
            GetConfiguredClientLanguage();

        if (territoryType == 0)
            return false;

        if (territoryType == cachedTerritoryType
            && language == cachedLanguage)
        {
            return cachedWeatherRate != null;
        }

        cachedTerritoryType =
            territoryType;

        cachedLanguage =
            language;

        cachedPlaceName = "";

        cachedWeatherRate = null;

        try
        {
            var territory =
                dataManager
                    .GetExcelSheet<TerritoryType>(language)
                    .GetRow(territoryType);

            cachedPlaceName =
                territory.PlaceName.Value.Name.ExtractText();

            if (territory.WeatherRate.RowId == 0)
                return false;

            cachedWeatherRate =
                territory.WeatherRate.Value;

            loggedWeatherFailure = false;

            return true;
        }
        catch (Exception ex)
        {
            if (!loggedWeatherFailure)
            {
                log.Warning(
                    ex,
                    "Failed to resolve Eorzea weather for territory {TerritoryType}.",
                    territoryType);

                loggedWeatherFailure = true;
            }

            return false;
        }
    }

    private ClientLanguage GetConfiguredClientLanguage()
    {
        return Loc.EffectiveLanguageCode(configuration.LanguageCode) switch
        {
            "ja" => ClientLanguage.Japanese,
            "de" => ClientLanguage.German,
            "fr" => ClientLanguage.French,
            _ => ClientLanguage.English
        };
    }

    private static int CalculateWeatherChance(
        DateTimeOffset now)
    {
        var unixSeconds =
            (uint)now.ToUnixTimeSeconds();

        var bell =
            unixSeconds / 175;

        var increment =
            (bell + 8 - (bell % 8)) % 24;

        var totalDays =
            unixSeconds / 4200;

        var calcBase =
            (totalDays * 100) + increment;

        unchecked
        {
            var step1 =
                (calcBase << 11) ^ calcBase;

            var step2 =
                (step1 >> 8) ^ step1;

            return (int)(step2 % 100);
        }
    }

    private static string GetWeatherIcon(
        string weatherName)
    {
        if (weatherName.Contains("Thunder", StringComparison.OrdinalIgnoreCase))
            return "!";

        if (weatherName.Contains("Rain", StringComparison.OrdinalIgnoreCase)
            || weatherName.Contains("Shower", StringComparison.OrdinalIgnoreCase))
        {
            return "~";
        }

        if (weatherName.Contains("Cloud", StringComparison.OrdinalIgnoreCase)
            || weatherName.Contains("Fog", StringComparison.OrdinalIgnoreCase))
        {
            return "*";
        }

        if (weatherName.Contains("Clear", StringComparison.OrdinalIgnoreCase)
            || weatherName.Contains("Fair", StringComparison.OrdinalIgnoreCase))
        {
            return "o";
        }

        if (weatherName.Contains("Snow", StringComparison.OrdinalIgnoreCase)
            || weatherName.Contains("Blizzard", StringComparison.OrdinalIgnoreCase))
        {
            return "+";
        }

        return "";
    }

    private static System.Numerics.Vector4 GetWeatherColor(
        string weatherName)
    {
        if (weatherName.Contains("Thunder", StringComparison.OrdinalIgnoreCase))
            return new(0.95f, 0.75f, 1f, 1f);

        if (weatherName.Contains("Rain", StringComparison.OrdinalIgnoreCase)
            || weatherName.Contains("Shower", StringComparison.OrdinalIgnoreCase))
        {
            return new(0.45f, 0.7f, 1f, 1f);
        }

        if (weatherName.Contains("Cloud", StringComparison.OrdinalIgnoreCase)
            || weatherName.Contains("Fog", StringComparison.OrdinalIgnoreCase))
        {
            return new(0.72f, 0.76f, 0.8f, 1f);
        }

        if (weatherName.Contains("Clear", StringComparison.OrdinalIgnoreCase)
            || weatherName.Contains("Fair", StringComparison.OrdinalIgnoreCase))
        {
            return new(1f, 0.86f, 0.42f, 1f);
        }

        if (weatherName.Contains("Snow", StringComparison.OrdinalIgnoreCase)
            || weatherName.Contains("Blizzard", StringComparison.OrdinalIgnoreCase))
        {
            return new(0.8f, 0.95f, 1f, 1f);
        }

        return new(0.65f, 0.85f, 1f, 1f);
    }
}

public sealed record EorzeaStatus(
    DateTime EorzeaTime,
    string PlaceName,
    string WeatherName,
    string WeatherIcon,
    uint WeatherIconId,
    System.Numerics.Vector4 WeatherColor);
