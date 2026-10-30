using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static partial class M3Widgets
{
	private static ImRaii.ColorDisposable PushInvisibleSliderChrome(bool condition)
	{
		return ImRaii.PushColor(ImGuiCol.FrameBg, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.FrameBgHovered, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.FrameBgActive, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.SliderGrab, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.SliderGrabActive, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.Text, new Vector4(0f, 0f, 0f, 0f), condition);
	}

	public static bool Slider(string id, ref float value, float min, float max, string displayValue, float width, bool logarithmic = false, bool enabled = true, float step = 0f)
	{
		return SliderCore(id, ref value, min, max, displayValue, null, width, logarithmic, enabled, step);
	}

	public static bool SliderInt(string id, ref int value, int min, int max, string displayValue, float width, bool logarithmic = false, bool enabled = true, int step = 0)
	{
		return SliderIntCore(id, ref value, min, max, displayValue, width, logarithmic, enabled, step);
	}

	private static bool SliderCore(string id, ref float value, float min, float max, string? displayValue, string? format, float width, bool logarithmic, bool enabled, float step)
	{
		var scale = M3.Scale;
		var editing = ImGuiP.TempInputIsActive(ImGui.GetID(id));
		var flags = ImGuiSliderFlags.AlwaysClamp | ImGuiSliderFlags.NoRoundToFormat | (logarithmic ? ImGuiSliderFlags.Logarithmic : ImGuiSliderFlags.None);
		var before = value;
		bool changed;

		using (ImRaii.Disabled(!enabled))
		using (PushInvisibleSliderChrome(!editing))
		using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(editing ? 8f : 0f, 10f) * scale))
		{
			ImGui.SetNextItemWidth(width);
			changed = ImGui.SliderFloat(id, ref value, min, max, "%.2f", flags);
		}

		if (changed && step > 0f)
		{
			var raw = value;
			value = Snap(raw, min, max, step);

			if (value == before && raw != before && !editing && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
			{
				value = Snap(before + MathF.CopySign(step, raw - before), min, max, step);
			}

			changed = value != before;
		}

		DrawSliderVisual(value, min, max, displayValue ?? FormatValue(format ?? string.Empty, value), M3.Scheme, scale, editing, logarithmic ? 0.01f : 0f, enabled);
		return changed;
	}

	private static bool SliderIntCore(string id, ref int value, int min, int max, string? displayValue, float width, bool logarithmic, bool enabled, int step)
	{
		var scale = M3.Scale;
		var editing = ImGuiP.TempInputIsActive(ImGui.GetID(id));
		var flags = ImGuiSliderFlags.AlwaysClamp | (logarithmic ? ImGuiSliderFlags.Logarithmic : ImGuiSliderFlags.None);
		var before = value;
		bool changed;

		using (ImRaii.Disabled(!enabled))
		using (PushInvisibleSliderChrome(!editing))
		using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(editing ? 8f : 0f, 10f) * scale))
		{
			ImGui.SetNextItemWidth(width);
			changed = ImGui.SliderInt(id, ref value, min, max, "%d", flags);
		}

		if (changed && step > 1)
		{
			var raw = value;
			value = Snap(raw, min, max, step);
			if (value == before && raw != before && !editing && !ImGui.IsMouseDown(ImGuiMouseButton.Left))
			{
				value = Snap(before + (raw > before ? (long)step : -step), min, max, step);
			}

			changed = value != before;
		}

		DrawSliderVisual(value, min, max, displayValue ?? value.ToString(), M3.Scheme, scale, editing, logarithmic ? 0.1f : 0f, enabled);
		return changed;
	}

	private static float Snap(float value, float min, float max, float step)
	{
		return Math.Clamp(min + (MathF.Round((value - min) / step) * step), MathF.Min(min, max), MathF.Max(min, max));
	}

	private static int Snap(long value, int min, int max, int step)
	{
		var snapped = min + ((long)Math.Round((value - (double)min) / step) * step);
		return (int)Math.Clamp(snapped, Math.Min(min, max), Math.Max(min, max));
	}

	private static float LogFraction(float value, float min, float max, float epsilon, float usableWidth)
	{
		if (min == max)
		{
			return 0f;
		}

		var flipped = max < min;
		if (flipped)
		{
			(min, max) = (max, min);
		}

		var clamped = Math.Clamp(value, min, max);
		var minFudged = MathF.Abs(min) < epsilon ? (min < 0f ? -epsilon : epsilon) : min;
		var maxFudged = MathF.Abs(max) < epsilon ? (max < 0f ? -epsilon : epsilon) : max;
		if (min == 0f && max < 0f)
		{
			minFudged = -epsilon;
		}
		else if (max == 0f && min < 0f)
		{
			maxFudged = -epsilon;
		}

		float result;
		if (clamped <= minFudged)
		{
			result = 0f;
		}
		else if (clamped >= maxFudged)
		{
			result = 1f;
		}
		else if (min * max < 0f)
		{
			var zero = -min / (max - min);
			var deadzone = ImGui.GetStyle().LogSliderDeadzone * 0.5f / MathF.Max(usableWidth, 1f);
			var left = zero - deadzone;
			var right = zero + deadzone;
			result = clamped == 0f ? zero
				: clamped < 0f ? (1f - (MathF.Log(-clamped / epsilon) / MathF.Log(-minFudged / epsilon))) * left
				: right + (MathF.Log(clamped / epsilon) / MathF.Log(maxFudged / epsilon) * (1f - right));
		}
		else if (min < 0f || max < 0f)
		{
			result = 1f - (MathF.Log(-clamped / -maxFudged) / MathF.Log(-minFudged / -maxFudged));
		}
		else
		{
			result = MathF.Log(clamped / minFudged) / MathF.Log(maxFudged / minFudged);
		}

		return flipped ? 1f - result : result;
	}

	private static void DrawSliderVisual(float value, float min, float max, string displayValue, M3Scheme s, float scale, bool editing, float logEpsilon, bool enabled)
	{
		var itemMin = ImGui.GetItemRectMin();
		var itemMax = ImGui.GetItemRectMax();
		var centerY = (itemMin.Y + itemMax.Y) * 0.5f;
		var drawList = ImGui.GetWindowDrawList();

		var hasReadout = !string.IsNullOrEmpty(displayValue);
		var textSize = hasReadout ? ImGui.CalcTextSize(displayValue) : Vector2.Zero;
		if (hasReadout)
		{
			drawList.AddText(new Vector2(itemMax.X + (10f * scale), centerY - (textSize.Y * 0.5f)),
				M3.U32(s.OnSurfaceVariant, enabled ? 1f : M3.DisabledContent), displayValue);
		}

		if (editing)
		{
			return;
		}

		var hovered = enabled && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var active = enabled && ImGui.IsItemActive();

		var trackHeight = 4f * scale;
		var handleRadius = 10f * scale;
		var trackLeft = itemMin.X + handleRadius;
		var trackRight = itemMax.X - handleRadius;
		var fraction = logEpsilon > 0f
			? LogFraction(value, min, max, logEpsilon, trackRight - trackLeft)
			: max > min ? Math.Clamp((value - min) / (max - min), 0f, 1f) : 0f;
		var handleX = float.Lerp(trackLeft, trackRight, fraction);
		var accent = enabled ? s.Primary : M3.Alpha(s.OnSurface, M3.DisabledContent);

		drawList.AddRectFilled(
			new Vector2(trackLeft, centerY - (trackHeight * 0.5f)),
			new Vector2(trackRight, centerY + (trackHeight * 0.5f)),
			enabled ? M3.U32(s.SurfaceContainerHighest) : M3.U32(s.OnSurface, M3.DisabledContainer), trackHeight);

		if (handleX > trackLeft)
		{
			drawList.AddRectFilled(
				new Vector2(trackLeft, centerY - (trackHeight * 0.5f)),
				new Vector2(handleX, centerY + (trackHeight * 0.5f)),
				M3.U32(accent), trackHeight);
		}

		if (hovered || active)
		{
			drawList.AddCircleFilled(new Vector2(handleX, centerY), handleRadius + (8f * scale),
				M3.U32(s.Primary, active ? M3.StatePressed : M3.StateHover), 24);
		}

		drawList.AddCircleFilled(new Vector2(handleX, centerY), handleRadius, M3.U32(accent), 24);
		drawList.AddCircleFilled(new Vector2(handleX, centerY), handleRadius * 0.45f, M3.U32(enabled ? s.OnPrimary : s.Surface, 0.9f), 16);

		if (hasReadout && active)
		{
			var padding = new Vector2(8f, 4f) * scale;
			var bubbleMin = new Vector2(handleX - (textSize.X * 0.5f) - padding.X, itemMin.Y - textSize.Y - (padding.Y * 2f) - (6f * scale));
			var bubbleMax = bubbleMin + textSize + (padding * 2f);
			drawList.AddRectFilled(bubbleMin, bubbleMax, M3.U32(s.InverseSurface), M3.ShapeSmall);
			drawList.AddText(bubbleMin + padding, M3.U32(s.InverseOnSurface), displayValue);
		}
	}

	public static float SliderValueGutter(string longestValue)
	{
		return ImGui.CalcTextSize(longestValue).X + (14f * M3.Scale);
	}
}
