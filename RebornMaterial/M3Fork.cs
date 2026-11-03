using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;

namespace RebornMaterial;

/// <summary>
/// Additions carried by the pnsdh fork of RebornMaterial (Korean / CJK use), kept in this one file so merges from
/// upstream rarely touch them. Upstream files only call into here at a few points:
/// <list type="bullet">
/// <item>the type scale in <see cref="M3"/> keeps upstream's Axis game font, the Korean Axis at every size on the
/// Korean Dalamud and Hangul from Dalamud's Noto elsewhere (<see cref="NewAxisFontHandle"/>), and is sized at
/// ratios of the default font (fixed pixel sizes end up smaller than body text when the default font is large);</item>
/// <item><see cref="M3Draw"/> draws icons with <see cref="IconFont"/>, which follows the text size, and centres
/// them on the glyph's inked shape;</item>
/// <item>navigation drawer items, tree rows and setting rows put leading icons in a fixed-width slot
/// (<see cref="IconSlotWidth"/>), so their labels line up;</item>
/// <item>the library's own strings go through <see cref="T"/>, so a plugin can translate them.</item>
/// </list>
/// </summary>
public static partial class M3Fork
{
	// Type scale, as ratios of Dalamud's default font size (before the text size setting): upstream's pixel sizes
	// over the 16px default font they were tuned for. Sizes below body text stay closer to it than upstream's 11px
	// labels: Hangul gets hard to read quickly.
	internal const float HeadlineSmallRatio = 22f / 16f;
	internal const float TitleLargeRatio = 19f / 16f;
	internal const float TitleMediumRatio = 16f / 16f;
	internal const float BodySmallRatio = 0.95f;
	internal const float LabelSmallRatio = 0.85f;

	private const int IconFontCapacity = 4;

	// The Korean Dalamud draws Axis in KrnAxis, Hangul included, only between these sizes (after the atlas scale).
	private const float KrnAxisMinPx = 14f;
	private const float KrnAxisMaxPx = 24f;

	private static bool? _hasKrnAxis;
	private static readonly Lock _lock = new();
	private static readonly Dictionary<int, IFontHandle> _iconHandles = [];

	/// <summary>
	/// Translates the library's own built-in strings (tooltips such as "Clear search", placeholder text). Set it to
	/// the plugin's translation function; it receives the English text and returns what to show.
	/// </summary>
	public static Func<string, string>? Localize { get; set; }

	/// <summary>Pixel size of a type scale <paramref name="ratio"/>, before the text size setting.</summary>
	internal static float Px(float ratio)
	{
		return M3.PluginInterface.UiBuilder.FontDefaultSizePx * ratio;
	}

	/// <summary>
	/// The Axis game font at <paramref name="px"/>. The Korean Dalamud picks the game's Korean Axis (KrnAxis, Hangul
	/// drawn to match) only from 14 to 24px, and the global Axis, which has no Hangul, outside that. So on the Korean
	/// Dalamud, below 14px the font is built at 14px and scaled down with <see cref="ImFont.Scale"/>
	/// (<see cref="AxisBuildPx"/>), and past 24px it's KrnAXIS_360 read from the game files (M3Fork.KrnAxis36.cs).
	/// Without KrnAxis, Hangul comes from Dalamud's Noto; Dalamud copies game glyphs in after the atlas is built, over
	/// merged ones, so Noto only fills the glyphs the game font lacks.
	/// </summary>
	internal static IFontHandle NewAxisFontHandle(int px)
	{
		return M3.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(e =>
		{
			e.OnPreBuild(tk =>
			{
				if (UseKrnAxis36(px * tk.Scale))
				{
					tk.Font = NewKrnAxis36Font(tk, px);
					return;
				}

				var buildPx = AxisBuildPx(px, tk.Scale);
				var font = tk.AddGameGlyphs(new GameFontStyle(GameFontFamily.Axis, buildPx), null, default);
				if (!HasKrnAxis)
				{
					// KrnAxis has every Hangul syllable; elsewhere the game font has none.
					tk.AttachExtraGlyphsForDalamudLanguage(new SafeFontConfig { SizePx = buildPx, MergeFont = font });
				}

				tk.Font = font;
			});
			e.OnPostBuild(tk =>
			{
				if (UseKrnAxis36(px * tk.Scale))
				{
					AddKrnAxis36Glyphs(tk, tk.Font, px);
				}
				else
				{
					tk.Font.Scale = px / AxisBuildPx(px, tk.Scale);
				}
			});
		});
	}

