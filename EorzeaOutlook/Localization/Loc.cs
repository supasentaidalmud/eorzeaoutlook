using System;
using System.Collections.Generic;
using System.Globalization;

namespace EorzeaOutlook.Localization;

public static class Loc
{
    private static readonly Dictionary<string, Dictionary<string, string>> Translations =
        new()
        {
            ["de"] = new()
            {
                ["Toolbar.NewEvent"] = "+ Neuer Termin",
                ["Toolbar.Today"] = "Heute",
                ["Toolbar.ManageEvents"] = "Termine verwalten",
                ["Toolbar.Cancel"] = "Abbrechen",
                ["Toolbar.Ok"] = "OK",
                ["Toolbar.SaveChanges"] = "Änderungen speichern",
                ["Settings.Title"] = "Einstellungen",
                ["Settings.Tooltip"] = "Einstellungen",
                ["Settings.Language"] = "Sprache",
                ["Settings.Note"] = "Ändert die Plugin-Oberfläche.",
                ["Sidebar.Events"] = "Termine",
                ["Sidebar.Upcoming"] = "Demnächst",
                ["Sidebar.SelectAll"] = "Alle auswählen",
                ["Sidebar.SelectedForDeletion"] = "{0} zum Löschen ausgewählt",
                ["Sidebar.NoEventsToManage"] = "Keine Termine zum Verwalten.",
                ["Sidebar.NoUpcomingEvents"] = "Keine bevorstehenden Termine.",
                ["Calendar.InGameEvents"] = "Spielereignisse",
                ["Official.Description"] = "Offizielles Lodestone-Ereignis.",
                ["Dialog.CreateEvent"] = "Termin erstellen",
                ["Dialog.NewEvent"] = "Neuer Termin",
                ["Dialog.EditEvent"] = "Termin bearbeiten",
                ["Dialog.AddTitle"] = "Titel hinzufügen",
                ["Dialog.Category"] = "Kategorie",
                ["Dialog.Schedule"] = "Zeitplan",
                ["Dialog.Reminder"] = "Erinnerung",
                ["Dialog.Description"] = "Beschreibung",
                ["Dialog.SaveEvent"] = "Termin speichern",
                ["Dialog.DeleteEvent"] = "Termin löschen",
                ["Schedule.Month"] = "Monat",
                ["Schedule.Day"] = "Tag",
                ["Schedule.Year"] = "Jahr",
                ["Schedule.Hour"] = "Stunde",
                ["Schedule.Minute"] = "Minute",
                ["Schedule.MinutesBefore"] = "Minuten vorher",
                ["Category.Raid"] = "Raid",
                ["Category.Maps"] = "Karten",
                ["Category.FC"] = "FG",
                ["Category.OceanFishing"] = "Ozeanangeln",
                ["Category.Custom"] = "Benutzerdefiniert",
                ["Month.1"] = "Januar",
                ["Month.2"] = "Februar",
                ["Month.3"] = "März",
                ["Month.4"] = "April",
                ["Month.5"] = "Mai",
                ["Month.6"] = "Juni",
                ["Month.7"] = "Juli",
                ["Month.8"] = "August",
                ["Month.9"] = "September",
                ["Month.10"] = "Oktober",
                ["Month.11"] = "November",
                ["Month.12"] = "Dezember",
                ["Day.Sun"] = "So",
                ["Day.Mon"] = "Mo",
                ["Day.Tue"] = "Di",
                ["Day.Wed"] = "Mi",
                ["Day.Thu"] = "Do",
                ["Day.Fri"] = "Fr",
                ["Day.Sat"] = "Sa",
            },
            ["fr"] = new()
            {
                ["Toolbar.NewEvent"] = "+ Nouvel événement",
                ["Toolbar.Today"] = "Aujourd'hui",
                ["Toolbar.ManageEvents"] = "Gérer",
                ["Toolbar.Cancel"] = "Annuler",
                ["Toolbar.Ok"] = "OK",
                ["Toolbar.SaveChanges"] = "Enregistrer",
                ["Settings.Title"] = "Paramètres",
                ["Settings.Tooltip"] = "Paramètres",
                ["Settings.Language"] = "Langue",
                ["Settings.Note"] = "Modifie le texte du plugin.",
                ["Sidebar.Events"] = "Événements",
                ["Sidebar.Upcoming"] = "À venir",
                ["Sidebar.SelectAll"] = "Tout sélectionner",
                ["Sidebar.SelectedForDeletion"] = "{0} sélectionné(s)",
                ["Sidebar.NoEventsToManage"] = "Aucun événement à gérer.",
                ["Sidebar.NoUpcomingEvents"] = "Aucun événement à venir.",
                ["Calendar.InGameEvents"] = "Événements du jeu",
                ["Official.Description"] = "Événement officiel du Lodestone.",
                ["Dialog.CreateEvent"] = "Créer un événement",
                ["Dialog.NewEvent"] = "Nouvel événement",
                ["Dialog.EditEvent"] = "Modifier l'événement",
                ["Dialog.AddTitle"] = "Ajouter un titre",
                ["Dialog.Category"] = "Catégorie",
                ["Dialog.Schedule"] = "Horaire",
                ["Dialog.Reminder"] = "Rappel",
                ["Dialog.Description"] = "Description",
                ["Dialog.SaveEvent"] = "Enregistrer",
                ["Dialog.DeleteEvent"] = "Supprimer",
                ["Schedule.Month"] = "Mois",
                ["Schedule.Day"] = "Jour",
                ["Schedule.Year"] = "Année",
                ["Schedule.Hour"] = "Heure",
                ["Schedule.Minute"] = "Minute",
                ["Schedule.MinutesBefore"] = "Minutes avant",
                ["Category.Raid"] = "Raid",
                ["Category.Maps"] = "Cartes",
                ["Category.FC"] = "CL",
                ["Category.OceanFishing"] = "Pêche en mer",
                ["Category.Custom"] = "Personnalisé",
                ["Month.1"] = "janvier",
                ["Month.2"] = "février",
                ["Month.3"] = "mars",
                ["Month.4"] = "avril",
                ["Month.5"] = "mai",
                ["Month.6"] = "juin",
                ["Month.7"] = "juillet",
                ["Month.8"] = "août",
                ["Month.9"] = "septembre",
                ["Month.10"] = "octobre",
                ["Month.11"] = "novembre",
                ["Month.12"] = "décembre",
                ["Day.Sun"] = "dim.",
                ["Day.Mon"] = "lun.",
                ["Day.Tue"] = "mar.",
                ["Day.Wed"] = "mer.",
                ["Day.Thu"] = "jeu.",
                ["Day.Fri"] = "ven.",
                ["Day.Sat"] = "sam.",
            },
            ["ja"] = new()
            {
                ["Toolbar.NewEvent"] = "+ 新規予定",
                ["Toolbar.Today"] = "今日",
                ["Toolbar.ManageEvents"] = "予定管理",
                ["Toolbar.Cancel"] = "キャンセル",
                ["Toolbar.Ok"] = "OK",
                ["Toolbar.SaveChanges"] = "保存",
                ["Settings.Title"] = "設定",
                ["Settings.Tooltip"] = "設定",
                ["Settings.Language"] = "言語",
                ["Settings.Note"] = "プラグインの表示言語を変更します。",
                ["Sidebar.Events"] = "予定",
                ["Sidebar.Upcoming"] = "今後",
                ["Sidebar.SelectAll"] = "すべて選択",
                ["Sidebar.SelectedForDeletion"] = "{0} 件を削除対象に選択",
                ["Sidebar.NoEventsToManage"] = "管理する予定はありません。",
                ["Sidebar.NoUpcomingEvents"] = "今後の予定はありません。",
                ["Calendar.InGameEvents"] = "ゲーム内イベント",
                ["Official.Description"] = "Lodestone公式イベントです。",
                ["Dialog.CreateEvent"] = "予定を作成",
                ["Dialog.NewEvent"] = "新規予定",
                ["Dialog.EditEvent"] = "予定を編集",
                ["Dialog.AddTitle"] = "タイトルを入力",
                ["Dialog.Category"] = "カテゴリ",
                ["Dialog.Schedule"] = "日時",
                ["Dialog.Reminder"] = "リマインダー",
                ["Dialog.Description"] = "説明",
                ["Dialog.SaveEvent"] = "予定を保存",
                ["Dialog.DeleteEvent"] = "予定を削除",
                ["Schedule.Month"] = "月",
                ["Schedule.Day"] = "日",
                ["Schedule.Year"] = "年",
                ["Schedule.Hour"] = "時",
                ["Schedule.Minute"] = "分",
                ["Schedule.MinutesBefore"] = "分前",
                ["Category.Raid"] = "レイド",
                ["Category.Maps"] = "地図",
                ["Category.FC"] = "FC",
                ["Category.OceanFishing"] = "オーシャンフィッシング",
                ["Category.Custom"] = "カスタム",
                ["Month.1"] = "1月",
                ["Month.2"] = "2月",
                ["Month.3"] = "3月",
                ["Month.4"] = "4月",
                ["Month.5"] = "5月",
                ["Month.6"] = "6月",
                ["Month.7"] = "7月",
                ["Month.8"] = "8月",
                ["Month.9"] = "9月",
                ["Month.10"] = "10月",
                ["Month.11"] = "11月",
                ["Month.12"] = "12月",
                ["Day.Sun"] = "日",
                ["Day.Mon"] = "月",
                ["Day.Tue"] = "火",
                ["Day.Wed"] = "水",
                ["Day.Thu"] = "木",
                ["Day.Fri"] = "金",
                ["Day.Sat"] = "土",
            },
        };

