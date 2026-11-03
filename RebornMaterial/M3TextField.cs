using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static class M3TextField
{
	public static float Height => M3.FitText(40f, 8f);

	public static bool Draw(string id, string hint, ref string text, float width, int maxLength = 256, bool password = false)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = Height;
		var min = ImGui.GetCursorScreenPos();
		var max = min + new Vector2(width, height);
		var drawList = ImGui.GetWindowDrawList();
		var hovered = ImGui.IsMouseHoveringRect(min, max) && ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows);

		var fill = hovered ? M3.StateLayer(s.SurfaceContainerHighest, s.OnSurface, true, false) : s.SurfaceContainerHighest;
		drawList.AddRectFilled(min, max, M3.U32(fill), M3.ShapeExtraSmall, ImDrawFlags.RoundCornersTop);

		var padding = new Vector2(12f * scale, MathF.Max(0f, (height - ImGui.GetTextLineHeight()) * 0.5f));
		var transparent = new Vector4(0f, 0f, 0f, 0f);
		var flags = password ? ImGuiInputTextFlags.Password : ImGuiInputTextFlags.None;

		bool changed;
		using (ImRaii.PushId(id))
		using (ImRaii.PushColor(ImGuiCol.FrameBg, transparent)
			.Push(ImGuiCol.FrameBgHovered, transparent)
			.Push(ImGuiCol.FrameBgActive, transparent))
		using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, padding))
		{
			ImGui.SetNextItemWidth(width);
			changed = ImGui.InputTextWithHint("##field", hint, ref text, maxLength, flags);
		}

		var focused = ImGui.IsItemActive();
		var thickness = (focused ? 2f : 1f) * scale;
		var indicator = focused ? s.Primary : M3.Alpha(s.OnSurfaceVariant, hovered ? 1f : 0.7f);
		drawList.AddRectFilled(new Vector2(min.X, max.Y - thickness), max, M3.U32(indicator));

		return changed;
	}
}