	/// <summary>Whether the Korean Dalamud draws Axis in KrnAxis, Hangul included.</summary>
	private static bool HasKrnAxis => _hasKrnAxis ??= IsKrnAxis(16f);

	private static bool IsKrnAxis(float px)
	{
		return GameFontStyle.GetRecommendedFamilyAndSize(GameFontFamily.Axis, px * 3f / 4f).ToString().StartsWith("Krn", StringComparison.Ordinal);
	}

	/// <summary>
	/// The size to build Axis at for <paramref name="px"/>: the nearest size the Korean Dalamud draws in KrnAxis, or
	/// <paramref name="px"/> itself without KrnAxis (the global Dalamud has a sharp Axis at every size). Below 14px it
	/// is shrunk from KrnAxis 12pt, which stays sharp. Past 24px KrnAXIS_360 takes over (<see cref="UseKrnAxis36"/>);
	/// only if that can't be read is it enlarged from KrnAxis 18pt, and softens.
	/// </summary>
	private static float AxisBuildPx(int px, float atlasScale)
	{
		if (!HasKrnAxis)
		{
			return px;
		}

		var effective = px * atlasScale;
		if (effective < KrnAxisMinPx)
		{
			return KrnAxisMinPx / atlasScale;
		}

		return effective > KrnAxisMaxPx && !IsKrnAxis(effective) ? KrnAxisMaxPx / atlasScale : px;
	}

	/// <summary>Secondary prose meant to be read, in the default font a little smaller than body text. The library
	/// itself doesn't use it (setting rows keep upstream's label small); it's for a plugin's own text.</summary>
	public static ImFontPtr BodySmall => M3Fonts.GetDefaultFont(BodySmallRatio * M3.TextScale);

	/// <summary>
	/// Width of a column of icons, at least <paramref name="minimum"/>: Font Awesome's fixed width (1.25 em) at the
	/// icon size. Glyphs differ in width, more visibly once they follow the text size, so text placed after each
	/// icon's own width starts at a different x on every row; centred in this slot, rows line up.
	/// </summary>
	internal static float IconSlotWidth(float minimum)
	{
		return MathF.Max(minimum, IconFont.FontSize * 1.25f);
	}

	/// <summary>Font Awesome at body text size, so icons scale with the text beside them.</summary>
	public static ImFontPtr IconFont
	{
		get
		{
			var key = Math.Max(1, (int)MathF.Round(M3.PluginInterface.UiBuilder.FontDefaultSizePx * M3.TextScale));
			lock (_lock)
			{
				if (!_iconHandles.TryGetValue(key, out var handle))
				{
					while (_iconHandles.Count >= IconFontCapacity)
					{
						var oldest = _iconHandles.Keys.First();
						_iconHandles[oldest].Dispose();
						_ = _iconHandles.Remove(oldest);
					}

					handle = M3.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(
						e => e.OnPreBuild(tk => tk.AddFontAwesomeIconFont(new SafeFontConfig { SizePx = key })));
					_iconHandles[key] = handle;
				}

				// Until this size is built, the Dalamud icon font stands in.
				return Loaded(handle) ?? UiBuilder.IconFont;
			}
		}
	}

	/// <summary>
	/// Starts building every font of the type scale (and the icon font) at the current text size. Building the
	/// Hangul glyph set takes seconds, so calling this early (e.g. on the plugin's first draw) keeps windows from
	/// opening in a stand-in font and visibly resizing. Must be called from the draw loop.
	/// </summary>
	public static void PreloadFonts()
	{
		_ = M3.HeadlineSmall;
		_ = M3.TitleLarge;
		_ = M3.TitleMedium;
		_ = M3.Body;
		_ = BodySmall;
		_ = M3.LabelSmall;
		_ = IconFont;
	}

	/// <summary>
	/// Top of an icon beside a card title starting at <paramref name="titleTop"/>, centred on the title's line. The
	/// title font is larger than the icon line once the type scale grows, so a fixed offset leaves the icon high.
	/// </summary>
	internal static float TitleIconTop(FontAwesomeIcon icon, float titleTop)
	{
		float lineHeight;
		using (Dalamud.Interface.Utility.Raii.ImRaii.PushFont(M3.TitleMedium))
		{
			lineHeight = ImGui.GetTextLineHeight();
		}

		return titleTop + ((lineHeight - M3Draw.MeasureIcon(icon).Y) * 0.5f);
	}

	/// <summary><paramref name="english"/> through <see cref="Localize"/>, or unchanged when none is set.</summary>
	internal static string T(string english)
	{
		return Localize?.Invoke(english) ?? english;
	}

