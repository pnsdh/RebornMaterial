using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public readonly record struct M3ColorChoice(string Name, Vector4 Color);

public static partial class M3Widgets
{
	private static float ChoicesGap => MathF.Max(M3.Space2, 6f * M3.Scale);

	public static float ColorChoicesWidth(int count, bool custom = false)
	{
		var n = count + (custom ? 1 : 0);
		return n <= 0 ? 0f : (n * ColorSwatchSize) + ((n - 1) * ChoicesGap);
	}

	private static int ChoicesPerLine(float width)
	{
		var diameter = ColorSwatchSize;
		var gap = ChoicesGap;
		return Math.Max(1, (int)MathF.Floor((width + gap + 0.5f) / (diameter + gap)));
	}

	private static float ChoicesHeight(int n, float width)
	{
		if (n <= 0)
		{
			return 0f;
		}

		var perLine = ChoicesPerLine(width);
		var lines = (n + perLine - 1) / perLine;
		return (lines * ColorSwatchSize) + ((lines - 1) * ChoicesGap);
	}

	private static bool SameColor(Vector4 a, Vector4 b, bool alpha)
	{
		const float tolerance = 0.003f;
		return MathF.Abs(a.X - b.X) <= tolerance
			&& MathF.Abs(a.Y - b.Y) <= tolerance
			&& MathF.Abs(a.Z - b.Z) <= tolerance
			&& (!alpha || MathF.Abs(a.W - b.W) <= tolerance);
	}

	public static bool ColorChoices(string id, ref Vector4 color, ReadOnlySpan<M3ColorChoice> choices, bool custom = false, bool alpha = false, bool enabled = true, float? maxWidth = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var diameter = ColorSwatchSize;
		var radius = diameter * 0.5f;
		var gap = ChoicesGap;
		var drawList = ImGui.GetWindowDrawList();
		var dim = enabled ? 1f : M3.DisabledContent;
		var perLine = ChoicesPerLine(maxWidth ?? ImGui.GetContentRegionAvail().X);
		var origin = ImGui.GetCursorScreenPos();
		var matched = false;
		var changed = false;

		for (var i = 0; i < choices.Length; i++)
		{
			var choice = choices[i];
			ImGui.SetCursorScreenPos(origin + new Vector2((i % perLine) * (diameter + gap), (i / perLine) * (diameter + gap)));

			var pressed = ImGui.InvisibleButton($"{id}_{i}", new Vector2(diameter)) && enabled;
			var hovered = enabled && ImGui.IsItemHovered();
			var held = enabled && ImGui.IsItemActive();
			var min = ImGui.GetItemRectMin();
			var max = ImGui.GetItemRectMax();
			var center = (min + max) * 0.5f;
			var shown = alpha ? choice.Color : choice.Color with { W = 1f };

			var selected = SameColor(color, choice.Color, alpha);
			matched |= selected;

			if (hovered)
			{
				drawList.AddCircleFilled(center, radius + (6f * scale), M3.U32(s.OnSurface, held ? M3.StatePressed : M3.StateHover), 32);
				ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			}

			drawList.AddCircleFilled(center, radius, M3.U32(shown, shown.W * dim), 32);
			drawList.AddCircle(center, radius, M3.U32(s.Outline, 0.35f * dim), 32, 1f * scale);

			if (selected)
			{
				drawList.AddCircle(center, radius + (3.5f * scale), M3.U32(s.OnSurface, dim), 32, 2f * scale);
				M3Draw.IconCentered(drawList, FontAwesomeIcon.Check, center - new Vector2(radius), center + new Vector2(radius),
					M3.Alpha(M3.ContentOn(choice.Color), dim), 0.8f);
			}

			M3Draw.FocusRing(min, max, radius);

			if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
			{
				M3Tooltip.Show(choice.Name);
			}

			if (pressed && !selected)
			{
				color = shown;
				changed = true;
			}
		}

		if (custom)
		{
			var index = choices.Length;
			var swatchMin = origin + new Vector2((index % perLine) * (diameter + gap), (index / perLine) * (diameter + gap));
			ImGui.SetCursorScreenPos(swatchMin);

			var own = color;
			bool picked;
			using (ImRaii.PushId(id))
			{
				picked = ColorSwatch("custom", ref own, alpha: alpha, enabled: enabled, tooltip: matched ? M3Fork.T("Pick any colour") : M3Fork.T("Your own colour. Click to change it."));
			}

			if (picked)
			{
				color = alpha ? own : own with { W = 1f };
				changed = true;
			}

			if (!matched)
			{
				drawList.AddCircle(swatchMin + new Vector2(radius), radius + (3.5f * scale), M3.U32(s.OnSurface, dim), 32, 2f * scale);
			}
		}

		return changed;
	}

	public static bool RowColorChoices(string label, ref Vector4 color, ReadOnlySpan<M3ColorChoice> choices, string? supporting = null, bool custom = true, bool alpha = false, bool enabled = true)
	{
		var (display, id) = SplitLabel(label);
		var n = choices.Length + (custom ? 1 : 0);
		var width = MathF.Min(ColorChoicesWidth(choices.Length, custom), M3SettingRow.MaxControlWidth());
		var height = ChoicesHeight(n, width);

		var row = M3SettingRow.Begin(display, supporting, new Vector2(width, height), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = ColorChoices($"##{id}_choices", ref color, choices, custom, alpha, enabled, width);
		M3SettingRow.End(row);
		return changed;
	}
}
