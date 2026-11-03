using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.IoC;
using Dalamud.Plugin.Services;
using Lumina.Data.Files;

namespace RebornMaterial;

// KrnAXIS_360, the game's Korean Axis at 36pt (48px). The Korean client ships it, but the Korean Dalamud only maps
// KrnAxis 12, 14 and 18pt, so past 24px Axis would fall back to the global font, which has no Hangul. This builds it
// the way Dalamud builds its own game fonts: each texture channel becomes an atlas texture of its own, and the
// glyphs point into it, scaled from 48px to the size asked for. If the Korean Dalamud maps it itself one day,
// IsKrnAxis sees that and Dalamud's is used instead.
public static partial class M3Fork
{
	private const string KrnAxis36Path = "common/font/KrnAXIS_360.fdt";
	private const string KrnTexturePath = "common/font/font_krn_{0}.tex";

	// Dalamud's horizontal offset for the 36pt Axis files, in font units.
	private const float KrnAxis36HorizontalOffset = -4f;

	private const int DxgiFormatB8G8R8A8 = 87;
	private const int DxgiFormatB4G4R4A4 = 115;

	// Dalamud's own fallback and ellipsis characters.
	private const string FallbackChars = "〓�?-";
	private const string EllipsisChars = "…\u0085";

	private static FdtReader? _krnAxis36;
	private static bool _krnAxis36Read;
	private static readonly Dictionary<int, IDalamudTextureWrap> _krnChannels = [];

	private sealed class ForkServices
	{
		[PluginService]
		internal static IDataManager DataManager { get; private set; } = null!;
	}

	private static bool UseKrnAxis36(float effectivePx)
	{
		return effectivePx > KrnAxisMaxPx && HasKrnAxis && !IsKrnAxis(effectivePx) && KrnAxis36() != null;
	}

	/// <summary>The KrnAXIS_360 glyph table, or null when the game files don't have it.</summary>
	private static FdtReader? KrnAxis36()
	{
		lock (_lock)
		{
			if (!_krnAxis36Read)
			{
				_krnAxis36Read = true;
				try
				{
					_ = M3.PluginInterface.Create<ForkServices>();
					var file = ForkServices.DataManager.GetFile(KrnAxis36Path);
					_krnAxis36 = file == null ? null : new FdtReader(file.Data);
				}
				catch (Exception)
				{
					_krnAxis36 = null;
				}
			}

			return _krnAxis36;
		}
	}

	/// <summary>An empty font at <paramref name="px"/>, filled in after the build by <see cref="AddKrnAxis36Glyphs"/>.</summary>
	private static ImFontPtr NewKrnAxis36Font(IFontAtlasBuildToolkitPreBuild tk, int px)
	{
		return tk.AddDalamudAssetFont(Dalamud.DalamudAsset.NotoSansCjkMedium, new SafeFontConfig { SizePx = px, GlyphRanges = [' ', ' ', 0] });
	}

