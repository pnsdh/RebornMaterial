using Dalamud.Interface.GameFonts;
using Dalamud.Interface.ManagedFontAtlas;

namespace RebornMaterial;

public static class M3Fonts
{
	private const int GameFontCapacity = 12;
	private const int DefaultFontCapacity = 16;

	private sealed class CachedFont(IFontHandle handle)
	{
		public IFontHandle Handle { get; } = handle;
		public int LastUsedFrame { get; set; }
		public int ResolvedFrame { get; set; } = -1;
		public ImFontPtr Resolved { get; set; }
	}

	private static readonly Lock _lock = new();
	private static readonly Dictionary<int, CachedFont> _handles = [];
	private static readonly Dictionary<int, CachedFont> _defaultHandles = [];

	public static ImFontPtr GetFont(float size)
	{
		var key = Math.Max(1, (int)MathF.Round(size));

		return Resolve(_handles, GameFontCapacity, key, static px => M3.PluginInterface.UiBuilder.FontAtlas.NewGameFontHandle(
			new GameFontStyle(GameFontFamily.Axis, px)));
	}

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
				cached = new CachedFont(create(key));
				cache[key] = cached;
			}

			cached.LastUsedFrame = frame;
			if (cached.ResolvedFrame == frame)
			{
				return cached.Resolved;
			}

			var font = Loaded(cached.Handle) ?? Nearest(cache, key, frame);
			if (font is not { } found)
			{
				return ImGui.GetFont();
			}

			cached.Resolved = found;
			cached.ResolvedFrame = frame;
			return found;
		}
	}

	private static ImFontPtr? Nearest(Dictionary<int, CachedFont> cache, int key, int frame)
	{
		CachedFont? nearest = null;
		var nearestKey = -1;
		ImFontPtr? fallback = null;
		foreach (var (other, candidate) in cache)
		{
			if (other == key || (fallback != null && Math.Abs(other - key) >= Math.Abs(nearestKey - key)))
			{
				continue;
			}

			if (Loaded(candidate.Handle) is { } ready)
			{
				nearest = candidate;
				nearestKey = other;
				fallback = ready;
			}
		}

		nearest?.LastUsedFrame = frame;

		return fallback;
	}

	private static ImFontPtr? Loaded(IFontHandle handle)
	{
		if (!handle.Available)
		{
			return null;
		}

		try
		{
			using var pushed = handle.Push();
			var font = ImGui.GetFont();
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
