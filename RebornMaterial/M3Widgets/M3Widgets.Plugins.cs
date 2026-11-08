namespace RebornMaterial;

public static partial class M3Widgets
{
	public static Vector2 PluginPillSize(IReadOnlyList<M3PluginRequirement> plugins, bool any = false)
	{
		var (label, icon, _) = PluginPillState(plugins, any);
		return PillSize(label, icon);
	}

	public static void PluginPill(string id, IReadOnlyList<M3PluginRequirement> plugins, bool any = false)
	{
		var (label, icon, tone) = PluginPillState(plugins, any);
		Pill(id, label, tone, icon);
		if (ImGui.IsItemHovered())
		{
			M3Tooltip.Show(M3Plugins.Describe(plugins, any));
		}
	}

	public static bool RowPluginSwitch(string label, ref bool value, IReadOnlyList<M3PluginRequirement> plugins, string? supporting = null, bool any = false,
		bool enabled = true)
	{
		var (display, id) = SplitLabel(label);
		var pill = PluginPillSize(plugins, any);
		var switchSize = SwitchSize();
		var gap = M3.Space2;
		var controlSize = new Vector2(pill.X + gap + switchSize.X, MathF.Max(pill.Y, switchSize.Y));

		var row = M3SettingRow.Begin(display, supporting, controlSize, disabled: !enabled);
		var origin = row.ControlPosition;
		ImGui.SetCursorScreenPos(origin + new Vector2(0f, (controlSize.Y - pill.Y) * 0.5f));
		PluginPill($"##{id}_plugins", plugins, any);
		ImGui.SetCursorScreenPos(origin + new Vector2(pill.X + gap, (controlSize.Y - switchSize.Y) * 0.5f));
		var changed = Switch($"##{id}_switch", ref value, enabled);
		M3SettingRow.End(row);
		return changed;
	}

	private static (string Label, FontAwesomeIcon Icon, Vector4 Tone) PluginPillState(IReadOnlyList<M3PluginRequirement> plugins, bool any)
	{
		var ready = any ? M3Plugins.AnyLoaded(plugins) : M3Plugins.AllLoaded(plugins);
		string label;
		if (plugins.Count == 1)
		{
			label = plugins[0].Name;
		}
		else if (any)
		{
			label = "Plugin missing";
			foreach (var plugin in plugins)
			{
				if (M3Plugins.IsLoaded(plugin))
				{
					label = plugin.Name;
					break;
				}
			}
		}
		else
		{
			label = $"{plugins.Count} plugins";
		}

		return (label, ready ? FontAwesomeIcon.Check : FontAwesomeIcon.Times, M3.Severity(ready ? M3Severity.Success : M3Severity.Error));
	}
}
