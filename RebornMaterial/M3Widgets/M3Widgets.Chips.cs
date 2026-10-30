using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static partial class M3Widgets
{
	public static float ChipHeight => M3.FitText(32f, 6f);

	public static float ChipWidth(string label, FontAwesomeIcon icon = FontAwesomeIcon.None, FontAwesomeIcon trailingIcon = FontAwesomeIcon.None, IDalamudTextureWrap? texture = null)
	{
		var scale = M3.Scale;
		var width = ImGui.CalcTextSize(label).X + (16f * scale * 2f);
		var iconWidth = IconSlotWidth(icon, texture);
		if (iconWidth > 0f)
		{
			width += iconWidth + (8f * scale);
		}

		if (trailingIcon != FontAwesomeIcon.None)
		{
			width += M3Draw.MeasureIcon(trailingIcon).X + (8f * scale);
		}

		return width;
	}

	public static bool Chip(string id, string label, bool selected, FontAwesomeIcon icon = FontAwesomeIcon.None, string? tooltip = null, Vector4? accent = null, FontAwesomeIcon trailingIcon = FontAwesomeIcon.None, bool enabled = true, IDalamudTextureWrap? texture = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = ChipHeight;
		var size = new Vector2(ChipWidth(label, icon, trailingIcon, texture), height);
		var tone = accent ?? s.Primary;

		var pressed = ImGui.InvisibleButton(id, size) && enabled;
		var mouseOver = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var hovered = enabled && mouseOver;
		var held = enabled && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = M3.ShapeSmall;

		var container = selected ? s.SecondaryContainer : M3.Alpha(s.Surface, 0f);
		var content = selected ? s.OnSecondaryContainer : s.OnSurfaceVariant;
		var outline = selected ? (Vector4?)null : M3.Alpha(s.Outline, 0.8f);
		if (!enabled)
		{
			container = selected ? M3.Alpha(s.OnSurface, M3.DisabledContainer) : container;
			content = M3.Alpha(s.OnSurface, M3.DisabledContent);
			tone = content;
			outline = selected ? null : M3.Alpha(s.OnSurface, M3.DisabledContainer);
		}
		else if (hovered || held)
		{
			container = container.W > 0f
				? M3.StateLayer(container, content, hovered, held)
				: M3.Alpha(s.OnSurface, held ? M3.StatePressed : M3.StateHover);
		}

		M3Draw.Container(drawList, min, max, container, rounding, outline, 1f);

		var iconWidth = IconSlotWidth(icon, texture);
		var gap = iconWidth > 0f ? 8f * scale : 0f;
		var trailingWidth = trailingIcon == FontAwesomeIcon.None ? 0f : M3Draw.MeasureIcon(trailingIcon).X + (8f * scale);
		var textSize = ImGui.CalcTextSize(label);
		var cursorX = min.X + ((size.X - (iconWidth + gap + textSize.X + trailingWidth)) * 0.5f);

		if (iconWidth > 0f)
		{
			DrawIconSlot(drawList, icon, texture, cursorX, min.Y, height, selected ? tone : content, enabled ? 1f : M3.DisabledContent);
			cursorX += iconWidth + gap;
		}

		drawList.AddText(new Vector2(cursorX, min.Y + ((height - textSize.Y) * 0.5f)), M3.U32(content), label);

		if (trailingIcon != FontAwesomeIcon.None)
		{
			var trailingSize = M3Draw.MeasureIcon(trailingIcon);
			M3Draw.Icon(drawList, trailingIcon,
				new Vector2(cursorX + textSize.X + (8f * scale), min.Y + ((height - trailingSize.Y) * 0.5f)), content);
		}

		M3Draw.FocusRing(min, max, rounding);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (mouseOver)
		{
			M3Tooltip.Show(tooltip);
		}

		return pressed;
	}

	public static Vector2 PillSize(string label, FontAwesomeIcon icon = FontAwesomeIcon.None)
	{
		var scale = M3.Scale;
		var width = ImGui.CalcTextSize(label).X + (12f * scale * 2f);
		width += icon == FontAwesomeIcon.None
			? (6f * scale) + (6f * scale)
			: M3Draw.MeasureIcon(icon).X + (6f * scale);
		return new Vector2(width, M3.FitText(24f, 3f));
	}

	public static bool Pill(string id, string label, Vector4 accent, FontAwesomeIcon icon = FontAwesomeIcon.None, string? tooltip = null, bool interactive = false)
	{
		var scale = M3.Scale;
		var size = PillSize(label, icon);

		var clicked = false;
		if (interactive)
		{
			clicked = ImGui.InvisibleButton(id, size);
		}
		else
		{
			ImGui.Dummy(size);
		}

		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = size.Y * 0.5f;

		if (interactive && hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		drawList.AddRectFilled(min, max, M3.U32(accent, hovered ? 0.20f : 0.13f), rounding);
		drawList.AddRect(min, max, M3.U32(accent, hovered ? 0.62f : 0.40f), rounding, ImDrawFlags.None, 1f * scale);

		var cursorX = min.X + (12f * scale);
		if (icon == FontAwesomeIcon.None)
		{
			var radius = 3f * scale;
			drawList.AddCircleFilled(new Vector2(cursorX + radius, min.Y + (size.Y * 0.5f)), radius, M3.U32(accent), 12);
			cursorX += (radius * 2f) + (6f * scale);
		}
		else
		{
			var iconSize = M3Draw.MeasureIcon(icon);
			M3Draw.Icon(drawList, icon, new Vector2(cursorX, min.Y + ((size.Y - iconSize.Y) * 0.5f)), accent);
			cursorX += iconSize.X + (6f * scale);
		}

		var textSize = ImGui.CalcTextSize(label);
		drawList.AddText(new Vector2(cursorX, min.Y + ((size.Y - textSize.Y) * 0.5f)), M3.U32(M3.Scheme.OnSurface, 0.95f), label);

		if (interactive)
		{
			M3Draw.FocusRing(min, max, rounding);
		}

		if (hovered)
		{
			M3Tooltip.Show(tooltip);
		}

		return clicked;
	}

	private static float InputChipRemoveWidth => ChipHeight;

	public static float InputChipWidth(string label, FontAwesomeIcon icon = FontAwesomeIcon.None)
	{
		var scale = M3.Scale;
		var width = (12f * scale) + ImGui.CalcTextSize(label).X + InputChipRemoveWidth;
		if (icon != FontAwesomeIcon.None)
		{
			width += M3Draw.MeasureIcon(icon).X + (8f * scale);
		}

		return width;
	}

	public static bool InputChip(string id, string label, out bool removed, FontAwesomeIcon icon = FontAwesomeIcon.None, string? tooltip = null, Vector4? accent = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = ChipHeight;
		var width = InputChipWidth(label, icon);
		var removeWidth = InputChipRemoveWidth;
		var tone = accent ?? s.Primary;

		var pressed = ImGui.InvisibleButton(id, new Vector2(width - removeWidth, height));
		var bodyHovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var bodyHeld = ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = min + new Vector2(width, height);
		M3Draw.FocusRing(min, max, M3.ShapeSmall);

		ImGui.SameLine(0f, 0f);
		removed = ImGui.InvisibleButton($"{id}_remove", new Vector2(removeWidth, height));
		var removeHovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var removeHeld = ImGui.IsItemActive();
		M3Draw.FocusRing(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), height * 0.5f);

		var drawList = ImGui.GetWindowDrawList();
		var rounding = M3.ShapeSmall;
		var content = s.OnSurfaceVariant;

		var container = M3.Alpha(s.Surface, 0f);
		if (bodyHovered || bodyHeld || removeHovered || removeHeld)
		{
			container = M3.Alpha(s.OnSurface, bodyHeld || removeHeld ? M3.StatePressed : M3.StateHover);
		}

		M3Draw.Container(drawList, min, max, container, rounding, M3.Alpha(s.Outline, 0.8f), 1f);

		var cursorX = min.X + (12f * scale);
		if (icon != FontAwesomeIcon.None)
		{
			var iconSize = M3Draw.MeasureIcon(icon);
			M3Draw.Icon(drawList, icon, new Vector2(cursorX, min.Y + ((height - iconSize.Y) * 0.5f)), tone);
			cursorX += iconSize.X + (8f * scale);
		}

		var textSize = ImGui.CalcTextSize(label);
		drawList.AddText(new Vector2(cursorX, min.Y + ((height - textSize.Y) * 0.5f)), M3.U32(content), label);

		var removeMin = new Vector2(max.X - removeWidth, min.Y);
		var removeCenter = removeMin + (new Vector2(removeWidth, height) * 0.5f);
		if (removeHovered || removeHeld)
		{
			drawList.AddCircleFilled(removeCenter, height * 0.36f, M3.U32(s.OnSurface, removeHeld ? M3.StatePressed : M3.StateHover), 24);
		}

		var arm = 4f * scale;
		var crossColor = M3.U32(removeHovered ? s.OnSurface : content);
		drawList.AddLine(removeCenter - new Vector2(arm, arm), removeCenter + new Vector2(arm, arm), crossColor, 1.5f * scale);
		drawList.AddLine(removeCenter + new Vector2(-arm, arm), removeCenter + new Vector2(arm, -arm), crossColor, 1.5f * scale);

		if (bodyHovered || removeHovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (removeHovered)
		{
			M3Tooltip.Show($"Remove {label}");
		}
		else if (bodyHovered)
		{
			M3Tooltip.Show(tooltip);
		}

		return pressed;
	}

	public static Vector2 BadgeSize(string? text)
	{
		var scale = M3.Scale;
		if (string.IsNullOrEmpty(text))
		{
			return new Vector2(6f, 6f) * scale;
		}

		using var font = ImRaii.PushFont(M3.LabelSmall);
		var textSize = ImGui.CalcTextSize(text);
		var height = MathF.Max(16f * scale, textSize.Y + (2f * scale));
		return new Vector2(MathF.Max(height, textSize.X + (8f * scale)), height);
	}

	public static string BadgeCount(int count)
	{
		return count > 999 ? "999+" : count.ToString();
	}

	public static void Badge(string? text = null, Vector4? color = null)
	{
		var size = BadgeSize(text);
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		BadgeAt(new Vector2(max.X - (size.X * 0.5f), min.Y + (size.Y * 0.5f)), text, color);
	}

	public static void BadgeAt(Vector2 center, string? text = null, Vector4? color = null)
	{
		var s = M3.Scheme;
		var drawList = ImGui.GetWindowDrawList();
		var fill = color ?? s.Error;
		var size = BadgeSize(text);

		if (string.IsNullOrEmpty(text))
		{
			drawList.AddCircleFilled(center, size.X * 0.5f, M3.U32(fill), 12);
			return;
		}

		var min = center - (size * 0.5f);
		drawList.AddRectFilled(min, min + size, M3.U32(fill), size.Y * 0.5f);

		using var font = ImRaii.PushFont(M3.LabelSmall);
		var textSize = ImGui.CalcTextSize(text);

		var onFill = color is null ? s.OnError : M3.ContentOn(fill);
		drawList.AddText(center - (textSize * 0.5f), M3.U32(onFill), text);
	}
}
