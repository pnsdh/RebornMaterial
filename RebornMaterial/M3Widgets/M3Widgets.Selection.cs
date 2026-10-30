using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static partial class M3Widgets
{
	public static Vector2 SwitchSize()
	{
		return new Vector2(52f, 32f) * M3.Scale;
	}

	public static bool Switch(string id, ref bool value, bool enabled = true)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var size = SwitchSize();

		var changed = false;
		if (ImGui.InvisibleButton(id, size) && enabled)
		{
			value = !value;
			changed = true;
		}

		var hovered = enabled && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = enabled && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var drawList = ImGui.GetWindowDrawList();
		var dim = enabled ? 1f : M3.DisabledContent;

		var trackSize = new Vector2(48f, 28f) * scale;
		var trackMin = min + ((size - trackSize) * 0.5f);
		var trackMax = trackMin + trackSize;
		var trackRadius = trackSize.Y * 0.5f;

		var progress = M3Motion.Approach(ImGuiP.GetItemID(), value ? 1f : 0f, M3Motion.FastDuration);

		var trackFill = M3ColorMath.Mix(s.SurfaceContainerHighest, s.Primary, progress);
		var trackOutline = M3ColorMath.Mix(s.Outline, s.Primary, progress);
		if (hovered || held)
		{
			trackFill = M3.StateLayer(trackFill, value ? s.OnPrimary : s.OnSurface, hovered, held);
		}

		drawList.AddRectFilled(trackMin, trackMax, M3.U32(trackFill, dim), trackRadius);
		drawList.AddRect(trackMin, trackMax, M3.U32(trackOutline, 0.9f * dim), trackRadius, ImDrawFlags.None, 2f * scale);

		var thumbDiameter = float.Lerp(16f, 24f, progress) * scale;
		if (held)
		{
			thumbDiameter = 28f * scale;
		}

		var thumbRadius = thumbDiameter * 0.5f;
		var travelStart = trackMin.X + (4f * scale) + (8f * scale);
		var travelEnd = trackMax.X - (4f * scale) - (12f * scale);
		var thumbCenter = new Vector2(float.Lerp(travelStart, travelEnd, progress), trackMin.Y + trackRadius);
		var thumbColor = M3ColorMath.Mix(s.Outline, s.OnPrimary, progress);

		if (hovered || held)
		{
			var halo = value ? s.Primary : s.OnSurface;
			drawList.AddCircleFilled(thumbCenter, 20f * scale, M3.U32(halo, held ? M3.StatePressed : M3.StateHover));
		}

		drawList.AddCircleFilled(thumbCenter, thumbRadius, M3.U32(thumbColor, dim), 24);

		if (progress > 0.55f)
		{
			var tick = thumbRadius * 0.45f;
			var checkColor = M3.U32(s.OnPrimaryContainer, (progress - 0.55f) / 0.45f * dim);
			drawList.AddLine(
				thumbCenter + new Vector2(-tick, 0f),
				thumbCenter + new Vector2(-tick * 0.2f, tick * 0.7f),
				checkColor, 2f * scale);
			drawList.AddLine(
				thumbCenter + new Vector2(-tick * 0.2f, tick * 0.7f),
				thumbCenter + new Vector2(tick, -tick * 0.6f),
				checkColor, 2f * scale);
		}

		M3Draw.FocusRing(trackMin, trackMax, trackRadius);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		return changed;
	}

	public static Vector2 CheckboxSize()
	{
		return new Vector2(20f, 20f) * M3.Scale;
	}

	public static Vector2 CheckboxHitSize()
	{
		return CheckboxSize() + (new Vector2(8f, 8f) * M3.Scale);
	}

	public static bool Checkbox(string id, ref bool value, bool enabled = true, bool mixed = false)
	{
		var hit = CheckboxHitSize();

		var changed = false;
		if (ImGui.InvisibleButton(id, hit) && enabled)
		{
			value = !value;
			changed = true;
		}

		var hovered = enabled && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = enabled && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		DrawCheckbox(ImGui.GetWindowDrawList(), min + (hit * 0.5f), ImGuiP.GetItemID(), value, mixed, hovered, held, enabled);
		M3Draw.FocusRing(min, min + hit, hit.X * 0.5f);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		return changed;
	}

	public static float LabeledCheckboxWidth(string label)
	{
		return LabeledCheckboxWidthOf(SplitLabel(label).Label);
	}

	private static float LabeledCheckboxWidthOf(string display)
	{
		var scale = M3.Scale;
		return CheckboxSize().X + (12f * scale) + ImGui.CalcTextSize(display).X + (8f * scale);
	}

	public static bool LabeledCheckbox(string label, ref bool value, bool enabled = true, bool mixed = false, string? tooltip = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var (display, id) = SplitLabel(label);
		var hit = CheckboxHitSize();
		var size = new Vector2(LabeledCheckboxWidthOf(display), MathF.Max(hit.Y, ImGui.GetFrameHeight()));

		var changed = false;
		if (ImGui.InvisibleButton(id, size) && enabled)
		{
			value = !value;
			changed = true;
		}

		var mouseOver = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var hovered = enabled && mouseOver;
		var held = enabled && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var drawList = ImGui.GetWindowDrawList();
		DrawCheckbox(drawList, new Vector2(min.X + (hit.X * 0.5f), min.Y + (size.Y * 0.5f)), ImGuiP.GetItemID(), value, mixed, hovered, held, enabled);

		var textSize = ImGui.CalcTextSize(display);
		drawList.AddText(new Vector2(min.X + hit.X + (4f * scale), min.Y + ((size.Y - textSize.Y) * 0.5f)),
			M3.U32(s.OnSurface, enabled ? 0.95f : M3.DisabledContent), display);
		M3Draw.FocusRing(min, min + size, M3.ShapeSmall);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (mouseOver)
		{
			M3Tooltip.Show(tooltip);
		}

		return changed;
	}

	private static void DrawCheckbox(ImDrawListPtr drawList, Vector2 center, uint id, bool value, bool mixed, bool hovered, bool held, bool enabled)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var box = CheckboxSize();
		var boxMin = center - (box * 0.5f);
		var boxMax = center + (box * 0.5f);
		var dim = enabled ? 1f : M3.DisabledContent;
		var filled = value || mixed;
		var progress = M3Motion.Approach(id, filled ? 1f : 0f, M3Motion.FastDuration);

		if (hovered || held)
		{
			drawList.AddCircleFilled(center, (box.X + (8f * scale)) * 0.55f,
				M3.U32(filled ? s.Primary : s.OnSurface, held ? M3.StatePressed : M3.StateHover));
		}

		if (progress > 0.01f)
		{
			drawList.AddRectFilled(boxMin, boxMax, M3.U32(enabled ? s.Primary : s.OnSurface, progress * dim), M3.ShapeExtraSmall * 0.5f);
		}

		if (progress < 0.99f)
		{
			drawList.AddRect(boxMin, boxMax, M3.U32(s.OnSurfaceVariant, (1f - progress) * dim), M3.ShapeExtraSmall * 0.5f, ImDrawFlags.None, 2f * scale);
		}

		if (progress <= 0.2f)
		{
			return;
		}

		var tick = box.X * 0.26f;
		var color = M3.U32(enabled ? s.OnPrimary : s.Surface, progress);
		if (mixed)
		{
			drawList.AddLine(center - new Vector2(tick, 0f), center + new Vector2(tick, 0f), color, 2f * scale);
			return;
		}

		drawList.AddLine(center + new Vector2(-tick, 0f), center + new Vector2(-tick * 0.25f, tick * 0.8f), color, 2f * scale);
		drawList.AddLine(center + new Vector2(-tick * 0.25f, tick * 0.8f), center + new Vector2(tick, -tick * 0.7f), color, 2f * scale);
	}

	public static Vector2 RadioSize()
	{
		return new Vector2(28f, 28f) * M3.Scale;
	}

	public static bool RadioButton(string id, bool selected, bool enabled = true)
	{
		var hit = RadioSize();

		var pressed = ImGui.InvisibleButton(id, hit) && enabled;
		var hovered = enabled && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = enabled && ImGui.IsItemActive();
		var center = ImGui.GetItemRectMin() + (hit * 0.5f);
		var progress = M3Motion.Approach(ImGuiP.GetItemID(), selected ? 1f : 0f, M3Motion.FastDuration);

		DrawRadio(ImGui.GetWindowDrawList(), center, hit.X, progress, selected, hovered, held, enabled);
		M3Draw.FocusRing(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), hit.X * 0.5f);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		return pressed;
	}

	public static int RadioGroup(string id, IReadOnlyList<string> options, int selectedIndex, bool horizontal = false, bool enabled = true)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var radio = RadioSize();
		var height = MathF.Max(radio.Y, ImGui.GetTextLineHeight() + (4f * scale));
		var labelGap = 4f * scale;
		var result = -1;

		using var idScope = ImRaii.PushId(id);

		for (var i = 0; i < options.Count; i++)
		{
			var selected = i == selectedIndex;
			var textSize = ImGui.CalcTextSize(options[i]);
			var width = radio.X + labelGap + textSize.X + (12f * scale);

			if (horizontal && i > 0)
			{
				ImGui.SameLine(0f, 8f * scale);
			}

			ImGui.PushID(i);
			var clicked = ImGui.InvisibleButton("##option", new Vector2(width, height));
			ImGui.PopID();
			if (clicked && enabled && !selected)
			{
				result = i;
			}

			var hovered = enabled && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
			var held = enabled && ImGui.IsItemActive();
			var min = ImGui.GetItemRectMin();
			var drawList = ImGui.GetWindowDrawList();
			var center = new Vector2(min.X + (radio.X * 0.5f), min.Y + (height * 0.5f));
			var progress = M3Motion.Approach(ImGuiP.GetItemID(), selected ? 1f : 0f, M3Motion.FastDuration);

			DrawRadio(drawList, center, radio.X, progress, selected, hovered, held, enabled);

			drawList.AddText(new Vector2(min.X + radio.X + labelGap, min.Y + ((height - textSize.Y) * 0.5f)),
				M3.U32(s.OnSurface, enabled ? 0.95f : M3.DisabledContent), options[i]);
			M3Draw.FocusRing(min, ImGui.GetItemRectMax(), M3.ShapeSmall);

			if (hovered)
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			}
		}

		return result;
	}

	private static void DrawRadio(ImDrawListPtr drawList, Vector2 center, float hit, float progress, bool selected, bool hovered, bool held, bool enabled)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var dim = enabled ? 1f : M3.DisabledContent;

		if (hovered || held)
		{
			drawList.AddCircleFilled(center, hit * 0.55f, M3.U32(selected ? s.Primary : s.OnSurface, held ? M3.StatePressed : M3.StateHover), 24);
		}

		var ring = M3ColorMath.Mix(s.OnSurfaceVariant, s.Primary, progress);
		drawList.AddCircle(center, 9f * scale, M3.U32(ring, dim), 32, 2f * scale);

		if (progress > 0.01f)
		{
			drawList.AddCircleFilled(center, 5f * scale * progress, M3.U32(s.Primary, dim), 24);
		}
	}
}
