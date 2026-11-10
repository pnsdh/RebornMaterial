# RebornMaterial

Material Design 3 components for Dalamud ImGui plugins, based heavily on Google's Material Design 3 UI/UX design.

The whole colour scheme is generated from one accent colour, and every metric follows the user's text and element size settings.

> **This is the pnsdh fork** of [LTS-FFXIV/RebornMaterial](https://github.com/LTS-FFXIV/RebornMaterial), for plugins used with Korean (and other CJK) text. It merges upstream automatically every day (`.github/workflows/sync-upstream.yaml`); a conflict or a broken build opens an issue instead. Its changes are kept small, mostly in `M3Fork.cs` plus a few lines marked `Fork:`:
>
> - Titles, headlines and labels keep the Axis game font. On the Korean Dalamud they're drawn in the game's Korean Axis (KrnAxis, Hangul included) at every size: Dalamud maps it from 14px up, so smaller sizes are shrunk from 14px. Without KrnAxis, Hangul is filled in from Dalamud's Noto.
> - The type scale is a set of ratios of the default font size (upstream's pixel sizes over the 16px default, with label small raised to 0.85 and a new body small at 0.95), so headings never end up smaller than body text when the default font is large. `M3Fork.BodySmall` is there for a plugin's own secondary text.
> - Icons use a Font Awesome font that follows the text size (`M3Fork.IconFont`) and are centred on the glyph's inked shape, not its advance box. Leading icons in navigation drawers, trees and setting rows sit in a fixed-width slot, so the labels after them line up.
> - The library's own strings ("Clear search", "Minimize", …) go through `M3Fork.Localize`, so a plugin can translate them.
> - `M3Fork.PreloadFonts()` starts building the whole type scale early, so windows don't open in a stand-in font.

## Components

| Type | What it draws |
| --- | --- |
| `M3` | Scheme, shapes, spacing, fonts, scale, motion, and the colour helpers (`Alpha`, `StateLayer`, `ContentOn`, `Severity`, `OnSeverity`, `OnSeverityContainer`, `Harmonize`, `CustomColor`, `AccentFor`) |
| `M3Scheme`, `M3ColorMath` | Every Material color role, including the on-colours for secondary, tertiary, warning, success and info, plus `ToHex` and `TryParseHex` for `#RGB` to `#RRGGBBAA` |
| `M3Style` | Pushes the scheme onto ImGui's own style for a whole window, at a Comfortable, Compact or Tight density |
| `M3Widgets` | Buttons (with accent colors, game icons, and hold to confirm), copy buttons, icon buttons and toggles, game icons and game-icon buttons, switches, checkboxes (bare, labelled, or mixed), radios, segmented buttons, chips, pills (with a symbol or a texture), plugin-status pills, badges, sliders (including logarithmic and two-thumb range sliders), progress bars and spinners, text, search and number fields, hotkey fields (which can also record Ctrl, Shift or Alt on their own), combos (with search and enums), picker fields for popups of your own, menu items, help markers, reorder lists, banners, empty states, colour swatches, preset colour choices, dividers, and the window title-bar actions |
| `M3Widgets.Row*` | A label and description with one control: switch, switch with plugin-status pill, slider, range, two-field number range, number, combo, segmented buttons, colour, colour choices, text (or password), hotkey, button, hold button, icon button, copy, or a read-only value |
| `M3Card`, `M3ExpandableCard` | Filled, outlined and elevated cards, which can also collapse |
| `M3SettingRow`, `M3SubGroup` | Label and description on the left, control on the right. Wraps to two lines when narrow |
| `M3Navigation` | Navigation drawer and rail with section headings, and primary and secondary tabs that scroll when they don't fit |
| `M3Menu` | Popup and right-click menus, with submenus |
| `M3Tree` | Rows that open and close, for folders, groups and nested settings |
| `M3Flow` | Lays items out in a row that wraps onto new lines |
| `M3Text` | Text in the scheme's roles: muted, accent, headings, labels and severities, and bullets |
| `M3Chart` | Bar and line charts with a legend and hover readout, and the same data as a table |
| `M3Dialog` | Modal dialogs with actions, which can stay open after a press to show an error |
| `M3Table` | Table helpers: a plain header row with a line under it, header cells with tooltips, centred cells and in-cell progress bars |
| `M3Panel` | A bordered pane on a surface container, for list and detail layouts |
| `M3Window`, `M3FoldWindow`, `M3PanelWindow` | Window base classes: one drawn in the scheme, one that folds to its action pill, and a single-page one with the logo, a title and a status line |
| `M3ShellWindow`, `M3Page` | A window with a navigation drawer (a rail when narrow), a top app bar, optional search, and pages that the controller's shoulder buttons step through |
| `M3SettingsPage`, `M3SettingsSearch` | Settings pages made of cards of entries, and a search across them with links back to each page |
| `M3Brand` | The plugin's name, version and logo, used by the window classes |
| `M3Gamepad` | Controller support: button presses read once per frame, menus that open from a controller button, page stepping, and opt-in blocking of game input while your windows have controller focus |
| `M3Plugins` | Checks whether the plugins a feature needs are installed, enabled and new enough |
| `M3Snackbar` | Queued toast messages with an optional action |
| `M3Badge` | A small tag on a line of text, or painted beside text you drew yourself |
| `M3TextField` | Filled single-line text field |
| `M3ActionIcon` | Action and game icons with a cooldown sweep and charge pips |
| `M3HeroHeader` | Window header with an image, or a gradient with animated stars |
| `M3TopAppBar` | A page title and subtitle for a window without a title bar, with room for its action pill |
| `M3WindowFold` | Minimises a window down to its title-bar actions, with animation, from a button or from code. Its contents fade and keep their layout while it folds |
| `M3Draw`, `M3Motion` | Drawing primitives (arcs, shadows, gradients, window backdrops, icons, status dots, chevrons, focus rings), per-widget easing, and `M3Motion.Step` for values you keep yourself. All of it follows the animation length setting |
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

    // Optional. Adjusts the duration of animations; 0 turns them off.
    public float UiMotionScale { get; set; } = 1f;
}
```

`UiPaddingScale` and `UiMotionScale` default to 1, so settings without them still compile. If the settings live somewhere the library can't reference, write a small adapter that forwards to them. Pass nothing to use a plain `M3Settings`, which you can change through `M3.Settings`.

### Windows

Derive from `M3PanelWindow` for a single-page window or `M3ShellWindow` for one with pages, and set the brand once at startup:

```csharp
M3Brand.Name = "My Plugin";
M3Brand.Version = pluginInterface.Manifest.AssemblyVersion.ToString();
M3Brand.FallbackIcon = FontAwesomeIcon.Dragon;
M3Brand.SetLogoResource(typeof(MyPlugin).Assembly, "MyPlugin.Images.Logo.png");

// Optional: every M3Window's density, for example from a compact-layout setting.
M3Window.DefaultDensity = Config.CompactLayout ? M3Density.Tight : M3Density.Compact;
```

### Controller

This is mostly added because of GBR.
With Dalamud's gamepad navigation on, controller users can reach every component. On top of that:

- `M3Menu.BeginContext` opens on `M3Gamepad.ContextMenuButton` (Square or X by default, like the game) as well as on a right-click.
- `M3ShellWindow` steps through its pages on `M3Gamepad.PreviousPageButton` and `NextPageButton` (L1 and R1). Elsewhere, `M3Gamepad.PageStep()` and `M3Navigation.Step` do the same for tabs.
- `M3Gamepad.JustPressed`, `IsHeld` and `JustReleased` read buttons that were sampled once in `M3.BeginFrame`, so a press counts for exactly one frame.

To stop the game from moving, turning the camera or acting on buttons while one of your windows has controller focus, turn on input blocking once after `M3.Initialize`:

```csharp
M3Gamepad.EnableInputBlocking(); // movement, camera and buttons; each can be left out
```

Fold windows report their focus themselves. Set `BlocksGameInput = false` on a window that should leave the game alone, such as a status window used while moving. A window that isn't a fold window calls `ReportFocus()` from its `Draw`.

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
