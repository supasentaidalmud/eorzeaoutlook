using Dalamud.Bindings.ImGui;

namespace EorzeaOutlook.Windows.Components;

internal static class UiText
{
    public static string FitToAvailableWidth(
        string text,
        float padding = 0)
    {
        var availableWidth =
            ImGui.GetContentRegionAvail().X - padding;

        if (ImGui.CalcTextSize(text).X <= availableWidth)
            return text;

        const string ellipsis =
            "...";

        for (int length = text.Length - 1; length > ellipsis.Length; length--)
        {
            var candidate =
                text[..length].TrimEnd() + ellipsis;

            if (ImGui.CalcTextSize(candidate).X <= availableWidth)
                return candidate;
        }

        return ellipsis;
    }
}
