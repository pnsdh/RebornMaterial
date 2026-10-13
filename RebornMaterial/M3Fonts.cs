using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;

namespace RebornMaterial;

public static class M3Fonts
{
	private const int GameFontCapacity = 12;
	private const int DefaultFontCapacity = 16;

	private readonly record struct CachedFont(IFontHandle Handle, int LastUsedFrame);

	private static readonly Lock _lock = new();
	private static readonly Dictionary<int, CachedFont> _handles = [];
	private static readonly Dictionary<int, CachedFont> _defaultHandles = [];

	// Axis, the game's UI font, at a pixel size.
	public static ImFontPtr GetFont(float size)
	{
		// Round to a stable integer key to avoid excessive variants.
		var key = Math.Max(1, (int)MathF.Round(size));

		// The pixel-size constructor matters: GetRecommendedFamilyAndSize takes points, and given pixels picks a much larger font.
		return Resolve(_handles, GameFontCapacity, key, static px => M3.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(
			new GameFontStyle(GameFontFamily.Axis, px)));
	}

	// Dalamud's default font, scaled from its configured size.
	public static ImFontPtr GetDefaultFont(float scale)
	{
		var key = Math.Max(1, (int)MathF.Round(M3.PluginInterface.UiBuilder.FontDefaultSizePx * scale));

		return Resolve(_defaultHandles, DefaultFontCapacity, key, static px => M3.PluginInterface.UiBuilder.FontAtlas.NewDelegateFontHandle(
			e => e.OnPreBuild(tk => tk.AddDalamudDefaultFont(px))));
	}

	private static ImFontPtr Resolve(Dictionary<int, CachedFont> cache, int capacity, int key, Func<int, IFontHandle> create)
	{
		var frame = ImGui.GetFrameCount();
		lock (_lock)
		{
			if (!cache.TryGetValue(key, out var cached))
			{
				Evict(cache, capacity - 1, frame);
				cached = new CachedFont(create(key), frame);
			}

			cache[key] = cached with { LastUsedFrame = frame };
			if (Loaded(cached.Handle) is { } font)
			{
				return font;
			}

			// The atlas builds asynchronously; until this size is ready, the nearest loaded one stands in.
			var nearest = -1;
			ImFontPtr? fallback = null;
			foreach (var (other, candidate) in cache)
			{
				if (other == key || (fallback != null && Math.Abs(other - key) >= Math.Abs(nearest - key)))
				{
					continue;
				}

				if (Loaded(candidate.Handle) is { } ready)
				{
					nearest = other;
					fallback = ready;
				}
			}

			if (fallback is not { } nearestFont)
			{
				return ImGui.GetFont();
			}

			// Still in use, so it must not be the one evicted to make room.
			cache[nearest] = cache[nearest] with { LastUsedFrame = frame };
			return nearestFont;
		}
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

	private static void Evict(Dictionary<int, CachedFont> cache, int keep, int frame)
	{
		while (cache.Count > keep)
		{
			var oldestKey = -1;
			var oldestFrame = frame;
			foreach (var (key, cached) in cache)
			{
				if (cached.LastUsedFrame < oldestFrame)
				{
					oldestKey = key;
					oldestFrame = cached.LastUsedFrame;
				}
			}

			if (oldestKey < 0)
			{
				return;
			}

			cache[oldestKey].Handle.Dispose();
			_ = cache.Remove(oldestKey);
		}
	}

	public static void DisposeAll()
	{
		lock (_lock)
		{
			foreach (var cached in _handles.Values)
			{
				cached.Handle.Dispose();
			}

			_handles.Clear();

			foreach (var cached in _defaultHandles.Values)
			{
				cached.Handle.Dispose();
			}

			_defaultHandles.Clear();
		}
	}
}
