using System.Text;

namespace RebornMaterial;

public readonly record struct M3PluginRequirement(string InternalName, string? DisplayName = null, Version? MinVersion = null)
{
	public string Name => DisplayName ?? InternalName;
}

public static class M3Plugins
{
	private const long SnapshotLifetimeMilliseconds = 1000;
	private static Dictionary<string, Version?>? _loaded;
	private static long _loadedAt;

	public static bool IsLoaded(in M3PluginRequirement plugin)
	{
		return Loaded().TryGetValue(plugin.InternalName, out var version) && (plugin.MinVersion == null || version >= plugin.MinVersion);
	}

	public static bool AllLoaded(IReadOnlyList<M3PluginRequirement> plugins)
	{
		for (var i = 0; i < plugins.Count; i++)
		{
			if (!IsLoaded(plugins[i]))
			{
				return false;
			}
		}

		return true;
	}

	public static bool AnyLoaded(IReadOnlyList<M3PluginRequirement> plugins)
	{
		for (var i = 0; i < plugins.Count; i++)
		{
			if (IsLoaded(plugins[i]))
			{
				return true;
			}
		}

		return false;
	}

	public static string Describe(IReadOnlyList<M3PluginRequirement> plugins, bool any = false)
	{
		var ready = any ? AnyLoaded(plugins) : AllLoaded(plugins);
		var text = new StringBuilder(any
			? ready ? "Needs one of these plugins, and one is ready:" : "Needs one of these plugins installed and enabled:"
			: ready ? "Everything this needs is installed and enabled:" : "Needs these plugins installed and enabled:");

		var loaded = Loaded();
		for (var i = 0; i < plugins.Count; i++)
		{
			var plugin = plugins[i];
			var state = !loaded.TryGetValue(plugin.InternalName, out var version) ? "not installed or disabled"
				: plugin.MinVersion != null && version < plugin.MinVersion ? "outdated"
				: "ready";

			text.Append("\n• ").Append(plugin.Name);
			if (plugin.MinVersion != null)
			{
				text.Append(' ').Append(plugin.MinVersion).Append('+');
			}

			text.Append(": ").Append(state);
		}

		return text.ToString();
	}

	internal static void Invalidate()
	{
		_loaded = null;
	}

	private static Dictionary<string, Version?> Loaded()
	{
		var loaded = _loaded;
		var now = Environment.TickCount64;
		if (loaded != null && now - _loadedAt < SnapshotLifetimeMilliseconds)
		{
			return loaded;
		}

		loaded = new Dictionary<string, Version?>(StringComparer.Ordinal);
		foreach (var installed in M3.PluginInterface.InstalledPlugins)
		{
			if (!installed.IsLoaded)
			{
				continue;
			}

			var version = installed.Version;
			if (!loaded.TryGetValue(installed.InternalName, out var existing) || version > existing)
			{
				loaded[installed.InternalName] = version;
			}
		}

		_loadedAt = now;
		_loaded = loaded;
		return loaded;
	}
}
