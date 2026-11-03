using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static class M3Badge
{
	private static float PaddingX => 6f * M3.Scale;

	public static void Draw(string label, Vector4 accent, string? tooltip = null)
	{
		var frameHeight = ImGui.GetFrameHeight();
		var size = Size(label);

		ImGui.Dummy(new Vector2(size.X, frameHeight));
		_ = Paint(ImGui.GetWindowDrawList(), ImGui.GetItemRectMin() + new Vector2(0f, (frameHeight - size.Y) * 0.5f), label, accent);

		if (!string.IsNullOrEmpty(tooltip) && ImGui.IsItemHovered())
		{
			M3Tooltip.Show(tooltip);
		}
	}

	public static Vector2 Size(string label)
	{
		var lineHeight = ImGui.GetTextLineHeight();
		using var font = ImRaii.PushFont(M3.LabelSmall);
		return new Vector2(ImGui.CalcTextSize(label).X + (PaddingX * 2f), lineHeight);
	}

	public static Vector2 Paint(ImDrawListPtr drawList, Vector2 min, string label, Vector4 accent)
	{
		var max = min + Size(label);
		drawList.AddRectFilled(min, max, M3.U32(accent, 0.18f), M3.ShapeFull);
		drawList.AddRect(min, max, M3.U32(accent, 0.45f), M3.ShapeFull, ImDrawFlags.None, 1f * M3.Scale);

		using var font = ImRaii.PushFont(M3.LabelSmall);
		var textSize = ImGui.CalcTextSize(label);
		drawList.AddText(new Vector2(min.X + PaddingX, min.Y + ((max.Y - min.Y - textSize.Y) * 0.5f)), M3.U32(accent, 0.95f), label);
		return max;
	}
}
