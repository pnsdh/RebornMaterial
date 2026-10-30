using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public readonly record struct M3Segment(
	string Label,
	FontAwesomeIcon Icon = FontAwesomeIcon.None,
	string? Tooltip = null,
	Vector4? Accent = null);

public static partial class M3Widgets
{
	public static float SegmentedHeight => M3.FitText(32f, 6f);

	public static float SegmentedWidth(ReadOnlySpan<M3Segment> segments, ImFontPtr? labelFont = null)
	{
		var scale = M3.Scale;
		var widest = 0f;

		using var font = ImRaii.PushFont(labelFont.GetValueOrDefault(), labelFont.HasValue);
		foreach (var segment in segments)
		{
			var width = ImGui.CalcTextSize(segment.Label).X + (14f * scale * 2f);
			if (segment.Icon != FontAwesomeIcon.None)
			{
				width += M3Draw.MeasureIcon(segment.Icon).X + (6f * scale);
			}

			widest = MathF.Max(widest, width);
		}

		return widest * segments.Length;
	}

	public static int SegmentedButtons(string id, ReadOnlySpan<M3Segment> segments, int selectedIndex, float? width = null, ImFontPtr? labelFont = null, bool enabled = true)
	{
		if (segments.Length == 0)
		{
			return -1;
		}

		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = SegmentedHeight;
		var total = width ?? SegmentedWidth(segments, labelFont);
		var segmentWidth = total / segments.Length;
		var rounding = height * 0.5f;
		var origin = ImGui.GetCursorScreenPos();
		var drawList = ImGui.GetWindowDrawList();
		var result = -1;

		using var idScope = ImRaii.PushId(id);

		for (var i = 0; i < segments.Length; i++)
		{
			var segment = segments[i];
			var selected = i == selectedIndex;
			var segmentMin = new Vector2(origin.X + (segmentWidth * i), origin.Y);

			ImGui.SetCursorScreenPos(segmentMin);
			ImGui.PushID(i);
			var clicked = ImGui.InvisibleButton("##segment", new Vector2(segmentWidth, height));
			ImGui.PopID();
			if (clicked && !selected && enabled)
			{
				result = i;
			}

			var mouseOver = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
			var hovered = enabled && mouseOver;
			var held = enabled && ImGui.IsItemActive();
			var segmentMax = segmentMin + new Vector2(segmentWidth, height);
			var tone = enabled ? segment.Accent ?? s.Primary : M3.Alpha(s.OnSurface, M3.DisabledContent);

			var corners = segments.Length == 1 ? ImDrawFlags.RoundCornersAll
				: i == 0 ? ImDrawFlags.RoundCornersLeft
				: i == segments.Length - 1 ? ImDrawFlags.RoundCornersRight
				: ImDrawFlags.RoundCornersNone;

			if (selected)
			{
				drawList.AddRectFilled(segmentMin, segmentMax,
					enabled ? M3.U32(s.SecondaryContainer, 0.95f) : M3.U32(s.OnSurface, M3.DisabledContainer), rounding, corners);
			}

			if (hovered || held)
			{
				drawList.AddRectFilled(segmentMin, segmentMax,
					M3.U32(selected ? tone : s.OnSurface, held ? M3.StatePressed : M3.StateHover), rounding, corners);
				ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			}

			var content = selected ? tone
				: enabled ? M3.Alpha(s.OnSurfaceVariant, hovered ? 1f : 0.85f)
				: M3.Alpha(s.OnSurface, M3.DisabledContent);
			var iconSize = segment.Icon == FontAwesomeIcon.None ? Vector2.Zero : M3Draw.MeasureIcon(segment.Icon);
			var gap = segment.Icon == FontAwesomeIcon.None ? 0f : 6f * scale;

			using (ImRaii.PushFont(labelFont.GetValueOrDefault(), labelFont.HasValue))
			{
				var textSize = ImGui.CalcTextSize(segment.Label);
				var cursorX = segmentMin.X + ((segmentWidth - iconSize.X - gap - textSize.X) * 0.5f);

				if (segment.Icon != FontAwesomeIcon.None)
				{
					M3Draw.Icon(drawList, segment.Icon, new Vector2(cursorX, segmentMin.Y + ((height - iconSize.Y) * 0.5f)), content);
					cursorX += iconSize.X + gap;
				}

				drawList.AddText(new Vector2(cursorX, segmentMin.Y + ((height - textSize.Y) * 0.5f)), M3.U32(content), segment.Label);
			}

			if (i > 0)
			{
				drawList.AddLine(segmentMin + new Vector2(0f, 1f * scale), new Vector2(segmentMin.X, segmentMax.Y - (1f * scale)),
					M3.U32(s.Outline, 0.55f), 1f * scale);
			}

			M3Draw.FocusRing(segmentMin, segmentMax, rounding);

			if (mouseOver)
			{
				M3Tooltip.Show(segment.Tooltip);
			}
		}

		drawList.AddRect(origin, origin + new Vector2(total, height), M3.U32(s.Outline, 0.65f),
			rounding, ImDrawFlags.RoundCornersAll, 1f * scale);

		ImGui.SetCursorScreenPos(origin);
		ImGui.Dummy(new Vector2(total, height));
		return result;
	}
}
