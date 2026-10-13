using Dalamud.Interface.ManagedFontAtlas;

namespace RebornMaterial;

/// <summary>
/// Additions carried by the pnsdh fork of RebornMaterial (Korean / CJK use), kept in this one file so merges from
/// upstream rarely touch them. Upstream files only call into here at a few points:
/// <list type="bullet">
/// <item>the type scale in <see cref="M3"/> uses the Dalamud default font at ratios of its size (the Axis game
/// font has no Hangul, and fixed pixel sizes end up smaller than body text when the default font is large);</item>
/// <item><see cref="M3Draw"/> draws icons with <see cref="IconFont"/>, which follows the text size, and centres
/// them on the glyph's inked shape;</item>
/// <item>the library's own strings go through <see cref="T"/>, so a plugin can translate them.</item>
/// </list>
/// </summary>
public static class M3Fork
{
	// Type scale, as ratios of Dalamud's default font size (before the text size setting). Sizes below body text
	// stay close to it: Hangul gets hard to read quickly.
	internal const float HeadlineSmallRatio = 1.4f;
	internal const float TitleLargeRatio = 1.2f;
	internal const float TitleMediumRatio = 1.1f;
	internal const float BodySmallRatio = 0.95f;
	internal const float LabelSmallRatio = 0.85f;

	private const int IconFontCapacity = 4;

	private static readonly Lock _lock = new();
	private static readonly Dictionary<int, IFontHandle> _iconHandles = [];

	/// <summary>
	/// Translates the library's own built-in strings (tooltips such as "Clear search", placeholder text). Set it to
	/// the plugin's translation function; it receives the English text and returns what to show.
	/// </summary>
	public static Func<string, string>? Localize { get; set; }

	/// <summary>Secondary prose meant to be read, such as a setting's supporting text.</summary>
	public static ImFontPtr BodySmall => M3Fonts.GetDefaultFont(BodySmallRatio * M3.TextScale);

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

	/// <summary><paramref name="english"/> through <see cref="Localize"/>, or unchanged when none is set.</summary>
	internal static string T(string english)
	{
		return Localize?.Invoke(english) ?? english;
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
			return font.IsLoaded() ? font : null;
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
