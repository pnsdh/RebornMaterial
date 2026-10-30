using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static partial class M3Widgets
{
	public static float ColorSwatchSize => 28f * M3.Scale;

	public static bool ColorSwatch(string id, ref Vector4 color, Vector4? defaultColor = null, bool alpha = true, bool enabled = true, string? tooltip = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var diameter = ColorSwatchSize;
		var radius = diameter * 0.5f;

		var clicked = ImGui.InvisibleButton(id, Vector2.One * diameter) && enabled;
		var mouseOver = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var hovered = enabled && mouseOver;
		var min = ImGui.GetItemRectMin();
		var center = min + (Vector2.One * radius);
		var drawList = ImGui.GetWindowDrawList();
		var dim = enabled ? 1f : M3.DisabledContent;

		if (alpha && color.W < 0.999f)
		{
			drawList.AddCircleFilled(center, radius, M3.U32(s.SurfaceContainerHighest, dim), 32);
			drawList.PathArcTo(center, radius, MathF.PI * 0.5f, MathF.PI * 1.5f, 16);
			drawList.PathFillConvex(M3.U32(s.OnSurfaceVariant, 0.55f * dim));
		}

		var shown = alpha ? color : color with { W = 1f };
		drawList.AddCircleFilled(center, radius, M3.U32(shown with { W = shown.W * dim }), 32);
		drawList.AddCircle(center, radius, M3.U32(s.Outline, (hovered ? 1f : 0.6f) * dim), 32, 1.5f * scale);
		M3Draw.FocusRing(min, min + (Vector2.One * diameter), radius);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (mouseOver)
		{
			M3Tooltip.Show(tooltip);
		}

		var popupId = $"{id}_picker";
		if (clicked)
		{
			ImGui.OpenPopup(popupId);
		}

		var changed = false;
		using var popup = ImRaii.Popup(popupId);
		if (!popup)
		{
			return false;
		}

		var flags = alpha ? ImGuiColorEditFlags.AlphaBar | ImGuiColorEditFlags.AlphaPreviewHalf : ImGuiColorEditFlags.NoAlpha;
		changed = ImGui.ColorPicker4($"##{popupId}_picker", ref color, flags);

		var hex = HexOf(color, alpha);
		ImGui.SetNextItemWidth(120f * scale);
		if (ImGui.InputText($"##{popupId}_hex", ref hex, 10) && TryParseHex(hex, out var parsed))
		{
			color = alpha ? parsed : parsed with { W = color.W };
			changed = true;
		}

		if (defaultColor is { } fallback)
		{
			ImGui.SameLine(0f, M3.Space2);
			if (Button($"##{popupId}_reset", M3Fork.T("Reset"), M3ButtonStyle.Text, FontAwesomeIcon.Undo, enabled: fallback != color))
			{
				color = fallback;
				changed = true;
			}
		}

		return changed;
	}

	private static string HexOf(Vector4 color, bool alpha)
	{
		static int Channel(float value)
		{
			return (int)MathF.Round(Math.Clamp(value, 0f, 1f) * 255f);
		}

		var rgb = $"#{Channel(color.X):X2}{Channel(color.Y):X2}{Channel(color.Z):X2}";
		return alpha ? $"{rgb}{Channel(color.W):X2}" : rgb;
	}

	private static bool TryParseHex(string text, out Vector4 color)
	{
		color = default;
		var digits = text.Trim().TrimStart('#');
		if ((digits.Length != 6 && digits.Length != 8) || !uint.TryParse(digits, System.Globalization.NumberStyles.HexNumber, null, out var value))
		{
			return false;
		}

		if (digits.Length == 6)
		{
			value = (value << 8) | 0xFF;
		}

		color = new Vector4(((value >> 24) & 0xFF) / 255f, ((value >> 16) & 0xFF) / 255f, ((value >> 8) & 0xFF) / 255f, (value & 0xFF) / 255f);
		return true;
	}
}
