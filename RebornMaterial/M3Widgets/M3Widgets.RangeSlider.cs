using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static partial class M3Widgets
{
	private static readonly Dictionary<uint, int> _rangeThumb = [];

	private static void ResetRange()
	{
		_rangeThumb.Clear();
	}

	public static bool RangeSlider(string id, ref float low, ref float high, float min, float max, float width, string format = "%.2f", bool enabled = true, float step = 0f, float minSpan = 0f, string? displayValue = null)
	{
		return RangeCore(id, ref low, ref high, min, max, width, format, false, enabled, step, minSpan, displayValue);
	}

	public static bool RangeSliderInt(string id, ref int low, ref int high, int min, int max, float width, bool enabled = true, int step = 0, int minSpan = 0, string? displayValue = null)
	{
		float lowValue = low;
		float highValue = high;
		if (!RangeCore(id, ref lowValue, ref highValue, min, max, width, string.Empty, true, enabled, step, minSpan, displayValue))
		{
			return false;
		}

		low = (int)lowValue;
		high = (int)highValue;
		return true;
	}

	public static bool RowRange(string label, ref float low, ref float high, float min, float max, string format, string? supporting = null, bool enabled = true, float step = 0f, float minSpan = 0f)
	{
		var (display, id) = RowParts(label, "_range");
		var trackWidth = 150f * M3.Scale;
		var controlSize = new Vector2(trackWidth + SliderGutter(format, min, max, true), ButtonHeight);

		var row = M3SettingRow.Begin(display, supporting, controlSize, disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = RangeCore(id, ref low, ref high, min, max, trackWidth, format, false, enabled, step, minSpan, null);
		M3SettingRow.End(row);
		return changed;
	}

	public static bool RowRangeInt(string label, ref int low, ref int high, int min, int max, string? supporting = null, bool enabled = true, int step = 0, int minSpan = 0)
	{
		var (display, id) = RowParts(label, "_range");
		var trackWidth = 150f * M3.Scale;
		var gutter = MathF.Max(ImGui.CalcTextSize($"{min} - {min}").X, ImGui.CalcTextSize($"{max} - {max}").X) + (14f * M3.Scale);
		var controlSize = new Vector2(trackWidth + gutter, ButtonHeight);

		var row = M3SettingRow.Begin(display, supporting, controlSize, disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = RangeSliderInt(id, ref low, ref high, min, max, trackWidth, enabled, step, minSpan);
		M3SettingRow.End(row);
		return changed;
	}

	private static string RangeReadout(string format, float low, float high)
	{
		return string.Concat(FormatValue(format, low), " - ", FormatValue(format, high));
	}

	private static float RangeHandleX(float value, float min, float range, float trackLeft, float trackRight)
	{
		return float.Lerp(trackLeft, trackRight, range > 0f ? Math.Clamp((value - min) / range, 0f, 1f) : 0f);
	}

	private static int NearestThumb(float mouseX, float lowX, float highX, bool lowAtMin)
	{
		var toLow = MathF.Abs(mouseX - lowX);
		var toHigh = MathF.Abs(mouseX - highX);
		if (toLow != toHigh)
		{
			return toLow < toHigh ? 0 : 1;
		}

		return mouseX > highX || (mouseX == highX && lowAtMin) ? 1 : 0;
	}

	private static bool RangeCore(string id, ref float low, ref float high, float min, float max, float width, string format, bool integer, bool enabled, float step, float minSpan, string? displayValue)
	{
		var scale = M3.Scale;
		var s = M3.Scheme;
		var height = ImGui.GetTextLineHeight() + (20f * scale);
		if (max < min)
		{
			(min, max) = (max, min);
		}

		minSpan = MathF.Max(minSpan, 0f);
		var lo = Math.Clamp(MathF.Min(low, high), min, max);
		var hi = Math.Clamp(MathF.Max(low, high), min, max);

		using (ImRaii.Disabled(!enabled))
		{
			_ = ImGui.InvisibleButton(id, new Vector2(MathF.Max(width, 1f), height));
		}

		var key = ImGuiP.GetItemID();
		var itemMin = ImGui.GetItemRectMin();
		var itemMax = ImGui.GetItemRectMax();
		var centerY = (itemMin.Y + itemMax.Y) * 0.5f;
		var trackHeight = 4f * scale;
		var handleRadius = 10f * scale;
		var trackLeft = itemMin.X + handleRadius;
		var trackRight = itemMax.X - handleRadius;
		var range = max - min;
		var mouseX = ImGui.GetIO().MousePos.X;

		var lowX = RangeHandleX(lo, min, range, trackLeft, trackRight);
		var highX = RangeHandleX(hi, min, range, trackLeft, trackRight);

		if (enabled && ImGui.IsItemActivated())
		{
			_rangeThumb[key] = NearestThumb(mouseX, lowX, highX, lo <= min);
		}

		var active = enabled && ImGui.IsItemActive();
		var activeThumb = !active ? -1
			: _rangeThumb.TryGetValue(key, out var stored) ? stored
			: NearestThumb(mouseX, lowX, highX, lo <= min);
		var changed = false;

		if (active && ImGui.IsMouseDown(ImGuiMouseButton.Left))
		{
			var fraction = Math.Clamp((mouseX - trackLeft) / MathF.Max(trackRight - trackLeft, 1f), 0f, 1f);
			var v = min + (fraction * range);
			if (integer)
			{
				v = step > 1f ? Snap((long)MathF.Round(v), (int)min, (int)max, (int)step) : MathF.Round(v);
			}
			else if (step > 0f)
			{
				v = Snap(v, min, max, step);
			}

			if (activeThumb == 0)
			{
				v = MathF.Max(MathF.Min(v, hi - minSpan), min);
				if (v != lo)
				{
					lo = v;
					changed = true;
				}
			}
			else
			{
				v = MathF.Min(MathF.Max(v, lo + minSpan), max);
				if (v != hi)
				{
					hi = v;
					changed = true;
				}
			}

			if (changed)
			{
				low = lo;
				high = hi;
				lowX = RangeHandleX(lo, min, range, trackLeft, trackRight);
				highX = RangeHandleX(hi, min, range, trackLeft, trackRight);
			}
		}

		if (ImGui.IsItemDeactivated())
		{
			_ = _rangeThumb.Remove(key);
		}

		var hoveredThumb = active ? activeThumb
			: enabled && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem) ? NearestThumb(mouseX, lowX, highX, lo <= min)
			: -1;

		var drawList = ImGui.GetWindowDrawList();
		var accent = enabled ? s.Primary : M3.Alpha(s.OnSurface, M3.DisabledContent);
		var accentColor = M3.U32(accent);
		var dotColor = M3.U32(enabled ? s.OnPrimary : s.Surface, 0.9f);

		drawList.AddRectFilled(
			new Vector2(trackLeft, centerY - (trackHeight * 0.5f)),
			new Vector2(trackRight, centerY + (trackHeight * 0.5f)),
			enabled ? M3.U32(s.SurfaceContainerHighest) : M3.U32(s.OnSurface, M3.DisabledContainer), trackHeight);

		if (highX > lowX)
		{
			drawList.AddRectFilled(
				new Vector2(lowX, centerY - (trackHeight * 0.5f)),
				new Vector2(highX, centerY + (trackHeight * 0.5f)),
				accentColor, trackHeight);
		}

		var topThumb = hoveredThumb >= 0 ? hoveredThumb : 1;
		var bottomCenter = new Vector2(topThumb == 0 ? highX : lowX, centerY);
		var topCenter = new Vector2(topThumb == 0 ? lowX : highX, centerY);

		drawList.AddCircleFilled(bottomCenter, handleRadius, accentColor, 24);
		drawList.AddCircleFilled(bottomCenter, handleRadius * 0.45f, dotColor, 16);

		if (hoveredThumb >= 0)
		{
			drawList.AddCircleFilled(topCenter, handleRadius + (8f * scale),
				M3.U32(s.Primary, active ? M3.StatePressed : M3.StateHover), 24);
		}

		drawList.AddCircleFilled(topCenter, handleRadius, accentColor, 24);
		drawList.AddCircleFilled(topCenter, handleRadius * 0.45f, dotColor, 16);

		M3Draw.FocusRing(itemMin, itemMax, height * 0.5f);

		var readoutColor = M3.U32(s.OnSurfaceVariant, enabled ? 1f : M3.DisabledContent);
		var readoutX = itemMax.X + (10f * scale);
		if (displayValue != null)
		{
			var size = ImGui.CalcTextSize(displayValue);
			drawList.AddText(new Vector2(readoutX, centerY - (size.Y * 0.5f)), readoutColor, displayValue);
		}
		else if (integer)
		{
			var size = ImGui.CalcTextSize($"{(int)lo} - {(int)hi}");
			drawList.AddText(new Vector2(readoutX, centerY - (size.Y * 0.5f)), readoutColor, $"{(int)lo} - {(int)hi}");
		}
		else
		{
			var text = SliderReadout(key, format, lo, hi, true);
			var size = ImGui.CalcTextSize(text);
			drawList.AddText(new Vector2(readoutX, centerY - (size.Y * 0.5f)), readoutColor, text);
		}

		if (active)
		{
			var value = activeThumb == 0 ? lo : hi;
			var bubble = integer ? null : FormatValue(format, value);
			var textSize = bubble != null ? ImGui.CalcTextSize(bubble) : ImGui.CalcTextSize($"{(int)value}");
			var padding = new Vector2(8f, 4f) * scale;
			var bubbleMin = new Vector2(topCenter.X - (textSize.X * 0.5f) - padding.X, itemMin.Y - textSize.Y - (padding.Y * 2f) - (6f * scale));
			var bubbleMax = bubbleMin + textSize + (padding * 2f);
			drawList.AddRectFilled(bubbleMin, bubbleMax, M3.U32(s.InverseSurface), M3.ShapeSmall);
			if (bubble != null)
			{
				drawList.AddText(bubbleMin + padding, M3.U32(s.InverseOnSurface), bubble);
			}
			else
			{
				drawList.AddText(bubbleMin + padding, M3.U32(s.InverseOnSurface), $"{(int)value}");
			}
		}

		return changed;
	}
}
