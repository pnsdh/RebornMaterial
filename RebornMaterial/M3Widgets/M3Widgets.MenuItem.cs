using Dalamud.Interface.Textures.TextureWraps;

namespace RebornMaterial;

public static partial class M3Widgets
{
	public static float MenuItemHeight => M3.FitText(34f, 7f);

	public static bool MenuItem(string id, string label, bool selected, FontAwesomeIcon icon = FontAwesomeIcon.None, bool enabled = true, string? shortcut = null, string? tooltip = null, IDalamudTextureWrap? texture = null)
	{
		return MenuItemCore(id, label, selected, icon, enabled, shortcut, tooltip, texture, false);
	}

	internal static bool MenuItemCore(string id, string label, bool selected, FontAwesomeIcon icon, bool enabled, string? shortcut, string? tooltip, IDalamudTextureWrap? texture, bool fit,
		bool submenu = false, bool open = false)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = MenuItemHeight;
		var leadWidth = MathF.Max(24f * scale, texture != null ? TextureIconSize + (4f * scale) : 0f);
		var shortcutWidth = submenu ? 28f * scale
			: string.IsNullOrEmpty(shortcut) ? 0f : ImGui.CalcTextSize(shortcut).X + (16f * scale);
		var available = ImGui.GetContentRegionAvail().X;
		var needed = leadWidth + (8f * scale) + ImGui.CalcTextSize(label).X + shortcutWidth + (16f * scale);
		var width = fit ? MathF.Max(1f, available) : MathF.Max(available, needed);

		var pressed = ImGui.InvisibleButton(id, new Vector2(width, height)) && enabled;
		var mouseOver = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var hovered = enabled && mouseOver;
		var held = enabled && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var dim = enabled ? 1f : M3.DisabledContent;

		if (selected)
		{
			drawList.AddRectFilled(min, max, M3.U32(s.SecondaryContainer, 0.8f * dim), M3.ShapeSmall);
		}

		if (hovered || held || open)
		{
			drawList.AddRectFilled(min, max, M3.U32(s.OnSurface, held ? M3.StatePressed : M3.StateHover), M3.ShapeSmall);
		}

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		var leadX = min.X + (8f * scale);
		if (selected)
		{
			DrawIconSlot(drawList, FontAwesomeIcon.Check, null, leadX, min.Y, height, M3.Alpha(s.Primary, dim));
		}
		else
		{
			DrawIconSlot(drawList, icon, texture, leadX, min.Y, height, M3.Alpha(s.OnSurfaceVariant, dim), dim);
		}

		var textX = min.X + leadWidth + (8f * scale);
		var room = max.X - textX - shortcutWidth - (8f * scale);
		var display = fit ? M3Navigation.Truncate(label, room) : label;
		var textSize = ImGui.CalcTextSize(display);
		drawList.AddText(new Vector2(textX, min.Y + ((height - textSize.Y) * 0.5f)),
			M3.U32(selected ? s.OnSecondaryContainer : s.OnSurface, 0.95f * dim), display);

		if (submenu)
		{
			var chevronSize = M3Draw.MeasureIcon(FontAwesomeIcon.ChevronRight);
			M3Draw.IconCentered(drawList, FontAwesomeIcon.ChevronRight,
				new Vector2(max.X - (12f * scale) - chevronSize.X, min.Y), new Vector2(max.X - (12f * scale), max.Y),
				M3.Alpha(s.OnSurfaceVariant, dim), 0.8f);
		}
		else if (!string.IsNullOrEmpty(shortcut))
		{
			var shortcutSize = ImGui.CalcTextSize(shortcut);
			drawList.AddText(new Vector2(max.X - (12f * scale) - shortcutSize.X, min.Y + ((height - shortcutSize.Y) * 0.5f)),
				M3.U32(s.OnSurfaceVariant, 0.85f * dim), shortcut);
		}

		M3Draw.FocusRing(min, max, M3.ShapeSmall);

		if (mouseOver)
		{
			M3Tooltip.Show(tooltip ?? (display != label ? label : null));
		}

		return pressed;
	}
}
