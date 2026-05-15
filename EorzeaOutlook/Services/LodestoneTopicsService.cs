using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Dalamud.Plugin.Services;

using EorzeaOutlook.Localization;
using EorzeaOutlook.Models;

namespace EorzeaOutlook.Services;

public sealed class LodestoneTopicsService : IDisposable
{
    private static readonly TimeSpan RegexTimeout =
        TimeSpan.FromSeconds(1);

    private static readonly TimeSpan OfficialEventRetention =
        TimeSpan.FromDays(14);

    private static readonly Regex TopicListItemRegex =
        new(
            "<li\\b(?<attributes>[^>]*)>(?<content>.*?)</li>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline,
            RegexTimeout);

    private static readonly Regex AnchorRegex =
        new(
            "<a\\b[^>]*href=\"(?<url>[^\"]+)\"[^>]*>(?<title>.*?)</a>",
            RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline,
            RegexTimeout);

    private static readonly Regex EnglishEventPeriodRegex =
        new(
            @"Event Period\s+From\s+(?:(?:Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday),\s+)?(?<startMonth>Jan\.|January|Feb\.|February|Mar\.|March|Apr\.|April|May|Jun\.|June|Jul\.|July|Aug\.|August|Sep\.|Sept\.|September|Oct\.|October|Nov\.|November|Dec\.|December)\s+(?<startDay>\d{1,2}),\s+(?<startYear>\d{4})\s+at\s+(?<startTime>\d{1,2}:\d{2}\s*[ap]\.m\.)\s+to\s+(?:(?:Monday|Tuesday|Wednesday|Thursday|Friday|Saturday|Sunday),\s+)?(?<endMonth>Jan\.|January|Feb\.|February|Mar\.|March|Apr\.|April|May|Jun\.|June|Jul\.|July|Aug\.|August|Sep\.|Sept\.|September|Oct\.|October|Nov\.|November|Dec\.|December)\s+(?<endDay>\d{1,2}),\s+(?<endYear>\d{4})\s+at\s+(?<endTime>\d{1,2}:\d{2}\s*[ap]\.m\.)\s+\((?<zone>PDT|PST)\)",
            RegexOptions.Compiled | RegexOptions.IgnoreCase,
            RegexTimeout);

    private static readonly Regex GermanEventPeriodRegex =
        new(
            @"Ereigniszeitraum\s+Von\s+(?:(?:Montag|Dienstag|Mittwoch|Donnerstag|Freitag|Samstag|Sonntag),\s+)?den\s+(?<startDay>\d{1,2})\.\s+(?<startMonth>[A-Za-zÄÖÜäöüß]+)\s+(?<startYear>\d{4})\s+um\s+(?<startHour>\d{1,2}):(?<startMinute>\d{2})\s+Uhr\s+bis\s+(?:(?:Montag|Dienstag|Mittwoch|Donnerstag|Freitag|Samstag|Sonntag),\s+)?den\s+(?<endDay>\d{1,2})\.\s+(?<endMonth>[A-Za-zÄÖÜäöüß]+)\s+(?<endYear>\d{4})\s+um\s+(?<endHour>\d{1,2}):(?<endMinute>\d{2})\s+Uhr",
            RegexOptions.Compiled | RegexOptions.IgnoreCase,
            RegexTimeout);

    private static readonly Regex FrenchEventPeriodRegex =
        new(
            @"Durée de l'événement\s+Du\s+(?:(?:lundi|mardi|mercredi|jeudi|vendredi|samedi|dimanche)\s+)?(?<startDay>\d{1,2})\s+(?<startMonth>[A-Za-zÀ-ÿ]+)\s+à\s+(?<startHour>\d{1,2})h(?<startMinute>\d{2})?\s+au\s+(?:(?:lundi|mardi|mercredi|jeudi|vendredi|samedi|dimanche)\s+)?(?<endDay>\d{1,2})\s+(?<endMonth>[A-Za-zÀ-ÿ]+)\s+(?<endYear>\d{4})\s+à\s+(?<endHour>\d{1,2})h(?<endMinute>\d{2})?",
            RegexOptions.Compiled | RegexOptions.IgnoreCase,
            RegexTimeout);

