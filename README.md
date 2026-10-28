# RebornMaterial

Material Design 3 components for Dalamud ImGui plugins, based heavily on Google's Material Design 3 UI/UX design.

The whole colour scheme is generated from one accent colour, and every metric follows the user's text and element size settings.

> **This is the pnsdh fork** of [LTS-FFXIV/RebornMaterial](https://github.com/LTS-FFXIV/RebornMaterial), for plugins used with Korean (and other CJK) text. It merges upstream automatically every day (`.github/workflows/sync-upstream.yaml`); a conflict or a broken build opens an issue instead. Its changes are kept small, mostly in `M3Fork.cs` plus a few lines marked `Fork:`:
>
> - Titles, headlines and labels use the Dalamud default font instead of the Axis game font, which has no Hangul.
> - The type scale is a set of ratios of the default font size (headline 1.4, title large 1.2, title medium 1.1, body 1.0, body small 0.95, label small 0.85), so headings never end up smaller than body text when the default font is large. Setting-row supporting text uses the new `M3Fork.BodySmall`.
> - Icons use a Font Awesome font that follows the text size (`M3Fork.IconFont`) and are centred on the glyph's inked shape, not its advance box.
> - The library's own strings ("Clear search", "Minimize", …) go through `M3Fork.Localize`, so a plugin can translate them.
> - `M3Fork.PreloadFonts()` starts building the whole type scale early, so windows don't open in a stand-in font.

## Components

| Type | What it draws |
| --- | --- |
| `M3` | Scheme, shapes, spacing, fonts, scale, and the colour helpers (`Alpha`, `StateLayer`, `ContentOn`, `Severity`, `Harmonize`, `CustomColor`, `AccentFor`) |
| `M3Style` | Pushes the scheme onto ImGui's own style for a whole window, at a Comfortable, Compact or Tight density |
| `M3Widgets` | Buttons (with accent colours, game icons, and hold to confirm), icon buttons and toggles, switches, checkboxes (bare, labelled, or mixed), radios, segmented buttons, chips, pills, badges, sliders (including logarithmic), progress bars and spinners, text, search and number fields, hotkey fields, combos (with search and enums), menu items, help markers, reorder lists, banners, empty states, colour swatches, dividers, and the window title-bar actions |
| `M3Widgets.Row*` | A label and description with one control: switch, slider, number, combo, segmented buttons, colour, text or hotkey |
| `M3Card`, `M3ExpandableCard` | Filled, outlined and elevated cards, which can also collapse |
| `M3SettingRow`, `M3SubGroup` | Label and description on the left, control on the right. Wraps to two lines when narrow |
| `M3Navigation` | Navigation drawer and rail with section headings, and primary and secondary tabs that scroll when they don't fit |
| `M3Menu` | Popup and right-click menus, with submenus |
| `M3Tree` | Rows that open and close, for folders, groups and nested settings |
| `M3Flow` | Lays items out in a row that wraps onto new lines |
| `M3Text` | Text in the scheme's roles: muted, accent, headings, labels and severities, and bullets |
| `M3Chart` | Bar and line charts with a legend and hover readout, and the same data as a table |
| `M3Dialog` | Modal dialogs with actions |
| `M3Snackbar` | Queued toast messages with an optional action |
| `M3Badge` | A small tag on a line of text, or painted beside text you drew yourself |
| `M3TextField` | Filled single-line text field |
| `M3ActionIcon` | Action and game icons with a cooldown sweep and charge pips |
| `M3HeroHeader` | Window header with an image, or a gradient with animated stars |
| `M3TopAppBar` | A page title and subtitle for a window without a title bar, with room for its action pill |
| `M3WindowFold` | Minimises a window down to its title-bar actions, with animation, from a button or from code. Its contents fade and keep their layout while it folds |
| `M3Draw`, `M3Motion` | Drawing primitives (arcs, shadows, gradients, window backdrops, icons, focus rings) and per-widget easing |
| `M3Gallery` | A showcase of everything above, for a debug tab |

## Setup

Add the package:

```xml
<PackageReference Include="RebornMaterial" Version="1.0.0" />
```

Then wire it into the plugin's lifetime:

```csharp
using RebornMaterial;

public MyPlugin(IDalamudPluginInterface pluginInterface)
{
    Config = pluginInterface.GetPluginConfig() as Configuration ?? new Configuration();

    // Optional. Below 1 makes every component more compact than the Material spec (RSR uses 0.75).
    M3.ElementBaseline = 1f;
    M3.Initialize(pluginInterface, Config);

    pluginInterface.UiBuilder.Draw += OnDraw;
}

private void OnDraw()
{
    M3.BeginFrame(); // Once per frame, before any window draws.
    windowSystem.Draw();
}

public void Dispose()
{
    M3.Dispose(); // Releases the fonts and clears cached animation state.
}
```

### Settings

`M3.Initialize` takes any `IM3Settings`. Its property names match the existing Reborn configs, so a configuration class can usually implement it directly:

```csharp
public class Configuration : IPluginConfiguration, IM3Settings
{
    public Vector4 UiAccentColor { get; set; } = M3.DefaultSeed;
    public float UiTextScale { get; set; } = 1f;
    public float UiElementScale { get; set; } = 1f;

    // Optional. Scales the space around and between things; controls and text keep their size.
    public float UiPaddingScale { get; set; } = 1f;
}
```

`UiPaddingScale` has a default of 1, so settings without it still compile. If the settings live somewhere the library can't reference, write a small adapter that forwards to them. Pass nothing to use a plain `M3Settings`, which you can change through `M3.Settings`.

### Tooltips

Components show tooltips through `M3Tooltip`. To route them through your own tooltip code instead, for example one that honours a "show tooltips" option:

```csharp
M3Tooltip.Handler = text => MyTooltips.Show(text);
```

## Building locally

You need the .NET 10 SDK and a Dalamud dev install. Like any Dalamud plugin, the build finds Dalamud through XIVLauncher's `addon/Hooks/dev`, or through `DALAMUD_HOME`.

```bash
dotnet build -c Release
```

```bash
dotnet pack RebornMaterial/RebornMaterial.csproj -c Release -o artifacts
```
