using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static partial class M3Widgets
{
	private static readonly Dictionary<uint, (int Count, float FontSize, int Frame, float Widest)> _rowComboWidths = [];

	private static void ResetRows()
	{
		_rowComboWidths.Clear();
	}

	public static bool RowSwitch(string label, ref bool value, string? supporting = null, bool enabled = true)
	{
		return RowSwitch(label, ref value, out _, supporting, enabled);
	}

	public static bool RowSwitch(string label, ref bool value, out bool hovered, string? supporting = null, bool enabled = true)
	{
		var (display, id) = SplitLabel(label);
		var row = M3SettingRow.Begin(display, supporting, SwitchSize(), disabled: !enabled);
		hovered = row.Hovered;
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = Switch($"##{id}_switch", ref value, enabled);
		M3SettingRow.End(row);
		return changed;
	}

	public static bool RowDragFloat(string label, ref float value, float min, float max, string format, string? supporting = null, bool logarithmic = false, bool enabled = true, float step = 0f)
	{
		var (display, id) = SplitLabel(label);
		var trackWidth = 150f * M3.Scale;
		var gutter = MathF.Max(SliderValueGutter(FormatValue(format, min)), SliderValueGutter(FormatValue(format, max)));
		var controlSize = new Vector2(trackWidth + gutter, ButtonHeight);

		var row = M3SettingRow.Begin(display, supporting, controlSize, disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = SliderCore($"##{id}_slider", ref value, min, max, null, format, trackWidth, logarithmic, enabled, step);
		M3SettingRow.End(row);
		return changed;
	}

	public static bool RowDragInt(string label, ref int value, int min, int max, string? supporting = null, bool logarithmic = false, bool enabled = true, int step = 0)
	{
		var (display, id) = SplitLabel(label);
		var trackWidth = 150f * M3.Scale;
		var gutter = MathF.Max(SliderValueGutter(min.ToString()), SliderValueGutter(max.ToString()));
		var controlSize = new Vector2(trackWidth + gutter, ButtonHeight);

		var row = M3SettingRow.Begin(display, supporting, controlSize, disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = SliderIntCore($"##{id}_slider", ref value, min, max, null, trackWidth, logarithmic, enabled, step);
		M3SettingRow.End(row);
		return changed;
	}

	private static float RowComboWidth(float widestLabel)
	{
		var width = MathF.Max(160f * M3.Scale, MathF.Ceiling(widestLabel) + (ComboTextInset * 2f) + ComboChevronWidth + 1f);
		return MathF.Min(width, MathF.Max(120f * M3.Scale, M3SettingRow.MaxControlWidth() * 0.6f));
	}

	private static float WidestLabel<TState>(string comboId, int count, TState state, Func<TState, int, string> itemLabel)
	{
		var key = ImGui.GetID(comboId);
		var fontSize = ImGui.GetFontSize();
		var frame = ImGui.GetFrameCount();
		if (_rowComboWidths.TryGetValue(key, out var cached) && cached.Count == count && cached.FontSize == fontSize && frame - cached.Frame < 60)
		{
			return cached.Widest;
		}

		var widest = 0f;
		for (var i = 0; i < count; i++)
		{
			widest = MathF.Max(widest, ImGui.CalcTextSize(itemLabel(state, i)).X);
		}

		_rowComboWidths[key] = (count, fontSize, frame, widest);
		return widest;
	}

	public static bool RowCombo(string label, ref int index, IReadOnlyList<string> items, string? supporting = null, bool search = false, bool enabled = true)
	{
		var (display, id) = SplitLabel(label);
		var comboId = $"##{id}_combo";
		var width = RowComboWidth(WidestLabel(comboId, items.Count, items, static (list, i) => list[i]));
		var row = M3SettingRow.Begin(display, supporting, new Vector2(width, ComboHeight), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = Combo(comboId, ref index, items, width, search: search, enabled: enabled);
		M3SettingRow.End(row);
		return changed;
	}

	public static bool RowCombo(string label, ref int index, int count, Func<int, string> itemLabel, string? supporting = null, bool search = false, bool enabled = true)
	{
		var (display, id) = SplitLabel(label);
		var comboId = $"##{id}_combo";
		var width = RowComboWidth(WidestLabel(comboId, count, itemLabel, static (labels, i) => labels(i)));
		var row = M3SettingRow.Begin(display, supporting, new Vector2(width, ComboHeight), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = Combo(comboId, ref index, count, itemLabel, width, search: search, enabled: enabled);
		M3SettingRow.End(row);
		return changed;
	}

	public static bool RowCombo<T>(string label, ref T value, string? supporting = null, Func<T, string>? itemLabel = null, bool search = false, bool enabled = true)
		where T : struct, Enum
	{
		var (display, id) = SplitLabel(label);
		var comboId = $"##{id}_combo";
		var count = EnumValues<T>.All.Length;
		var widest = itemLabel == null
			? WidestLabel(comboId, count, EnumValues<T>.Labels, static (labels, i) => labels[i])
			: WidestLabel(comboId, count, itemLabel, static (labels, i) => labels(EnumValues<T>.All[i]));
		var width = RowComboWidth(widest);
		var row = M3SettingRow.Begin(display, supporting, new Vector2(width, ComboHeight), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = Combo(comboId, ref value, width, itemLabel, search, enabled);
		M3SettingRow.End(row);
		return changed;
	}

	public static bool RowCombo(string label, Type enumType, ref Enum value, string? supporting = null, Func<Enum, string>? itemLabel = null, bool search = false, bool enabled = true)
	{
		var values = EnumValuesOf(enumType);
		var (display, id) = SplitLabel(label);
		var comboId = $"##{id}_combo";
		var width = RowComboWidth(WidestLabel(comboId, values.Length, (values, itemLabel),
			static (state, i) => state.itemLabel?.Invoke(state.values[i]) ?? EnumLabel(state.values[i])));
		var row = M3SettingRow.Begin(display, supporting, new Vector2(width, ComboHeight), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = Combo(comboId, enumType, ref value, width, itemLabel, search, enabled);
		M3SettingRow.End(row);
		return changed;
	}

	public static bool RowSegmented(string label, ref int index, ReadOnlySpan<M3Segment> segments, string? supporting = null, bool enabled = true)
	{
		var (display, id) = SplitLabel(label);
		var width = MathF.Min(SegmentedWidth(segments), M3SettingRow.MaxControlWidth());

		var row = M3SettingRow.Begin(display, supporting, new Vector2(width, SegmentedHeight), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var clicked = SegmentedButtons($"##{id}_segments", segments, index, width, enabled: enabled);
		M3SettingRow.End(row);

		if (clicked < 0)
		{
			return false;
		}

		index = clicked;
		return true;
	}

	public static bool RowColor(string label, ref Vector4 color, Vector4? defaultColor = null, string? supporting = null, bool alpha = true, bool enabled = true)
	{
		var (display, id) = SplitLabel(label);
		var scale = M3.Scale;
		var swatch = ColorSwatchSize;
		var canReset = defaultColor is { } fallback && fallback != color;
		var resetSize = 32f * scale;
		var controlSize = new Vector2(swatch + (canReset ? M3.Space1 + resetSize : 0f), MathF.Max(swatch, resetSize));

		var row = M3SettingRow.Begin(display, supporting, controlSize, disabled: !enabled);
		var origin = row.ControlPosition;
		ImGui.SetCursorScreenPos(origin + new Vector2(0f, (controlSize.Y - swatch) * 0.5f));
		var changed = ColorSwatch($"##{id}_color", ref color, defaultColor, alpha, enabled);

		if (canReset)
		{
			ImGui.SetCursorScreenPos(origin + new Vector2(swatch + M3.Space1, (controlSize.Y - resetSize) * 0.5f));
			if (IconButton($"##{id}_reset", FontAwesomeIcon.Undo, M3Fork.T("Reset to default"), diameter: resetSize, enabled: enabled))
			{
				color = defaultColor!.Value;
				changed = true;
			}
		}

		M3SettingRow.End(row);
		return changed;
	}

	public static bool RowText(string label, ref string text, string? hint = null, string? supporting = null, int maxLength = 256, bool enabled = true)
	{
		var (display, id) = SplitLabel(label);
		var width = MathF.Min(240f * M3.Scale, M3SettingRow.MaxControlWidth() * 0.6f);

		var row = M3SettingRow.Begin(display, supporting, new Vector2(width, M3TextField.Height), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		bool changed;
		using (ImRaii.Disabled(!enabled))
		{
			changed = M3TextField.Draw($"##{id}_text", hint ?? string.Empty, ref text, width, maxLength);
		}

		M3SettingRow.End(row);
		return changed;
	}
}
