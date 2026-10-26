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

## Using it in a window

```csharp
private M3Style.Scope _theme;

public override void PreDraw()
{
    _theme = M3Style.Push();
}

public override void PostDraw()
{
    _theme.Dispose();
    _theme = default;
}

public override void Draw()
{
    using (var card = M3Card.Begin("general", "General", FontAwesomeIcon.Cog))
    {
        var row = M3SettingRow.Begin("Enabled", "Turns the feature on.", M3Widgets.SwitchSize());
        var enabled = Config.Enabled;
        if (M3Widgets.Switch("##enabled", ref enabled))
        {
            Config.Enabled = enabled;
        }


        M3SettingRow.End(row);

        if (M3Widgets.Button("##save", "Save", M3ButtonStyle.Filled, FontAwesomeIcon.Save))
        {
            Config.Save();
            M3Snackbar.Show("Saved");
        }
    }

    // Last, so it draws on top.
    M3Snackbar.Draw(ImGui.GetWindowPos(), ImGui.GetWindowPos() + ImGui.GetWindowSize());
}
```

To scale everything in one window, push `M3.PushWindowScale(scale)` before `M3Style.Push` and dispose it after.

### Windows without a title bar

`M3WindowFold` draws the title-bar actions as a pill in the top-right corner, and folds the window down into it. `M3TopAppBar` gives the page a title that leaves room for the pill, and says how many actions fit:

```csharp
private readonly M3WindowFold _fold = new();
private int _shownActions = Actions.Length;

public override void PreDraw()
{
    _theme = M3Style.Push();

    // The fold adds flags while it runs, so start from the window's own each frame.
    Flags = ImGuiWindowFlags.NoTitleBar | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse;
    if (_fold.Prepare(this, _shownActions, Brand))
    {
        // Fully restored: give back the position and size the fold took over.
        Position = null;
        Size = DefaultSize;
        SizeCondition = ImGuiCond.FirstUseEver;
        SizeConstraints = DefaultSizeConstraints;
    }
}

public override void PostDraw()
{
    _fold.PopStyle();
    _theme.Dispose();
    _theme = default;
}

public override void Draw()
{
    _fold.BeginDraw();
    if (_fold.Amount < 1f)
    {
        using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, _fold.ContentAlpha);
        M3Draw.WindowBackdrop(_fold.Rounding);

        using var content = _fold.BeginContent("##content");
        if (content)
        {
            using (var bar = M3TopAppBar.Begin("Settings", "What this page is for", Actions.Length, Brand))
            {
                _shownActions = bar.ShownActions;
                _fold.BarTop = bar.BarTop;
            }

            // The page.
        }
    }

    // Last, so the pill stays on top while the window folds. Actions drop from the left when they don't fit.
    var first = Actions.Length - _shownActions;
    var pressed = _fold.DrawBar("##actions", Actions.AsSpan(first), Brand, out var closed, M3.Scheme.SurfaceContainerHigh);
    if (pressed >= 0)
    {
        OnAction(first + pressed); // An index into Actions.
    }

    IsOpen &= !closed;
}
```

For a header of your own, `M3Widgets.WindowActionsThatFit` says how many actions fit in a width.

### Density

`M3Style.Push` takes an `M3Density`:

| Density | Use it for |
| --- | --- |
| `Comfortable` | The default. Material's own spacing, for settings windows |
| `Compact` | Tighter item spacing and smaller window corners, for small overlays |
| `Tight` | Compact spacing and thin window edges. Cards, setting rows, expandable card headers and the navigation drawer also use less padding |

Window edges, item spacing and component padding follow the padding setting through `M3.PaddingScale`. Frame padding sets the height of inputs, so it follows the element size instead. In a custom layout, use `M3.Space1` to `Space3` for gaps, or `M3Style.Spacing(regular, tight)` for padding that should also tighten inside a `Tight` window.

`M3Gallery.Draw()` shows every component, with controls for the preview scale and accent colour. Put it in a debug tab to try things out.

