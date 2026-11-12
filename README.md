# RebornMaterial

A wip UI library, full comprehensive readme coming soon tm

> **This is the pnsdh fork** of [LTS-FFXIV/RebornMaterial](https://github.com/LTS-FFXIV/RebornMaterial), for plugins used with Korean (and other CJK) text. It merges upstream automatically every day (`.github/workflows/sync-upstream.yaml`); a conflict or a broken build opens an issue instead. Its changes are kept small, mostly in `M3Fork.cs` plus a few lines marked `Fork:`:
>
> - Titles, headlines and labels keep the Axis game font. On the Korean Dalamud they're drawn in the game's Korean Axis (KrnAxis, Hangul included) at every size: Dalamud maps it from 14px up, so smaller sizes are shrunk from 14px. Without KrnAxis, Hangul is filled in from Dalamud's Noto.
> - The type scale is a set of ratios of the default font size (upstream's pixel sizes over the 16px default, with label small raised to 0.85 and a new body small at 0.95), so headings never end up smaller than body text when the default font is large. `M3Fork.BodySmall` is there for a plugin's own secondary text.
> - Icons use a Font Awesome font that follows the text size (`M3Fork.IconFont`) and are centred on the glyph's inked shape, not its advance box. Leading icons in navigation drawers, trees and setting rows sit in a fixed-width slot, so the labels after them line up.
> - The library's own strings ("Clear search", "Minimize", …) go through `M3Fork.Localize`, so a plugin can translate them.
> - `M3Fork.PreloadFonts()` starts building the whole type scale early, so windows don't open in a stand-in font.