    private static readonly Regex JapaneseEventPeriodRegex =
        new(
            @"開催期間\s+(?:(?<startYear>\d{4})年)?(?<startMonth>\d{1,2})月(?<startDay>\d{1,2})日(?:（.）)?(?<startHour>\d{1,2}):(?<startMinute>\d{2})頃?\s*～\s*(?:(?<endYear>\d{4})年)?(?<endMonth>\d{1,2})月(?<endDay>\d{1,2})日(?:（.）)?(?<endHour>\d{1,2}):(?<endMinute>\d{2})頃?",
            RegexOptions.Compiled,
            RegexTimeout);

    private readonly HttpClient httpClient;

    private readonly Configuration configuration;

    private readonly IPluginLog log;

    private readonly TimeZoneInfo pacificTimeZone;

    private readonly object refreshLock =
        new();

    private static readonly string[] SupportedLanguageCodes =
    [
        "en",
        "ja",
        "de",
        "fr"
    ];

    private CancellationTokenSource? refreshCancellation;

    private List<EventData>? pendingOfficialEvents;

    private string pendingOfficialEventsLanguageCode =
        "";

    private readonly Dictionary<string, List<EventData>> pendingOfficialEventCaches =
        [];

    private DateTime nextRefreshAttempt =
        DateTime.MinValue;

    private bool disposed = false;

    private bool refreshInProgress = false;

    public LodestoneTopicsService(
        Configuration configuration,
        IPluginLog log)
    {
        this.configuration = configuration;

        this.log = log;

        httpClient =
            new HttpClient
            {
                Timeout =
                    TimeSpan.FromSeconds(12)
            };

        httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
            "EorzeaOutlook/1.0");

        pacificTimeZone =
            GetPacificTimeZone();

