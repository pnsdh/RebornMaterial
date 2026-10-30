using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static partial class M3Widgets
{
	public static bool SearchField(string id, string hint, ref string text, float width, int maxLength = 128, bool busy = false, bool enabled = true)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = M3.FitText(40f, 8f);
		var origin = ImGui.GetCursorScreenPos();
		var drawList = ImGui.GetWindowDrawList();
		var min = origin;
		var max = origin + new Vector2(width, height);
		var hoveringField = enabled && M3Draw.MouseOver(min, max);

		drawList.AddRectFilled(min, max, M3.U32(s.SurfaceContainerHigh, !enabled ? 0.5f : hoveringField ? 1f : 0.92f), height * 0.5f);

		var iconPadding = 14f * scale;
		var iconSize = M3Draw.MeasureIcon(FontAwesomeIcon.Search);
		if (busy)
		{
			var stroke = 2f * scale;
			Spinner(drawList, new Vector2(min.X + iconPadding + (iconSize.X * 0.5f), min.Y + (height * 0.5f)),
				MathF.Max(1f, (iconSize.X - stroke) * 0.5f), s.Primary, stroke);
		}
		else
		{
			M3Draw.Icon(drawList, FontAwesomeIcon.Search,
				new Vector2(min.X + iconPadding, min.Y + ((height - iconSize.Y) * 0.5f)),
				M3.Alpha(s.OnSurfaceVariant, 0.9f));
		}

		var fieldStart = min.X + iconPadding + iconSize.X + (10f * scale);
		var hasText = !string.IsNullOrEmpty(text);
		var clearWidth = hasText ? 32f * scale : 0f;
		var fieldWidth = MathF.Max(24f * scale, max.X - fieldStart - (14f * scale) - clearWidth);

		ImGui.SetCursorScreenPos(new Vector2(fieldStart, min.Y + ((height - ImGui.GetFrameHeight()) * 0.5f)));
		ImGui.SetNextItemWidth(fieldWidth);

		bool changed;
		using (ImRaii.Disabled(!enabled))
		using (PushClearFrame())
		{
			changed = ImGui.InputTextWithHint(id, hint, ref text, maxLength, ImGuiInputTextFlags.AutoSelectAll);
		}

		if (hasText)
		{
			ImGui.SetCursorScreenPos(new Vector2(max.X - clearWidth - (6f * scale), min.Y + ((height - (26f * scale)) * 0.5f)));
			if (IconButton($"{id}_clear", FontAwesomeIcon.Times, M3Fork.T("Clear search"), diameter: 26f * scale, enabled: enabled))
			{
				text = string.Empty;
				changed = true;
			}
		}

		ImGui.SetCursorScreenPos(new Vector2(min.X, max.Y));
		ImGui.Dummy(new Vector2(width, 0f));
		return changed;
	}

	public static float TextFieldHeight => M3.FitText(48f, 12f);

	public static bool TextField(string id, string label, ref string text, float width, int maxLength = 256, string? supporting = null, bool error = false, FontAwesomeIcon leadingIcon = FontAwesomeIcon.None, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, bool enabled = true)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = TextFieldHeight;
		var paddingX = 16f * scale;
		var framePadding = ImGui.GetStyle().FramePadding.X;
		var bodyFontSize = ImGui.GetFontSize();

		float smallFontSize;
		using (ImRaii.PushFont(M3.LabelSmall))
		{
			smallFontSize = ImGui.GetFontSize();
		}

		var origin = ImGui.GetCursorScreenPos();
		var min = origin + new Vector2(0f, smallFontSize * 0.5f);
		var max = min + new Vector2(width, height);
		var drawList = ImGui.GetWindowDrawList();
		var hovered = M3Draw.MouseOver(min, max);

		var textLeft = min.X + paddingX;
		if (leadingIcon != FontAwesomeIcon.None)
		{
			var iconSize = M3Draw.MeasureIcon(leadingIcon);
			M3Draw.Icon(drawList, leadingIcon, new Vector2(textLeft, min.Y + ((height - iconSize.Y) * 0.5f)),
				error ? s.Error : s.OnSurfaceVariant);
			textLeft += iconSize.X + (12f * scale);
		}

		var trailingWidth = error ? M3Draw.MeasureIcon(FontAwesomeIcon.ExclamationCircle).X + (12f * scale) : 0f;
		var textRight = max.X - paddingX - trailingWidth;

		var inputLeft = textLeft - framePadding;
		ImGui.SetCursorScreenPos(new Vector2(inputLeft, min.Y + ((height - ImGui.GetFrameHeight()) * 0.5f)));
		ImGui.SetNextItemWidth(MathF.Max(24f * scale, textRight + framePadding - inputLeft));

		bool changed;
		using (ImRaii.Disabled(!enabled))
		using (PushClearFrame())
		{
			changed = ImGui.InputText(id, ref text, maxLength, flags);
		}

		var focused = ImGui.IsItemActive();
		var floating = M3Motion.Approach(ImGuiP.GetItemID(), focused || !string.IsNullOrEmpty(text) ? 1f : 0f, M3Motion.FastDuration);

		var dim = enabled ? 1f : M3.DisabledContent;
		var accent = M3.Alpha(error ? s.Error : focused ? s.Primary : hovered && enabled ? s.OnSurface : s.Outline, dim);
		var labelColor = M3.Alpha(error ? s.Error : focused ? s.Primary : s.OnSurfaceVariant, dim);

		var hasLabel = !string.IsNullOrEmpty(label);
		var floatingX = min.X + paddingX;
		var notchStart = floatingX - (4f * scale);
		var notchEnd = hasLabel
			? notchStart + (((ImGui.CalcTextSize(label).X * (smallFontSize / bodyFontSize)) + (8f * scale)) * floating)
			: notchStart;
		OutlineWithNotch(drawList, min, max, M3.ShapeExtraSmall, notchStart, notchEnd, M3.U32(accent), (focused || error ? 2f : 1f) * scale);

		if (hasLabel)
		{
			var restingPosition = new Vector2(textLeft, min.Y + ((height - bodyFontSize) * 0.5f));
			var floatingPosition = new Vector2(floatingX, min.Y - (smallFontSize * 0.5f));
			drawList.AddText(ImGui.GetFont(), float.Lerp(bodyFontSize, smallFontSize, floating),
				Vector2.Lerp(restingPosition, floatingPosition, floating), M3.U32(labelColor), label);
		}

		if (error)
		{
			var iconSize = M3Draw.MeasureIcon(FontAwesomeIcon.ExclamationCircle);
			M3Draw.Icon(drawList, FontAwesomeIcon.ExclamationCircle,
				new Vector2(max.X - paddingX - iconSize.X, min.Y + ((height - iconSize.Y) * 0.5f)), s.Error);
		}

		var bottom = max.Y;
		if (!string.IsNullOrEmpty(supporting))
		{
			using var font = ImRaii.PushFont(M3.LabelSmall);
			bottom = M3Draw.WrappedText(supporting, new Vector2(min.X + paddingX, max.Y + (4f * scale)),
				MathF.Max(32f * scale, width - (paddingX * 2f)), error ? s.Error : M3.Alpha(s.OnSurfaceVariant, 0.9f));
		}

		ImGui.SetCursorScreenPos(new Vector2(origin.X, bottom));
		ImGui.Dummy(new Vector2(width, 0f));
		return changed;
	}

	private static void OutlineWithNotch(ImDrawListPtr drawList, Vector2 min, Vector2 max, float rounding, float notchStart, float notchEnd, uint color, float thickness)
	{
		if (notchEnd - notchStart < 1f)
		{
			drawList.AddRect(min, max, color, rounding, ImDrawFlags.None, thickness);
			return;
		}

		drawList.PathLineTo(new Vector2(notchEnd, min.Y));
		drawList.PathArcToFast(new Vector2(max.X - rounding, min.Y + rounding), rounding, 9, 12);
		drawList.PathArcToFast(new Vector2(max.X - rounding, max.Y - rounding), rounding, 0, 3);
		drawList.PathArcToFast(new Vector2(min.X + rounding, max.Y - rounding), rounding, 3, 6);
		drawList.PathArcToFast(new Vector2(min.X + rounding, min.Y + rounding), rounding, 6, 9);
		drawList.PathLineTo(new Vector2(notchStart, min.Y));
		drawList.PathStroke(color, ImDrawFlags.None, thickness);
	}
}
