using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static partial class M3Widgets
{
	public static float NumberFieldHeight => M3.FitText(40f, 8f);

	private static float StepperDiameter => NumberFieldHeight - (8f * M3.Scale);

	public static float NumberFieldWidth(bool steppers = false, string? unit = null)
	{
		var scale = M3.Scale;
		var width = 96f * scale;
		if (steppers)
		{
			width += (StepperDiameter * 2f) + (6f * scale);
		}

		if (!string.IsNullOrEmpty(unit))
		{
			width += ImGui.CalcTextSize(unit).X + (6f * scale);
		}

		return width;
	}

	public static bool NumberField(string id, ref float value, float width, float step = 0f, float min = float.MinValue, float max = float.MaxValue, string format = "%.2f", string? unit = null, bool enabled = true)
	{
		using var idScope = ImRaii.PushId(id);
		var layout = BeginNumberField(width, step > 0f, unit);

		bool changed;
		using (ImRaii.Disabled(!enabled))
		using (PushClearFrame())
		{
			changed = ImGui.InputFloat("##value", ref value, 0f, 0f, format);
		}

		var direction = EndNumberField(layout, width, step > 0f, unit, enabled);
		if (direction != 0)
		{
			value += direction * step;
			changed = true;
		}

		if (changed)
		{
			value = Math.Clamp(value, min, max);
		}

		return changed;
	}

	public static bool NumberField(string id, ref int value, float width, int step = 0, int min = int.MinValue, int max = int.MaxValue, string? unit = null, bool enabled = true)
	{
		using var idScope = ImRaii.PushId(id);
		var layout = BeginNumberField(width, step > 0, unit);

		bool changed;
		using (ImRaii.Disabled(!enabled))
		using (PushClearFrame())
		{
			changed = ImGui.InputInt("##value", ref value, 0, 0);
		}

		var direction = EndNumberField(layout, width, step > 0, unit, enabled);
		if (direction != 0)
		{
			value = (int)Math.Clamp((long)value + ((long)direction * step), int.MinValue, int.MaxValue);
			changed = true;
		}

		if (changed)
		{
			value = Math.Clamp(value, min, max);
		}

		return changed;
	}

	public static bool RowNumber(string label, ref float value, float step = 0f, float min = float.MinValue, float max = float.MaxValue, string format = "%.2f", string? unit = null, string? supporting = null, bool enabled = true)
	{
		var (display, id) = SplitLabel(label);
		var width = NumberFieldWidth(step > 0f, unit);
		var row = M3SettingRow.Begin(display, supporting, new Vector2(width, NumberFieldHeight), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = NumberField($"##{id}_number", ref value, width, step, min, max, format, unit, enabled);
		M3SettingRow.End(row);
		return changed;
	}

	public static bool RowNumber(string label, ref int value, int step = 1, int min = int.MinValue, int max = int.MaxValue, string? unit = null, string? supporting = null, bool enabled = true)
	{
		var (display, id) = SplitLabel(label);
		var width = NumberFieldWidth(step > 0, unit);
		var row = M3SettingRow.Begin(display, supporting, new Vector2(width, NumberFieldHeight), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = NumberField($"##{id}_number", ref value, width, step, min, max, unit, enabled);
		M3SettingRow.End(row);
		return changed;
	}

	private readonly record struct NumberLayout(Vector2 Min, Vector2 Max, float UnitX);

	private static NumberLayout BeginNumberField(float width, bool steppers, string? unit)
	{
		var scale = M3.Scale;
		var height = NumberFieldHeight;
		var framePadding = ImGui.GetStyle().FramePadding.X;
		var min = ImGui.GetCursorScreenPos();
		var max = min + new Vector2(width, height);

		var stepperRoom = steppers ? (StepperDiameter * 2f) + (6f * scale) : 0f;
		var unitWidth = string.IsNullOrEmpty(unit) ? 0f : ImGui.CalcTextSize(unit).X + (6f * scale);
		var inputLeft = min.X + (12f * scale) - framePadding;
		var inputRight = max.X - (8f * scale) - stepperRoom - unitWidth + framePadding;

		ImGui.SetCursorScreenPos(new Vector2(inputLeft, min.Y + ((height - ImGui.GetFrameHeight()) * 0.5f)));
		ImGui.SetNextItemWidth(MathF.Max(24f * scale, inputRight - inputLeft));
		return new NumberLayout(min, max, inputRight - framePadding + (6f * scale));
	}

	private static int EndNumberField(NumberLayout layout, float width, bool steppers, string? unit, bool enabled)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var (min, max, unitX) = layout;
		var height = max.Y - min.Y;
		var focused = ImGui.IsItemActive();
		var hovered = enabled && M3Draw.MouseOver(min, max);
		var drawList = ImGui.GetWindowDrawList();
		var dim = enabled ? 1f : M3.DisabledContent;

		var outline = focused ? s.Primary : hovered ? s.OnSurface : s.Outline;
		drawList.AddRect(min, max, M3.U32(outline, dim), M3.ShapeExtraSmall, ImDrawFlags.None, (focused ? 2f : 1f) * scale);

		if (!string.IsNullOrEmpty(unit))
		{
			var unitSize = ImGui.CalcTextSize(unit);
			drawList.AddText(new Vector2(unitX, min.Y + ((height - unitSize.Y) * 0.5f)), M3.U32(s.OnSurfaceVariant, 0.9f * dim), unit);
		}

		var direction = 0;
		if (steppers)
		{
			var diameter = StepperDiameter;
			var top = min.Y + ((height - diameter) * 0.5f);
			var right = max.X - (4f * scale);

			ImGui.PushButtonRepeat(true);
			ImGui.SetCursorScreenPos(new Vector2(right - (diameter * 2f) - (2f * scale), top));
			if (IconButton("##less", FontAwesomeIcon.Minus, null, diameter: diameter, enabled: enabled))
			{
				direction = -1;
			}

			ImGui.SetCursorScreenPos(new Vector2(right - diameter, top));
			if (IconButton("##more", FontAwesomeIcon.Plus, null, diameter: diameter, enabled: enabled))
			{
				direction = 1;
			}

			ImGui.PopButtonRepeat();
		}

		ImGui.SetCursorScreenPos(min);
		ImGui.Dummy(new Vector2(width, height));
		return direction;
	}
}
