using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static class M3Panel
{
	public static ImRaii.ChildDisposable Begin(string id, Vector2 size, bool scroll = true, Vector4? fill = null)
	{
		var s = M3.Scheme;
		using (ImRaii.PushColor(ImGuiCol.ChildBg, fill ?? M3.Alpha(s.SurfaceContainerLow, 0.88f))
			.Push(ImGuiCol.Border, M3.Alpha(s.OutlineVariant, 0.6f)))
		using (ImRaii.PushStyle(ImGuiStyleVar.ChildBorderSize, 1f).Push(ImGuiStyleVar.ChildRounding, M3.ShapeMedium))
		{
			return ImRaii.Child(id, size, true, scroll ? ImGuiWindowFlags.None : ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
		}
	}
}