    private static readonly Dictionary<string, string[]> MonthNameCache =
        [];

    private static readonly Dictionary<string, string[]> DayNameCache =
        [];

    private static readonly Dictionary<string, string[]> CategoryLabelCache =
        [];

    public static string Text(
        string languageCode,
        string key,
        string fallback)
    {
        var resolvedCode =
            EffectiveLanguageCode(languageCode);

        if (Translations.TryGetValue(resolvedCode, out var language)
            && language.TryGetValue(key, out var value))
        {
            return value;
        }

        return fallback;
    }

    public static string Label(
        string languageCode,
        string key,
        string fallback,
        string id)
    {
        return $"{Text(languageCode, key, fallback)}##{id}";
    }

    public static string Format(
        string languageCode,
        string key,
        string fallback,
        params object[] args)
    {
        return string.Format(
            Culture(languageCode),
            Text(languageCode, key, fallback),
            args);
    }

    public static CultureInfo Culture(
        string languageCode)
    {
        var resolvedCode =
            EffectiveLanguageCode(languageCode);

        return resolvedCode switch
        {
            "de" => CultureInfo.GetCultureInfo("de-DE"),
            "fr" => CultureInfo.GetCultureInfo("fr-FR"),
            "ja" => CultureInfo.GetCultureInfo("ja-JP"),
            _ => CultureInfo.GetCultureInfo("en-US")
        };
    }