### Disabled controls

Most controls take `enabled`. A disabled control still shows its tooltip, so the tooltip can say why it's off:

```csharp
M3Widgets.Button("##start", "Start", M3ButtonStyle.Filled, enabled: listReady, tooltip: listReady ? null : "Add an item to the list first.");
```

Every row helper also takes `enabled`, and dims its label along with the control.

### Setting rows

Each `Row*` helper draws a label, an optional description, and one control. Together they cover the usual setting types, which suits a settings page built by reflection:

| Value | Helper |
| --- | --- |
| `bool` | `RowSwitch` |
| `float`, `int` in a range | `RowDragFloat`, `RowDragInt` (pass `logarithmic: true` for wide ranges) |
| `float`, `int` typed in | `RowNumber`, with an optional step and unit |
| An enum | `RowCombo<T>`, or `RowCombo(label, type, ref value)` when the type is only known at run time |
| A list of names | `RowCombo(label, ref index, items)`, or `RowCombo(label, ref index, count, itemLabel)` to label them on demand |
| A few choices that should all stay visible | `RowSegmented` |
| A colour | `RowColor`, with an optional default to reset to |
| Text | `RowText` |
| A hotkey | `RowHotkey`, which stores an `M3Hotkey` of a `VirtualKey` and modifiers |

A label like `"Volume###volume"` shows "Volume" and uses "volume" as the ID.

### Menus and trees

`M3Menu.Item` closes every open menu when it's picked, submenus included:

```csharp
using (var menu = M3Menu.BeginContext("##list_menu"))
{
    if (menu.IsOpen)
    {
        if (M3Menu.Item("##rename", "Rename", FontAwesomeIcon.Pen)) { /* ... */ }

        using (var sub = M3Menu.BeginSub("##move", "Move to", FontAwesomeIcon.FolderOpen))
        {
            if (sub.IsOpen && M3Menu.Item("##daily", "Daily")) { /* ... */ }
        }
    }
}
```

`M3Tree` remembers which nodes are open. The row is the last item until the first child, so a right-click menu can follow it:

```csharp
using (var folder = M3Tree.Node("folder", "Daily lists", icon: FontAwesomeIcon.Folder))
{
    if (folder.Open)
    {
        using var list = M3Tree.Node("list", "Ores", selected: isSelected, leaf: true);
        if (list.Clicked) { /* select it */ }
    }
}
```

### Charts

```csharp
// A width of 0 fills the space available.
M3Chart.Bars("##sizes", buckets, [new("Average", average), new("Large", large)], new Vector2(0f, 220f * M3.Scale), yTitle: "Catches");
M3Chart.Table("##sizes_table", buckets, series);
```

Series take the colours in `M3Chart.Palette` in order. There are eight, and they never repeat, so fold any extra series into an "Other" series. The palette was checked for colour-blind separation and contrast against the scheme's surfaces.

### Keyboard and gamepad

Controls show a focus ring while the keyboard or a gamepad has focus, and never for the mouse. Tree nodes open with the right arrow and close with the left, and a reorder list moves the focused row with Ctrl and up or down.

### Action icons

`M3ActionIcon` takes a texture or a game icon ID, plus an optional `M3Cooldown`:

```csharp
var cooldown = new M3Cooldown(isCoolingDown, recastTime, elapsed, currentCharges, maxCharges);
if (M3ActionIcon.Draw("##fire", action.Icon, 48f * M3.Scale, cooldown, enabled: level >= action.Level, tooltip: action.Name))
{
    // Pressed.
}
```

## Building locally

You need the .NET 10 SDK and a Dalamud dev install. Like any Dalamud plugin, the build finds Dalamud through XIVLauncher's `addon/Hooks/dev`, or through `DALAMUD_HOME`.

```bash
dotnet build -c Release
```

```bash
dotnet pack RebornMaterial/RebornMaterial.csproj -c Release -o artifacts
```
