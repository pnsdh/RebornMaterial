using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static class M3Table
{
	public static Scope PushStyle()
	{
		var s = M3.Scheme;
		ImGui.PushStyleColor(ImGuiCol.TableHeaderBg, new Vector4(0f, 0f, 0f, 0f));
		ImGui.PushStyleColor(ImGuiCol.TableBorderStrong, M3.Alpha(s.OutlineVariant, 0.6f));
		ImGui.PushStyleColor(ImGuiCol.TableBorderLight, M3.Alpha(s.OutlineVariant, 0.35f));
		return new Scope(3);
	}

	public static void Header(string text, string? tooltip = null, bool center = true)
	{
		using (ImRaii.PushFont(M3.LabelSmall))
		using (ImRaii.PushColor(ImGuiCol.Text, M3.Scheme.OnSurfaceVariant))
		{
			if (center)
			{
				Center(ImGui.CalcTextSize(text).X);
			}

			ImGui.TextUnformatted(text);
		}

		if (tooltip != null)
		{
			M3Tooltip.Hovered(tooltip);
		}
	}

	public static void CenteredText(string text, Vector4? color = null)
	{
		Center(ImGui.CalcTextSize(text).X);
		using var textColor = ImRaii.PushColor(ImGuiCol.Text, color ?? M3.Scheme.OnSurface);
		ImGui.TextUnformatted(text);
	}

	public static void Progress(float fraction, string? text = null, Vector4? tone = null, float height = 0f)
	{
		var s = M3.Scheme;
		height = height > 0f ? height : ImGui.GetTextLineHeight();
		ImGui.Dummy(new Vector2(MathF.Max(1f, ImGui.GetContentRegionAvail().X), height));
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = height * 0.5f;
		drawList.AddRectFilled(min, max, M3.U32(s.SurfaceContainerHighest), rounding);

		fraction = float.IsNaN(fraction) ? 0f : Math.Clamp(fraction, 0f, 1f);
		if (fraction > 0f)
		{
			drawList.AddRectFilled(min, new Vector2(float.Lerp(min.X, max.X, fraction), max.Y), M3.U32(tone ?? s.Primary, 0.45f), rounding);
		}

		if (!string.IsNullOrEmpty(text))
		{
			var textSize = ImGui.CalcTextSize(text);
			drawList.AddText(min + ((max - min - textSize) * 0.5f), M3.U32(s.OnSurface), text);
		}
	}

	private static void Center(float width)
	{
		var offset = (ImGui.GetContentRegionAvail().X - width) * 0.5f;
		if (offset > 0f)
		{
			ImGui.SetCursorPosX(ImGui.GetCursorPosX() + offset);
		}
	}

	public readonly struct Scope(int colors) : IDisposable
	{
		public void Dispose()
		{
			if (colors > 0)
			{
				ImGui.PopStyleColor(colors);
			}
		}
	}
}