    public static string[] MonthNames(
        string languageCode)
    {
        var resolvedCode =
            EffectiveLanguageCode(languageCode);

        if (MonthNameCache.TryGetValue(resolvedCode, out var cachedMonths))
        {
            return cachedMonths;
        }

        var months =
            new string[12];

        for (var month = 1; month <= 12; month++)
        {
            months[month - 1] =
                Text(
                    languageCode,
                    $"Month.{month}",
                    CultureInfo.GetCultureInfo("en-US").DateTimeFormat.MonthNames[month - 1]);
        }

        MonthNameCache[resolvedCode] =
            months;

        return months;
    }

    public static string MonthYear(
        string languageCode,
        DateTime value)
    {
        return value.ToString(
            "MMMM yyyy",
            Culture(languageCode));
    }

    public static string DateTimeLong(
        string languageCode,
        DateTime value)
    {
        return value.ToString(
            "f",
            Culture(languageCode));
    }

    public static string TimeShort(
        string languageCode,
        DateTime value)
    {
        return value.ToString(
            "t",
            Culture(languageCode));
    }

    public static string TimeRangeShort(
        string languageCode,
        DateTime start,
        DateTime? end)
    {
        return end.HasValue
            ? $"{TimeShort(languageCode, start)}-{TimeShort(languageCode, end.Value)}"
            : TimeShort(languageCode, start);
    }

    public static string DateTimeRangeLong(
        string languageCode,
        DateTime start,
        DateTime? end)
    {
        return end.HasValue
            ? $"{DateTimeLong(languageCode, start)} - {DateTimeLong(languageCode, end.Value)}"
            : DateTimeLong(languageCode, start);
    }

    public static string[] DayNames(
        string languageCode)
    {
        var resolvedCode =
            EffectiveLanguageCode(languageCode);

        if (DayNameCache.TryGetValue(resolvedCode, out var cachedDayNames))
        {
            return cachedDayNames;
        }

        string[] dayNames =
        [
            Text(languageCode, "Day.Sun", "Sun"),
            Text(languageCode, "Day.Mon", "Mon"),
            Text(languageCode, "Day.Tue", "Tue"),
            Text(languageCode, "Day.Wed", "Wed"),
            Text(languageCode, "Day.Thu", "Thu"),
            Text(languageCode, "Day.Fri", "Fri"),
            Text(languageCode, "Day.Sat", "Sat")
        ];

        DayNameCache[resolvedCode] =
            dayNames;

        return dayNames;
    }

    public static string[] CategoryLabels(
        string languageCode)
    {
        var resolvedCode =
            EffectiveLanguageCode(languageCode);

        if (CategoryLabelCache.TryGetValue(resolvedCode, out var cachedCategoryLabels))
        {
            return cachedCategoryLabels;
        }

        string[] categoryLabels =
        [
            Text(languageCode, "Category.Raid", "Raid"),
            Text(languageCode, "Category.Maps", "Maps"),
            Text(languageCode, "Category.FC", "FC"),
            Text(languageCode, "Category.OceanFishing", "Ocean Fishing"),
            Text(languageCode, "Category.Custom", "Custom")
        ];

        CategoryLabelCache[resolvedCode] =
            categoryLabels;

        return categoryLabels;
    }

    public static string EffectiveLanguageCode(
        string languageCode)
    {
        if (!string.Equals(
                languageCode,
                "Auto",
                StringComparison.OrdinalIgnoreCase))
        {
            return languageCode.ToLowerInvariant();
        }

        var current =
            CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

        return Translations.ContainsKey(current)
            ? current
            : "en";
    }
}