	/// <summary>
	/// Fills <paramref name="font"/> with KrnAXIS_360's glyphs at <paramref name="px"/>, the way Dalamud's game fonts
	/// are filled: positions in font units scaled to the size and snapped to the atlas' pixel grid.
	/// </summary>
	private static unsafe void AddKrnAxis36Glyphs(IFontAtlasBuildToolkitPostBuild tk, ImFontPtr font, int px)
	{
		var fdt = KrnAxis36()!;
		var header = fdt.FontHeader;
		var scale = px / (header.Size * 4f / 3f);
		var grid = 1f / tk.Scale;
		float Snap(float units) => MathF.Round(units * scale / grid) * grid;

		var textureSize = new Vector2(header.TextureWidth, header.TextureHeight);
		var textures = new Dictionary<int, int>();
		var pixels = new Dictionary<int, byte[]>();
		foreach (var glyph in fdt.Glyphs)
		{
			var codepoint = glyph.CharInt;
			if (codepoint is < 0x20 or > char.MaxValue)
			{
				continue;
			}

			if (!textures.TryGetValue(glyph.TextureIndex, out var texture))
			{
				texture = tk.StoreTexture(KrnChannel(glyph.TextureIndex, pixels).CreateWrapSharingLowLevelResource(), true);
				textures[glyph.TextureIndex] = texture;
			}

			var size = new Vector2(glyph.BoundingWidth, glyph.BoundingHeight);
			var uv0 = new Vector2(glyph.TextureOffsetX, glyph.TextureOffsetY);
			var uv1 = (uv0 + size) / textureSize;
			uv0 /= textureSize;
			var x0 = Snap(KrnAxis36HorizontalOffset);
			var y0 = Snap(glyph.CurrentOffsetY);
			font.AddGlyph((ImFontConfig*)null, (ushort)codepoint, texture, x0, y0, x0 + (size.X * scale), y0 + (size.Y * scale),
				uv0.X, uv0.Y, uv1.X, uv1.Y, Snap(glyph.AdvanceWidth));
		}

		foreach (var pair in fdt.Distances)
		{
			if (pair.LeftInt <= char.MaxValue && pair.RightInt <= char.MaxValue)
			{
				font.AddKerningPair((ushort)pair.LeftInt, (ushort)pair.RightInt, Snap(pair.RightOffset));
			}
		}

		font.Ascent = Snap(header.Ascent);
		font.Descent = Snap(header.Descent);

		// Picked before the lookup table is built, which resolves the fallback glyph from it.
		var fallback = FallbackChars.FirstOrDefault(c => fdt.FindGlyph(c) != null);
		if (fallback != default)
		{
			font.FallbackChar = fallback;
		}

		var ellipsis = EllipsisChars.FirstOrDefault(c => fdt.FindGlyph(c) != null);
		if (ellipsis != default)
		{
			font.EllipsisChar = ellipsis;
		}

		tk.BuildLookupTable(font);
	}

	/// <summary>
	/// One channel of the KrnAxis textures as a white texture with the channel as alpha, kept for every build that
	/// needs it. Each game texture holds four glyph sheets, one per channel.
	/// </summary>
	private static IDalamudTextureWrap KrnChannel(int textureIndex, Dictionary<int, byte[]> pixels)
	{
		lock (_lock)
		{
			if (_krnChannels.TryGetValue(textureIndex, out var cached))
			{
				return cached;
			}

			var fileIndex = textureIndex / 4;
			var tex = ForkServices.DataManager.GetFile<TexFile>(string.Format(KrnTexturePath, fileIndex + 1))!;
			if (!pixels.TryGetValue(fileIndex, out var source))
			{
				source = tex.ImageData; // decoded to B8G8R8A8
				pixels[fileIndex] = source;
			}

			var channel = FdtReader.FontTableEntry.TextureChannelOrder[textureIndex % 4];
			var width = tex.Header.Width;
			var height = tex.Header.Height;
			var small = M3Services.TextureProvider.IsDxgiFormatSupported(DxgiFormatB4G4R4A4);
			var bytesPerPixel = small ? 2 : 4;
			var buffer = new byte[width * height * bytesPerPixel];
			for (var i = 0; i < width * height; i++)
			{
				var alpha = source[(i * 4) + channel];
				if (small)
				{
					buffer[i * 2] = 0xFF;
					buffer[(i * 2) + 1] = (byte)(alpha | 0x0F);
				}
				else
				{
					buffer[i * 4] = 0xFF;
					buffer[(i * 4) + 1] = 0xFF;
					buffer[(i * 4) + 2] = 0xFF;
					buffer[(i * 4) + 3] = alpha;
				}
			}

			var wrap = M3Services.TextureProvider.CreateFromRaw(
				new RawImageSpecification(width, height, small ? DxgiFormatB4G4R4A4 : DxgiFormatB8G8R8A8, width * bytesPerPixel),
				buffer,
				$"RebornMaterial KrnAXIS_360 [{fileIndex + 1}][{channel}]");
			_krnChannels[textureIndex] = wrap;
			return wrap;
		}
	}

	private static void DisposeKrnAxis36()
	{
		lock (_lock)
		{
			foreach (var wrap in _krnChannels.Values)
			{
				wrap.Dispose();
			}

			_krnChannels.Clear();
			_krnAxis36 = null;
			_krnAxis36Read = false;
		}
	}
}
