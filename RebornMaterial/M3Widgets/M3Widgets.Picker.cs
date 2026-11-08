using Dalamud.Interface.Textures.TextureWraps;

namespace RebornMaterial;

public static partial class M3Widgets
{
	public static bool PickerField(string id, string label, float width, bool open, bool placeholder = false, IDalamudTextureWrap? icon = null,
		bool enabled = true, string? tooltip = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = ComboHeight;

		var clicked = ImGui.InvisibleButton(id, new Vector2(MathF.Max(1f, width), height)) && enabled;
		var mouseOver = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var hovered = enabled && mouseOver;
		var held = enabled && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = M3.ShapeSmall;
		var dim = enabled ? 1f : M3.DisabledContent;

		var fill = M3.Alpha(s.SurfaceContainerHighest, enabled ? 0.55f : 0.25f);
		if (hovered || held)
		{
			fill = M3.StateLayer(fill, s.OnSurface, hovered, held);
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		drawList.AddRectFilled(min, max, M3.U32(fill), rounding);
		drawList.AddRect(min, max, M3.U32(open ? s.Primary : s.Outline, (open ? 1f : 0.75f) * dim), rounding, ImDrawFlags.None, (open ? 2f : 1f) * scale);

		var x = min.X + ComboTextInset;
		if (icon != null)
		{
			var iconSize = MathF.Min(height - (12f * scale), ImGui.GetTextLineHeight() + (4f * scale));
			if (M3Draw.Texture(drawList, icon, new Vector2(x, min.Y + ((height - iconSize) * 0.5f)), iconSize, dim))
			{
				x += iconSize + (8f * scale);
			}
		}

		var display = M3Navigation.Truncate(label, MathF.Max(8f * scale, max.X - x - ComboChevronWidth - ComboTextInset));
		var textSize = ImGui.CalcTextSize(display);
		var textColor = placeholder ? s.OnSurfaceVariant : s.OnSurface;
		drawList.AddText(new Vector2(x, min.Y + ((height - textSize.Y) * 0.5f)), M3.U32(textColor, 0.95f * dim), display);

		var chevron = new Vector2(max.X - (16f * scale), min.Y + (height * 0.5f));
		var arm = 4.5f * scale;
		var chevronColor = M3.U32(s.OnSurfaceVariant, (hovered || open ? 1f : 0.8f) * dim);
		drawList.AddLine(chevron + new Vector2(-arm, -arm * 0.5f), chevron + new Vector2(0f, arm * 0.6f), chevronColor, 2f * scale);
		drawList.AddLine(chevron + new Vector2(0f, arm * 0.6f), chevron + new Vector2(arm, -arm * 0.5f), chevronColor, 2f * scale);
		M3Draw.FocusRing(min, max, rounding);

		if (mouseOver)
		{
			M3Tooltip.Show(tooltip ?? (display != label ? label : null));
		}

		return clicked;
	}
}