        OfficialEvents =
            GetCachedEvents(
                Loc.EffectiveLanguageCode(configuration.LanguageCode));
    }

    public IReadOnlyList<EventData> OfficialEvents { get; private set; } =
        [];

    public void RefreshIfNeeded(
        bool force = false)
    {
        var languageCode =
            Loc.EffectiveLanguageCode(configuration.LanguageCode);

        LoadCachedEvents(languageCode);

        CancellationTokenSource refreshTokenSource;

        lock (refreshLock)
        {
            if (disposed || refreshInProgress)
                return;

            var now =
                DateTime.Now;

            if (!force && OfficialEvents.Count > 0 && now < nextRefreshAttempt)
                return;

            if (!force
                && TryGetRefreshTime(languageCode, out var lastRefresh)
                && (now - lastRefresh)
                    .TotalDays < 1)
            {
                return;
            }

            refreshInProgress = true;

            refreshCancellation?.Dispose();

            refreshTokenSource =
                new CancellationTokenSource();

            refreshCancellation =
                refreshTokenSource;
        }

        _ = RefreshAsync(
            languageCode,
            refreshTokenSource.Token);
    }

    public void WarmLanguageCachesIfNeeded()
    {
        CancellationTokenSource refreshTokenSource;

        lock (refreshLock)
        {
            if (disposed || refreshInProgress)
                return;

            refreshInProgress = true;

            refreshCancellation?.Dispose();

            refreshTokenSource =
                new CancellationTokenSource();

            refreshCancellation =
                refreshTokenSource;
        }

        _ = WarmLanguageCachesAsync(
            refreshTokenSource.Token);
    }

    public void Dispose()
    {
        lock (refreshLock)
        {
            disposed = true;

            refreshCancellation?.Cancel();

            refreshCancellation?.Dispose();

            refreshCancellation = null;
        }

        httpClient.Dispose();
    }

    private async Task RefreshAsync(
        string languageCode,
        CancellationToken cancellationToken)
    {
        try
        {
            var events =
                await FetchTopicEventsAsync(
                    languageCode,
                    cancellationToken);

            cancellationToken.ThrowIfCancellationRequested();

            lock (refreshLock)
            {
                if (events.Count > 0)
                {
                    pendingOfficialEvents =
                        events
                            .OrderBy(x => x.StartTime)
                            .ToList();

                    pendingOfficialEventsLanguageCode =
                        languageCode;
                }
                else
                {
                    log.Warning(
                        "Lodestone refresh completed but parsed no topic events for language {LanguageCode}.",
                        languageCode);
                }

                nextRefreshAttempt =
                    events.Count > 0
                        ? DateTime.Now.AddMinutes(15)
                        : DateTime.Now.AddMinutes(5);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            lock (refreshLock)
            {
                nextRefreshAttempt =
                    DateTime.Now.AddMinutes(5);
            }

            log.Warning(
                ex,
                "Failed to refresh Lodestone topic events.");
        }
        finally
        {
            lock (refreshLock)
            {
                refreshInProgress = false;
            }
        }
    }

    private async Task WarmLanguageCachesAsync(
        CancellationToken cancellationToken)
    {
        try
        {
            foreach (var languageCode in SupportedLanguageCodes)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (HasFreshCachedEvents(languageCode))
                    continue;

                var events =
                    await FetchTopicEventsAsync(
                        languageCode,
                        cancellationToken);

                cancellationToken.ThrowIfCancellationRequested();

                lock (refreshLock)
                {
                    if (events.Count > 0)
                    {
                        pendingOfficialEventCaches[languageCode] =
                            events
                                .OrderBy(x => x.StartTime)
                                .ToList();
                    }
                    else
                    {
                        log.Warning(
                            "Lodestone cache warmup parsed no topic events for language {LanguageCode}.",
                            languageCode);
                    }
                }
            }

            lock (refreshLock)
            {
                nextRefreshAttempt =
                    DateTime.Now.AddMinutes(15);
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (Exception ex)
        {
            lock (refreshLock)
            {
                nextRefreshAttempt =
                    DateTime.Now.AddMinutes(5);
            }

            log.Warning(
                ex,
                "Failed to warm Lodestone topic event caches.");
        }
        finally
        {
            lock (refreshLock)
            {
                refreshInProgress = false;
            }
        }
    }

    public void ApplyPendingRefresh()
    {
        List<EventData>? events;

        string languageCode;

        Dictionary<string, List<EventData>> cacheUpdates;

        lock (refreshLock)
        {
            if (pendingOfficialEvents == null
                && pendingOfficialEventCaches.Count == 0)
            {
                return;
            }

            events =
                pendingOfficialEvents;

            languageCode =
                pendingOfficialEventsLanguageCode;

            pendingOfficialEvents = null;

            pendingOfficialEventsLanguageCode = "";

            cacheUpdates =
                pendingOfficialEventCaches.ToDictionary(
                    x => x.Key,
                    x => x.Value);

            pendingOfficialEventCaches.Clear();
        }

        foreach (var cacheUpdate in cacheUpdates)
        {
            SaveLanguageCache(
                cacheUpdate.Key,
                cacheUpdate.Value);
        }

        if (events != null)
        {
            SaveLanguageCache(
                languageCode,
                events);
        }

        var currentLanguageCode =
            Loc.EffectiveLanguageCode(configuration.LanguageCode);

        if (TryGetCachedEvents(
                currentLanguageCode,
                out var currentEvents))
        {
            OfficialEvents =
                currentEvents;

            configuration.OfficialEvents =
                currentEvents.ToList();

            configuration.OfficialEventsLanguageCode =
                currentLanguageCode;

            if (TryGetRefreshTime(
                    currentLanguageCode,
                    out var refreshTime))
            {
                configuration.OfficialEventsLastRefresh =
                    refreshTime;
            }
        }

        configuration.Save();
    }

    private void SaveLanguageCache(
        string languageCode,
        List<EventData> events)
    {
        var now =
            DateTime.Now;

        var retainedEvents =
            TrimStaleOfficialEvents(
                events,
                now);

        configuration.OfficialEventsByLanguage[languageCode] =
            retainedEvents;

        configuration.OfficialEventsRefreshTimesByLanguage[languageCode] =
            now;
    }

    private static List<EventData> TrimStaleOfficialEvents(
        IEnumerable<EventData> events,
        DateTime now)
    {
        var retentionCutoff =
            now - OfficialEventRetention;

        return events
            .Where(x =>
                (x.EndTime ?? x.StartTime) >= retentionCutoff)
            .OrderBy(x => x.StartTime)
            .ToList();
    }

    private void LoadCachedEvents(
        string languageCode)
    {
        if (!TryGetCachedEvents(
                languageCode,
                out var cachedEvents))
        {
            return;
        }

        if (OfficialEventsLanguageMatches(languageCode)
            && OfficialEvents.Count == cachedEvents.Count)
        {
            return;
        }

        OfficialEvents =
            cachedEvents;

        configuration.OfficialEvents =
            cachedEvents.ToList();

        configuration.OfficialEventsLanguageCode =
            languageCode;

        if (TryGetRefreshTime(languageCode, out var refreshTime))
        {
            configuration.OfficialEventsLastRefresh =
                refreshTime;
        }
    }

    private List<EventData> GetCachedEvents(
        string languageCode)
    {
        return TryGetCachedEvents(
                languageCode,
                out var cachedEvents)
            ? cachedEvents
            : [];
    }

    private bool TryGetCachedEvents(
        string languageCode,
        out List<EventData> cachedEvents)
    {
        if (configuration.OfficialEventsByLanguage.TryGetValue(
                languageCode,
                out var configuredEvents)
            && configuredEvents.Count > 0)
        {
            cachedEvents =
                configuredEvents
                .OrderBy(x => x.StartTime)
                .ToList();

            return true;
        }

        if (configuration.OfficialEventsLanguageCode == languageCode
            && configuration.OfficialEvents.Count > 0)
        {
            cachedEvents =
                configuration.OfficialEvents
                .OrderBy(x => x.StartTime)
                .ToList();

            return true;
        }

        cachedEvents = [];

        return false;
    }

    private bool TryGetRefreshTime(
        string languageCode,
        out DateTime refreshTime)
    {
        if (configuration.OfficialEventsRefreshTimesByLanguage.TryGetValue(
                languageCode,
                out refreshTime)
            && configuration.OfficialEventsByLanguage.TryGetValue(
                languageCode,
                out var cachedEvents)
            && cachedEvents.Count > 0)
        {
            return true;
        }

        if (configuration.OfficialEventsLanguageCode == languageCode
            && configuration.OfficialEvents.Count > 0)
        {
            refreshTime =
                configuration.OfficialEventsLastRefresh;

            return refreshTime > DateTime.MinValue;
        }

        refreshTime =
            DateTime.MinValue;

        return false;
    }

    private bool HasFreshCachedEvents(
        string languageCode)
    {
        return TryGetRefreshTime(languageCode, out var lastRefresh)
            && DateTime.Now - lastRefresh < TimeSpan.FromDays(1);
    }

    private bool OfficialEventsLanguageMatches(
        string languageCode)
    {
        return configuration.OfficialEventsLanguageCode == languageCode;
    }

    private async Task<List<EventData>> FetchTopicEventsAsync(
        string languageCode,
        CancellationToken cancellationToken)
    {
        var topicsUri =
            GetTopicsUri(languageCode);

        var listHtml =
            await httpClient.GetStringAsync(
                topicsUri,
                cancellationToken);

        var events =
            ParseTopicListEvents(
                listHtml,
                topicsUri,
                languageCode);

        cancellationToken.ThrowIfCancellationRequested();

        if (events.Count > 0 || languageCode == "en")
            return events;

        var englishTopicsUri =
            GetTopicsUri("en");

        var englishListHtml =
            await httpClient.GetStringAsync(
                englishTopicsUri,
                cancellationToken);

        return ParseTopicListEvents(
            englishListHtml,
            englishTopicsUri,
            "en");
    }

    private List<EventData> ParseTopicListEvents(
        string listHtml,
        Uri topicsUri,
        string languageCode)
    {
        var events =
            new List<EventData>();

        foreach (Match itemMatch in TopicListItemRegex.Matches(listHtml))
        {
            var itemHtml =
                itemMatch.Groups["content"].Value;

            var itemText =
                CleanText(itemHtml);

            if (!TryParseEventPeriod(
                    languageCode,
                    itemText,
                    out var start,
                    out var end))
            {
                continue;
            }

            var anchorMatch =
                AnchorRegex.Match(itemHtml);

            var sourceUrl =
                anchorMatch.Success
                    ? new Uri(
                            topicsUri,
                            anchorMatch.Groups["url"].Value)
                        .ToString()
                    : topicsUri.ToString();

            var title =
                anchorMatch.Success
                    ? CleanTitle(anchorMatch.Groups["title"].Value)
                    : ExtractTitleFromTopicText(
                        itemText,
                        languageCode);

            if (string.IsNullOrWhiteSpace(title))
                continue;

            events.Add(
                new EventData
                {
                    Id = GuidFromUrl(sourceUrl + languageCode),
                    Title = title,
                    Description =
                        Loc.Text(
                            configuration.LanguageCode,
                            "Official.Description",
                            "Official Lodestone event."),
                    Category = "Official",
                    StartTime = start,
                    EndTime = end,
                    ReminderMinutesBefore = 0,
                    ReminderTriggered = true,
                    IsOfficial = true,
                    SourceUrl = sourceUrl
                });
        }

        return events
            .GroupBy(x => x.SourceUrl)
            .Select(x => x.First())
            .OrderBy(x => x.StartTime)
            .ToList();
    }

    private bool TryParseEventPeriod(
        string languageCode,
        string text,
        out DateTime localStart,
        out DateTime localEnd)
    {
        localStart = default;
        localEnd = default;

        return languageCode switch
        {
            "de" => TryParseGermanEventPeriod(text, out localStart, out localEnd),
            "fr" => TryParseFrenchEventPeriod(text, out localStart, out localEnd),
            "ja" => TryParseJapaneseEventPeriod(text, out localStart, out localEnd),
            _ => TryParseEnglishEventPeriod(text, out localStart, out localEnd)
        };
    }

    private static string ExtractTitleFromTopicText(
        string text,
        string languageCode)
    {
        var marker =
            languageCode switch
            {
                "de" => "Ereigniszeitraum",
                "fr" => "Durée de l'événement",
                "ja" => "開催期間",
                _ => "Event Period"
            };

        var markerIndex =
            text.IndexOf(
                marker,
                StringComparison.OrdinalIgnoreCase);

        if (markerIndex <= 0)
            return "";

        var beforePeriod =
            text[..markerIndex].Trim();

        var separatorIndex =
            beforePeriod.IndexOf(" - ", StringComparison.Ordinal);

        if (separatorIndex > 0)
        {
            return beforePeriod[..separatorIndex].Trim();
        }

        var sentences =
            beforePeriod
                .Split(
                    ['.', '!', '?'],
                    StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return sentences.Length > 0
            ? sentences[0]
            : beforePeriod;
    }

    private bool TryParseEnglishEventPeriod(
        string text,
        out DateTime localStart,
        out DateTime localEnd)
    {
        localStart = default;
        localEnd = default;

        var match =
            EnglishEventPeriodRegex.Match(text);

        if (!match.Success)
            return false;

        var startText =
            $"{NormalizeMonth(match.Groups["startMonth"].Value)} {match.Groups["startDay"].Value}, {match.Groups["startYear"].Value} {NormalizeMeridiem(match.Groups["startTime"].Value)}";

        var endText =
            $"{NormalizeMonth(match.Groups["endMonth"].Value)} {match.Groups["endDay"].Value}, {match.Groups["endYear"].Value} {NormalizeMeridiem(match.Groups["endTime"].Value)}";

        if (!DateTime.TryParse(
                startText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var pacificStart))
        {
            return false;
        }

        if (!DateTime.TryParse(
                endText,
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var pacificEnd))
        {
            return false;
        }

        localStart =
            TimeZoneInfo.ConvertTime(
                DateTime.SpecifyKind(pacificStart, DateTimeKind.Unspecified),
                pacificTimeZone,
                TimeZoneInfo.Local);

        localEnd =
            TimeZoneInfo.ConvertTime(
                DateTime.SpecifyKind(pacificEnd, DateTimeKind.Unspecified),
                pacificTimeZone,
                TimeZoneInfo.Local);

        return true;
    }

    private bool TryParseGermanEventPeriod(
        string text,
        out DateTime localStart,
        out DateTime localEnd)
    {
        localStart = default;
        localEnd = default;

        var match =
            GermanEventPeriodRegex.Match(text);

        if (!match.Success)
            return false;

        if (!TryGetMonth(match.Groups["startMonth"].Value, "de-DE", out var startMonth)
            || !TryGetMonth(match.Groups["endMonth"].Value, "de-DE", out var endMonth))
        {
            return false;
        }

        var sourceStart =
            CreateDateTime(
                match,
                "start",
                int.Parse(match.Groups["startYear"].Value, CultureInfo.InvariantCulture),
                startMonth);

        var sourceEnd =
            CreateDateTime(
                match,
                "end",
                int.Parse(match.Groups["endYear"].Value, CultureInfo.InvariantCulture),
                endMonth);

        ConvertToLocal(
            sourceStart,
            GetCentralEuropeTimeZone(),
            out localStart);

        ConvertToLocal(
            sourceEnd,
            GetCentralEuropeTimeZone(),
            out localEnd);

        return true;
    }

    private bool TryParseFrenchEventPeriod(
        string text,
        out DateTime localStart,
        out DateTime localEnd)
    {
        localStart = default;
        localEnd = default;

        var match =
            FrenchEventPeriodRegex.Match(text);

        if (!match.Success)
            return false;

        if (!TryGetMonth(match.Groups["startMonth"].Value, "fr-FR", out var startMonth)
            || !TryGetMonth(match.Groups["endMonth"].Value, "fr-FR", out var endMonth))
        {
            return false;
        }

        var endYear =
            int.Parse(match.Groups["endYear"].Value, CultureInfo.InvariantCulture);

        var sourceStart =
            CreateDateTime(
                match,
                "start",
                endYear,
                startMonth);

        var sourceEnd =
            CreateDateTime(
                match,
                "end",
                endYear,
                endMonth);

        if (sourceStart > sourceEnd)
        {
            sourceStart =
                sourceStart.AddYears(-1);
        }

        ConvertToLocal(
            sourceStart,
            GetCentralEuropeTimeZone(),
            out localStart);

        ConvertToLocal(
            sourceEnd,
            GetCentralEuropeTimeZone(),
            out localEnd);

        return true;
    }

    private bool TryParseJapaneseEventPeriod(
        string text,
        out DateTime localStart,
        out DateTime localEnd)
    {
        localStart = default;
        localEnd = default;

        var match =
            JapaneseEventPeriodRegex.Match(text);

        if (!match.Success)
            return false;

        var startYear =
            GetOptionalYear(match, "startYear", DateTime.Now.Year);

        var endYear =
            GetOptionalYear(match, "endYear", startYear);

        var sourceStart =
            CreateDateTime(
                match,
                "start",
                startYear,
                int.Parse(match.Groups["startMonth"].Value, CultureInfo.InvariantCulture));

        var sourceEnd =
            CreateDateTime(
                match,
                "end",
                endYear,
                int.Parse(match.Groups["endMonth"].Value, CultureInfo.InvariantCulture));

        if (sourceEnd < sourceStart)
        {
            sourceEnd =
                sourceEnd.AddYears(1);
        }

        ConvertToLocal(
            sourceStart,
            GetTokyoTimeZone(),
            out localStart);

        ConvertToLocal(
            sourceEnd,
            GetTokyoTimeZone(),
            out localEnd);

        return true;
    }

    private static Uri GetTopicsUri(
        string languageCode)
    {
        var host =
            languageCode switch
            {
                "de" => "de.finalfantasyxiv.com",
                "fr" => "fr.finalfantasyxiv.com",
                "ja" => "jp.finalfantasyxiv.com",
                _ => "na.finalfantasyxiv.com"
            };

        return new Uri(
            $"https://{host}/lodestone/topics/");
    }

    private static void ConvertToLocal(
        DateTime sourceTime,
        TimeZoneInfo sourceTimeZone,
        out DateTime localTime)
    {
        localTime =
            TimeZoneInfo.ConvertTime(
                DateTime.SpecifyKind(sourceTime, DateTimeKind.Unspecified),
                sourceTimeZone,
                TimeZoneInfo.Local);
    }

    private static DateTime CreateDateTime(
        Match match,
        string prefix,
        int year,
        int month)
    {
        var minuteGroup =
            match.Groups[$"{prefix}Minute"];

        var minute =
            minuteGroup.Success && !string.IsNullOrWhiteSpace(minuteGroup.Value)
                ? int.Parse(minuteGroup.Value, CultureInfo.InvariantCulture)
                : 0;

        return new DateTime(
            year,
            month,
            int.Parse(match.Groups[$"{prefix}Day"].Value, CultureInfo.InvariantCulture),
            int.Parse(match.Groups[$"{prefix}Hour"].Value, CultureInfo.InvariantCulture),
            minute,
            0);
    }

    private static int GetOptionalYear(
        Match match,
        string groupName,
        int fallback)
    {
        var group =
            match.Groups[groupName];

        return group.Success && !string.IsNullOrWhiteSpace(group.Value)
            ? int.Parse(group.Value, CultureInfo.InvariantCulture)
            : fallback;
    }

    private static bool TryGetMonth(
        string monthName,
        string cultureName,
        out int month)
    {
        var culture =
            CultureInfo.GetCultureInfo(cultureName);

        var normalized =
            monthName.Trim().TrimEnd('.').ToLower(culture);

        for (var index = 0; index < 12; index++)
        {
            var fullName =
                culture.DateTimeFormat.MonthNames[index]
                    .TrimEnd('.')
                    .ToLower(culture);

            var abbreviatedName =
                culture.DateTimeFormat.AbbreviatedMonthNames[index]
                    .TrimEnd('.')
                    .ToLower(culture);

            if (normalized == fullName
                || normalized == abbreviatedName)
            {
                month = index + 1;

                return true;
            }
        }

        month = 0;

        return false;
    }

    private static TimeZoneInfo GetPacificTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Pacific Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "America/Los_Angeles");
        }
    }

    private static TimeZoneInfo GetCentralEuropeTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "W. Europe Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Europe/Berlin");
        }
    }

    private static TimeZoneInfo GetTokyoTimeZone()
    {
        try
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Tokyo Standard Time");
        }
        catch (TimeZoneNotFoundException)
        {
            return TimeZoneInfo.FindSystemTimeZoneById(
                "Asia/Tokyo");
        }
    }

    private static string CleanText(
        string value)
    {
        var noScripts =
            Regex.Replace(
                value,
                "<script.*?</script>",
                " ",
                RegexOptions.IgnoreCase | RegexOptions.Singleline,
                RegexTimeout);

        var noTags =
            Regex.Replace(
                noScripts,
                "<.*?>",
                " ",
                RegexOptions.None,
                RegexTimeout);

        var decoded =
            WebUtility.HtmlDecode(noTags);

        return Regex.Replace(
            decoded,
            "\\s+",
            " ",
            RegexOptions.None,
            RegexTimeout)
            .Trim();
    }

    private static string CleanTitle(
        string value)
    {
        var title =
            CleanText(value);

        var scriptIndex =
            title.IndexOf(
                "document.getElementById",
                StringComparison.OrdinalIgnoreCase);

        if (scriptIndex >= 0)
        {
            title =
                title[..scriptIndex].TrimEnd(' ', '-');
        }

        return title;
    }

    private static string NormalizeMeridiem(
        string value)
    {
        return value
            .Replace("a.m.", "AM", StringComparison.OrdinalIgnoreCase)
            .Replace("p.m.", "PM", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeMonth(
        string value)
    {
        return value.TrimEnd('.');
    }

    private static Guid GuidFromUrl(
        string value)
    {
        var bytes =
            System.Security.Cryptography.MD5.HashData(
                System.Text.Encoding.UTF8.GetBytes(value));

        return new Guid(bytes);
    }
}
