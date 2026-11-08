namespace RebornMaterial;

public readonly record struct M3PluginRequirement(string InternalName, string? DisplayName = null, Version? MinVersion = null)
{
	public string Name => DisplayName ?? InternalName;
}

public static class M3Plugins
{
	public static bool IsLoaded(in M3PluginRequirement plugin)
	{
		var internalName = plugin.InternalName;
		var minimum = plugin.MinVersion;
		foreach (var installed in M3.PluginInterface.InstalledPlugins)
		{
			if (installed.IsLoaded && installed.InternalName == internalName && (minimum == null || installed.Version >= minimum))
			{
				return true;
			}
		}

		return false;
	}

	public static bool AllLoaded(IReadOnlyList<M3PluginRequirement> plugins)
	{
		foreach (var plugin in plugins)
		{
			if (!IsLoaded(plugin))
			{
				return false;
			}
		}

		return true;
	}

	public static bool AnyLoaded(IReadOnlyList<M3PluginRequirement> plugins)
	{
		foreach (var plugin in plugins)
		{
			if (IsLoaded(plugin))
			{
				return true;
			}
		}

		return false;
	}

	public static string Describe(IReadOnlyList<M3PluginRequirement> plugins, bool any = false)
	{
		var ready = any ? AnyLoaded(plugins) : AllLoaded(plugins);
		var lines = new List<string>(plugins.Count + 1)
		{
			any ? ready ? "Needs one of these plugins, and one is ready:" : "Needs one of these plugins installed and enabled:"
				: ready ? "Everything this needs is installed and enabled:" : "Needs these plugins installed and enabled:",
		};

		foreach (var plugin in plugins)
		{
			var state = "not installed or disabled";
			foreach (var installed in M3.PluginInterface.InstalledPlugins)
			{
				if (!installed.IsLoaded || installed.InternalName != plugin.InternalName)
				{
					continue;
				}

				state = plugin.MinVersion != null && installed.Version < plugin.MinVersion ? "outdated" : "ready";
				break;
			}

			var version = plugin.MinVersion == null ? string.Empty : $" {plugin.MinVersion}+";
			lines.Add($"• {plugin.Name}{version}: {state}");
		}

		return string.Join("\n", lines);
	}
}