	/// <summary>
	/// Marks the colour swatch at <paramref name="center"/> as the one that opens the colour picker. While it shows
	/// no colour of its own, it becomes a colour wheel (hues around the edge, white in the middle); once it shows
	/// <paramref name="own"/>, a thin ring of hues stays around that colour. The swatch's outline is drawn again on
	/// top, since the wheel's own edge isn't anti-aliased.
	/// </summary>
	internal static void PickerSwatchMark(ImDrawListPtr drawList, Vector2 center, float radius, Vector4? own, bool hovered, float dim)
	{
		const int segments = 48;
		var scale = M3.Scale;
		var inner = own is null ? 0f : radius - (3.5f * scale);
		var uv = ImGui.GetFontTexUvWhitePixel();
		var white = M3.U32(Vector4.One, dim);
		for (var i = 0; i < segments; i++)
		{
			var from = Hue(i / (float)segments);
			var to = Hue((i + 1) / (float)segments);
			var c0 = M3.U32(from, dim);
			var c1 = M3.U32(to, dim);
			var d0 = Direction(i / (float)segments);
			var d1 = Direction((i + 1) / (float)segments);
			if (own is null)
			{
				drawList.PrimReserve(3, 3);
				drawList.PrimVtx(center, uv, white);
				drawList.PrimVtx(center + (d0 * radius), uv, c0);
				drawList.PrimVtx(center + (d1 * radius), uv, c1);
			}
			else
			{
				drawList.PrimReserve(6, 6);
				drawList.PrimVtx(center + (d0 * inner), uv, c0);
				drawList.PrimVtx(center + (d0 * radius), uv, c0);
				drawList.PrimVtx(center + (d1 * radius), uv, c1);
				drawList.PrimVtx(center + (d0 * inner), uv, c0);
				drawList.PrimVtx(center + (d1 * radius), uv, c1);
				drawList.PrimVtx(center + (d1 * inner), uv, c1);
			}
		}

		if (own is { } shown)
		{
			drawList.AddCircle(center, inner, M3.U32(shown, shown.W * dim), 32, 1f * scale);
		}

		drawList.AddCircle(center, radius, M3.U32(M3.Scheme.Outline, (hovered ? 1f : 0.6f) * dim), 32, 1.5f * scale);

		// Red at the top, going clockwise.
		static Vector2 Direction(float turn) => new(MathF.Sin(turn * MathF.Tau), -MathF.Cos(turn * MathF.Tau));

		static Vector4 Hue(float turn)
		{
			float r = 0f, g = 0f, b = 0f;
			ImGui.ColorConvertHSVtoRGB(turn, 0.85f, 1f, ref r, ref g, ref b);
			return new Vector4(r, g, b, 1f);
		}
	}

	internal static void DisposeAll()
	{
		lock (_lock)
		{
			foreach (var handle in _iconHandles.Values)
			{
				handle.Dispose();
			}

			_iconHandles.Clear();
		}

		DisposeKrnAxis36();
		Localize = null;
	}

	private static ImFontPtr? Loaded(IFontHandle handle)
	{
		if (!handle.Available)
		{
			return null;
		}

		try
		{
			using var locked = handle.Lock();
			var font = locked.ImFont;
			return font.IsLoaded() ? font : (ImFontPtr?)null; // a bare null becomes a null-pointer ImFontPtr
		}
		catch (Exception)
		{
			return null;
		}
	}

	/// <summary>
	/// Where to draw <paramref name="icon"/> in <paramref name="font"/> at <paramref name="fontSize"/> so that the
	/// glyph's inked shape, not its advance box, is centred on <paramref name="center"/>. Many Font Awesome glyphs
	/// sit unevenly inside their advance width and line height, so centring the box leaves them off-centre.
	/// </summary>
	internal static unsafe Vector2 InkCenteredPosition(ImFontPtr font, float fontSize, FontAwesomeIcon icon, Vector2 center, Vector2 fallbackSize)
	{
		var glyph = font.FindGlyph((ushort)icon);
		if (glyph != null && glyph->X1 > glyph->X0 && glyph->Y1 > glyph->Y0 && font.FontSize > 0f)
		{
			// Glyph bounds are in the font's own units, relative to the pen position.
			var scale = fontSize / font.FontSize;
			var ink = new Vector2((glyph->X0 + glyph->X1) * 0.5f, (glyph->Y0 + glyph->Y1) * 0.5f) * scale;
			return center - ink;
		}

		return center - (fallbackSize * 0.5f);
	}
}
