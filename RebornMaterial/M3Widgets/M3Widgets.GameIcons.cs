namespace RebornMaterial;

public static partial class M3Widgets
{
	public static void GameIcon(uint iconId, float size, float alpha = 1f, bool hq = false, string? tooltip = null)
	{
		ImGui.Dummy(new Vector2(size));
		var texture = M3ActionIcon.GameIcon(iconId, hq: hq);
		if (texture != null)
		{
			M3Draw.Texture(ImGui.GetWindowDrawList(), texture, ImGui.GetItemRectMin(), size, alpha);
		}

		if (tooltip != null && ImGui.IsItemHovered())
		{
			M3Tooltip.Show(tooltip);
		}
	}

	public static bool GameIconButton(string id, uint iconId, float size, float alpha = 1f, string? tooltip = null, bool hq = false, bool selected = false, bool enabled = true)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var pressed = ImGui.InvisibleButton(id, new Vector2(size)) && enabled;
		var mouseOver = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var hovered = enabled && mouseOver;
		var held = enabled && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var pad = new Vector2(2f * scale);

		if (hovered || held)
		{
			drawList.AddRectFilled(min - pad, max + pad, M3.U32(s.OnSurface, held ? M3.StatePressed + M3.StateHover : M3.StateHover * 1.5f), M3.ShapeExtraSmall);
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		var texture = M3ActionIcon.GameIcon(iconId, hq: hq);
		if (texture != null)
		{
			M3Draw.Texture(drawList, texture, min, size, enabled ? hovered ? 1f : alpha : alpha * M3.DisabledContent);
		}

		if (selected)
		{
			drawList.AddRect(min - pad, max + pad, M3.U32(s.Primary, 0.9f), M3.ShapeExtraSmall + pad.X, ImDrawFlags.None, 1.5f * scale);
		}

		M3Draw.FocusRing(min, max, M3.ShapeExtraSmall);
		if (mouseOver)
		{
			M3Tooltip.Show(tooltip);
		}

		return pressed;
	}
}
