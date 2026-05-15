# EorzeaOutlook

EorzeaOutlook is a Dalamud plugin for personal calendar events, reminders, and official Lodestone in-game event tracking.

## Screenshot

![EorzeaOutlook calendar view](docs/screenshot.png)

## Building

1. Open `EorzeaOutlook.sln`.
2. Build the solution in `Debug` or `Release`.
3. The plugin output is written to `EorzeaOutlook/bin/x64/Debug/`.

## Dalamud Dev Plugin Loading

1. Build the solution.
2. In `/xlplugins`, add the output folder as a dev plugin path:
   `C:\temp\EorzeaOutlook\EorzeaOutlook\bin\x64\Debug`
3. Reload dev plugins.

Dalamud expects the generated `EorzeaOutlook.json` manifest next to `EorzeaOutlook.dll`, so add the output folder rather than the DLL itself.

## In Game

Use `/pcalendar` or `/pcal` to open Eorzea Outlook.
