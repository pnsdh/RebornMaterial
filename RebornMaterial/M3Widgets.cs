using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public readonly record struct M3Segment(
	string Label,
	FontAwesomeIcon Icon = FontAwesomeIcon.None,
	string? Tooltip = null,
	Vector4? Accent = null);

public readonly record struct M3WindowAction(string Id, FontAwesomeIcon Icon, string Tooltip);

public readonly record struct M3WindowBrand(IDalamudTextureWrap? Logo, string Label, FontAwesomeIcon FallbackIcon = FontAwesomeIcon.Fire);

public enum M3ButtonStyle
{
	Filled,

	Tonal,

	Outlined,

	Text,

	Danger,
}

public static partial class M3Widgets
{
	#region Switch

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

		var progress = M3Motion.Approach(ImGui.GetID(id), value ? 1f : 0f, M3Motion.FastDuration);

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
		return changed;
	}

	#endregion

	#region Checkbox

	public static Vector2 CheckboxSize()
	{
		return new Vector2(20f, 20f) * M3.Scale;
	}

	// Mixed draws a dash, for a box that stands for several values that don't agree. A click still flips value.
	public static bool Checkbox(string id, ref bool value, bool enabled = true, bool mixed = false)
	{
		var hit = CheckboxSize() + (new Vector2(8f, 8f) * M3.Scale);

		var changed = false;
		if (ImGui.InvisibleButton(id, hit) && enabled)
		{
			value = !value;
			changed = true;
		}

		var hovered = enabled && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = enabled && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		DrawCheckbox(ImGui.GetWindowDrawList(), min + (hit * 0.5f), ImGui.GetID(id), value, mixed, hovered, held, enabled);
		M3Draw.FocusRing(min, min + hit, hit.X * 0.5f);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		return changed;
	}

	public static float LabeledCheckboxWidth(string label)
	{
		var scale = M3.Scale;
		return CheckboxSize().X + (12f * scale) + ImGui.CalcTextSize(SplitLabel(label).Label).X + (8f * scale);
	}

	// The label is part of the click target. "Label##id" hides the id part as usual.
	public static bool LabeledCheckbox(string label, ref bool value, bool enabled = true, bool mixed = false, string? tooltip = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var (display, id) = SplitLabel(label);
		var hit = CheckboxSize() + (new Vector2(8f, 8f) * scale);
		var size = new Vector2(LabeledCheckboxWidth(label), MathF.Max(hit.Y, ImGui.GetFrameHeight()));

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
		DrawCheckbox(drawList, new Vector2(min.X + (hit.X * 0.5f), min.Y + (size.Y * 0.5f)), ImGui.GetID(id), value, mixed, hovered, held, enabled);

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

	#endregion

	#region Radio buttons

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
		var progress = M3Motion.Approach(ImGui.GetID(id), selected ? 1f : 0f, M3Motion.FastDuration);

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

			if (ImGui.InvisibleButton($"##option{i}", new Vector2(width, height)) && enabled && !selected)
			{
				result = i;
			}

			var hovered = enabled && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
			var held = enabled && ImGui.IsItemActive();
			var min = ImGui.GetItemRectMin();
			var drawList = ImGui.GetWindowDrawList();
			var center = new Vector2(min.X + (radio.X * 0.5f), min.Y + (height * 0.5f));
			var progress = M3Motion.Approach(ImGui.GetID($"##option{i}"), selected ? 1f : 0f, M3Motion.FastDuration);

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

	#endregion

	#region Buttons

	public static float ButtonHeight => M3.FitText(40f, 8f);

	private static float TextureIconSize => MathF.Max(18f * M3.Scale, ImGui.GetTextLineHeight());

	private static float IconSlotWidth(FontAwesomeIcon icon, IDalamudTextureWrap? texture)
	{
		return texture != null ? TextureIconSize : icon == FontAwesomeIcon.None ? 0f : M3Draw.MeasureIcon(icon).X;
	}

	// A texture wins over the icon. While the texture is still loading, the icon shows in its place.
	private static void DrawIconSlot(ImDrawListPtr drawList, FontAwesomeIcon icon, IDalamudTextureWrap? texture, float x, float top, float height, Vector4 color, float alpha = 1f)
	{
		if (texture != null)
		{
			var size = TextureIconSize;
			if (M3Draw.Texture(drawList, texture, new Vector2(x, top + ((height - size) * 0.5f)), size, alpha))
			{
				return;
			}
		}

		if (icon != FontAwesomeIcon.None)
		{
			var iconSize = M3Draw.MeasureIcon(icon);
			M3Draw.Icon(drawList, icon, new Vector2(x, top + ((height - iconSize.Y) * 0.5f)), color);
		}
	}

	public static float ButtonWidth(FontAwesomeIcon icon, string label, IDalamudTextureWrap? texture = null)
	{
		var scale = M3.Scale;
		var width = 24f * scale * 2f;
		var iconWidth = IconSlotWidth(icon, texture);
		if (iconWidth > 0f)
		{
			width += iconWidth + (8f * scale);
		}

		if (!string.IsNullOrEmpty(label))
		{
			width += ImGui.CalcTextSize(label).X;
		}

		return MathF.Max(width, 64f * scale);
	}

	private static (Vector4 Container, Vector4 Content, Vector4? Outline) ButtonColors(M3ButtonStyle style, Vector4? accent)
	{
		var s = M3.Scheme;
		var clear = M3.Alpha(s.Surface, 0f);

		if (accent is { } tone && style != M3ButtonStyle.Danger)
		{
			var roles = M3.CustomColor(tone, harmonize: false);
			return style switch
			{
				M3ButtonStyle.Filled => (roles.Color, roles.OnColor, null),
				M3ButtonStyle.Tonal => (roles.Container, roles.OnContainer, null),
				M3ButtonStyle.Outlined => (clear, roles.Color, M3.Alpha(roles.Color, 0.7f)),
				_ => (clear, roles.Color, null),
			};
		}

		return style switch
		{
			M3ButtonStyle.Filled => (s.Primary, s.OnPrimary, null),
			M3ButtonStyle.Tonal => (s.SecondaryContainer, s.OnSecondaryContainer, null),
			M3ButtonStyle.Outlined => (clear, s.Primary, s.Outline),
			M3ButtonStyle.Danger => (s.ErrorContainer, s.OnErrorContainer, null),
			_ => (clear, s.Primary, null),
		};
	}

	// The tooltip also shows while disabled, so it can say why.
	public static bool Button(string id, string label, M3ButtonStyle style = M3ButtonStyle.Tonal, FontAwesomeIcon icon = FontAwesomeIcon.None, float? width = null, bool enabled = true, string? tooltip = null, Vector4? accent = null, IDalamudTextureWrap? texture = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = ButtonHeight;
		var size = new Vector2(width ?? ButtonWidth(icon, label, texture), height);

		var pressed = ImGui.InvisibleButton(id, size) && enabled;
		var mouseOver = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var hovered = enabled && mouseOver;
		var held = enabled && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = height * 0.5f;

		var (container, content, outline) = ButtonColors(style, accent);

		if (!enabled)
		{
			container = container.W > 0f ? M3.Alpha(s.OnSurface, M3.DisabledContainer) : container;
			content = M3.Alpha(s.OnSurface, M3.DisabledContent);
			outline = outline is null ? null : M3.Alpha(s.OnSurface, M3.DisabledContainer);
		}
		else if (hovered || held)
		{
			container = container.W > 0f
				? M3.StateLayer(container, content, hovered, held)
				: M3.Alpha(content, held ? M3.StatePressed : M3.StateHover);
		}

		M3Draw.Container(drawList, min, max, container, rounding, outline, 1f);

		var iconWidth = IconSlotWidth(icon, texture);
		var gap = iconWidth <= 0f || string.IsNullOrEmpty(label) ? 0f : 8f * scale;
		var textSize = string.IsNullOrEmpty(label) ? Vector2.Zero : ImGui.CalcTextSize(label);
		var contentWidth = iconWidth + gap + textSize.X;
		var cursorX = min.X + ((size.X - contentWidth) * 0.5f);

		if (iconWidth > 0f)
		{
			DrawIconSlot(drawList, icon, texture, cursorX, min.Y, height, content, enabled ? 1f : M3.DisabledContent);
			cursorX += iconWidth + gap;
		}

		if (!string.IsNullOrEmpty(label))
		{
			drawList.AddText(new Vector2(cursorX, min.Y + ((height - textSize.Y) * 0.5f)), M3.U32(content), label);
		}

		M3Draw.FocusRing(min, max, rounding);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (mouseOver)
		{
			M3Tooltip.Show(tooltip);
		}

		return pressed;
	}

	private static readonly Dictionary<uint, float> _holdProgress = [];

	// Fills while held and returns true once it's full, so a risky action can't fire from a stray click.
	public static bool HoldButton(string id, string label, float seconds = 1f, M3ButtonStyle style = M3ButtonStyle.Danger, FontAwesomeIcon icon = FontAwesomeIcon.None, float? width = null, bool enabled = true, string? tooltip = null)
	{
		_ = Button(id, label, style, icon, width, enabled, tooltip ?? M3Fork.T("Hold to confirm"));
		var key = ImGui.GetID(id);
		var held = enabled && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var delta = ImGui.GetIO().DeltaTime;

		// Below zero means it finished and waits for the button to be let go.
		_holdProgress.TryGetValue(key, out var progress);
		var done = false;
		if (held && progress >= 0f)
		{
			progress += delta / MathF.Max(0.05f, seconds);
			if (progress >= 1f)
			{
				done = true;
				progress = -1f;
			}
		}
		else if (!held)
		{
			progress = progress < 0f ? 0f : MathF.Max(0f, progress - (delta * 4f));
		}

		_holdProgress[key] = progress;

		var shown = progress < 0f ? 1f : progress;
		if (shown > 0f)
		{
			var content = ButtonColors(style, null).Content;
			var drawList = ImGui.GetWindowDrawList();
			drawList.PushClipRect(min, new Vector2(float.Lerp(min.X, max.X, shown), max.Y), true);
			drawList.AddRectFilled(min, max, M3.U32(content, 0.22f), (max.Y - min.Y) * 0.5f);
			drawList.PopClipRect();
		}

		return done;
	}

	public static float IconButtonSize => 36f * M3.Scale;

	public static bool IconButton(string id, FontAwesomeIcon icon, string? tooltip = null, M3ButtonStyle style = M3ButtonStyle.Text, Vector4? tint = null, float? diameter = null, bool enabled = true)
	{
		var s = M3.Scheme;
		var size = Vector2.One * (diameter ?? IconButtonSize);

		var pressed = ImGui.InvisibleButton(id, size) && enabled;
		var mouseOver = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var hovered = enabled && mouseOver;
		var held = enabled && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();

		var (container, content) = style switch
		{
			M3ButtonStyle.Filled => (s.Primary, s.OnPrimary),
			M3ButtonStyle.Tonal => (s.SecondaryContainer, s.OnSecondaryContainer),
			M3ButtonStyle.Danger => (s.ErrorContainer, s.OnErrorContainer),
			M3ButtonStyle.Outlined => (M3.Alpha(s.Surface, 0f), s.OnSurfaceVariant),
			_ => (M3.Alpha(s.Surface, 0f), tint ?? s.OnSurfaceVariant),
		};

		if (tint is { } explicitTint)
		{
			content = explicitTint;
		}

		DrawIconButton(drawList, icon, min, max, container, content, style == M3ButtonStyle.Outlined, hovered, held, enabled);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (mouseOver)
		{
			M3Tooltip.Show(tooltip);
		}

		return pressed;
	}

	private static void DrawIconButton(ImDrawListPtr drawList, FontAwesomeIcon icon, Vector2 min, Vector2 max, Vector4 container, Vector4 content, bool outlined, bool hovered, bool held, bool enabled)
	{
		var s = M3.Scheme;
		var center = (min + max) * 0.5f;
		var radius = (max.X - min.X) * 0.5f;

		if (!enabled)
		{
			container = container.W > 0f ? M3.Alpha(s.OnSurface, M3.DisabledContainer) : container;
			content = M3.Alpha(s.OnSurface, M3.DisabledContent);
		}

		if (container.W > 0f)
		{
			var fill = hovered || held ? M3.StateLayer(container, content, hovered, held) : container;
			drawList.AddCircleFilled(center, radius, M3.U32(fill), 32);
		}
		else if (hovered || held)
		{
			drawList.AddCircleFilled(center, radius, M3.U32(content, held ? M3.StatePressed : M3.StateHover), 32);
		}

		if (outlined)
		{
			drawList.AddCircle(center, radius, M3.U32(enabled ? s.Outline : s.OnSurface, enabled ? 0.8f : M3.DisabledContainer), 32, 1f * M3.Scale);
		}

		M3Draw.IconCentered(drawList, icon, min, max, content);
		M3Draw.FocusRing(min, max, radius);
	}

	public static bool IconToggle(string id, FontAwesomeIcon icon, ref bool selected, string? tooltip = null, M3ButtonStyle style = M3ButtonStyle.Text, FontAwesomeIcon selectedIcon = FontAwesomeIcon.None, float? diameter = null, bool enabled = true)
	{
		var s = M3.Scheme;
		var size = Vector2.One * (diameter ?? IconButtonSize);

		var pressed = ImGui.InvisibleButton(id, size) && enabled;
		if (pressed)
		{
			selected = !selected;
		}

		var mouseOver = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var hovered = enabled && mouseOver;
		var held = enabled && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();

		var (container, content) = (style, selected) switch
		{
			(M3ButtonStyle.Filled, true) => (s.Primary, s.OnPrimary),
			(M3ButtonStyle.Filled, false) => (s.SurfaceContainerHighest, s.Primary),
			(M3ButtonStyle.Tonal, true) => (s.SecondaryContainer, s.OnSecondaryContainer),
			(M3ButtonStyle.Tonal, false) => (s.SurfaceContainerHighest, s.OnSurfaceVariant),
			(M3ButtonStyle.Outlined, true) => (s.InverseSurface, s.InverseOnSurface),
			(M3ButtonStyle.Danger, true) => (s.ErrorContainer, s.OnErrorContainer),
			(_, true) => (M3.Alpha(s.Surface, 0f), s.Primary),
			_ => (M3.Alpha(s.Surface, 0f), s.OnSurfaceVariant),
		};

		var glyph = selected && selectedIcon != FontAwesomeIcon.None ? selectedIcon : icon;
		DrawIconButton(ImGui.GetWindowDrawList(), glyph, min, max, container, content, style == M3ButtonStyle.Outlined && !selected, hovered, held, enabled);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (mouseOver)
		{
			M3Tooltip.Show(tooltip);
		}

		return pressed;
	}

	#endregion

	#region Window actions

	private static float WindowActionsPadding => 4f * M3.Scale;
	private static float WindowActionsGap => 2f * M3.Scale;
	private static float WindowActionsDividerGap => 8f * M3.Scale;
	private static float WindowBrandLogoSize => 24f * M3.Scale;

	public static Vector2 WindowActionsSize(int actionCount)
	{
		return WindowActionsSize(ActionsWidth(actionCount), 1);
	}

	public static Vector2 WindowActionsSize(int actionCount, in M3WindowBrand brand, float minimized)
	{
		return WindowActionsSize(LeadingWidth(actionCount, brand, minimized), 2);
	}

	public static int WindowActions(string id, Vector2 topRight, ReadOnlySpan<M3WindowAction> actions, out bool closed, Vector4? fill = null, string closeTooltip = "Close")
	{
		return DrawWindowActions(id, topRight, actions, null, 0f, out _, out closed, fill, closeTooltip);
	}

	public static int WindowActions(string id, Vector2 topRight, ReadOnlySpan<M3WindowAction> actions, in M3WindowBrand brand, float minimized, out bool toggled, out bool closed, Vector4? fill = null, string closeTooltip = "Close")
	{
		return DrawWindowActions(id, topRight, actions, brand, minimized, out toggled, out closed, fill, closeTooltip);
	}

	private static int DrawWindowActions(string id, Vector2 topRight, ReadOnlySpan<M3WindowAction> actions, M3WindowBrand? brand, float minimized, out bool toggled, out bool closed, Vector4? fill, string closeTooltip)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var padding = WindowActionsPadding;
		var gap = WindowActionsGap;
		var buttonSize = IconButtonSize;

		minimized = brand is null ? 0f : Math.Clamp(minimized, 0f, 1f);
		var leading = brand is { } measured ? LeadingWidth(actions.Length, measured, minimized) : ActionsWidth(actions.Length);
		var size = WindowActionsSize(leading, brand is null ? 1 : 2);

		var min = new Vector2(topRight.X - size.X, topRight.Y);
		var drawList = ImGui.GetWindowDrawList();
		drawList.AddRectFilled(min, min + size, M3.U32(fill ?? M3.Alpha(s.Surface, 0.72f)), M3.ShapeFull);

		using var idScope = ImRaii.PushId(id);
		var result = -1;
		var x = min.X + padding;
		var y = min.Y + padding;

		ImGui.PushClipRect(new Vector2(x, y), new Vector2(x + leading, y + buttonSize), true);
		var actionsAlpha = Math.Clamp(1f - (minimized * 2f), 0f, 1f);
		if (actionsAlpha > 0f)
		{
			using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * actionsAlpha);
			var actionX = x;
			for (var i = 0; i < actions.Length; i++)
			{
				var action = actions[i];
				ImGui.SetCursorScreenPos(new Vector2(actionX, y));
				if (IconButton(action.Id, action.Icon, action.Tooltip))
				{
					result = i;
				}

				actionX += buttonSize + gap;
			}
		}

		if (brand is { } shown && minimized > 0.5f)
		{
			using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * ((minimized * 2f) - 1f));
			DrawWindowBrand(drawList, shown, new Vector2(x, y), buttonSize);
		}

		ImGui.PopClipRect();
		x += leading;

		var dividerRoom = DividerRoom(leading);
		if (dividerRoom > 0f)
		{
			x += dividerRoom;
			drawList.AddLine(new Vector2(x, y + (8f * scale)), new Vector2(x, y + buttonSize - (8f * scale)),
				M3.U32(s.OutlineVariant, 0.9f * dividerRoom / WindowActionsDividerGap), 1f * scale);
			x += dividerRoom;
		}

		toggled = false;
		if (brand is not null)
		{
			ImGui.SetCursorScreenPos(new Vector2(x, y));
			toggled = CaretButton("##minimize", -MathF.PI * 0.5f * minimized, minimized < 0.5f ? M3Fork.T("Minimize") : M3Fork.T("Restore"));
			x += buttonSize + gap;
		}

		ImGui.SetCursorScreenPos(new Vector2(x, y));
		closed = IconButton("##close", FontAwesomeIcon.Times, closeTooltip);
		return result;
	}

	private static Vector2 WindowActionsSize(float leading, int controls)
	{
		var padding = WindowActionsPadding;
		var width = (padding * 2f) + leading + (DividerRoom(leading) * 2f)
			+ (IconButtonSize * controls) + (WindowActionsGap * (controls - 1));
		return new Vector2(width, IconButtonSize + (padding * 2f));
	}

	private static float DividerRoom(float leading)
	{
		return WindowActionsDividerGap * Math.Clamp(leading / WindowActionsDividerGap, 0f, 1f);
	}

	private static float ActionsWidth(int count)
	{
		return count <= 0 ? 0f : (IconButtonSize * count) + (WindowActionsGap * (count - 1));
	}

	private static float LeadingWidth(int actionCount, in M3WindowBrand brand, float minimized)
	{
		return float.Lerp(ActionsWidth(actionCount), BrandWidth(brand), Math.Clamp(minimized, 0f, 1f));
	}

	private static float BrandWidth(in M3WindowBrand brand)
	{
		var scale = M3.Scale;
		using var font = ImRaii.PushFont(M3.TitleMedium);
		return (6f * scale) + WindowBrandLogoSize + (8f * scale) + ImGui.CalcTextSize(brand.Label).X + (4f * scale);
	}

	private static void DrawWindowBrand(ImDrawListPtr drawList, in M3WindowBrand brand, Vector2 origin, float height)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var logoSize = WindowBrandLogoSize;
		var logoMin = new Vector2(origin.X + (6f * scale), origin.Y + ((height - logoSize) * 0.5f));
		var logoMax = logoMin + new Vector2(logoSize, logoSize);

		var logo = brand.Logo;
		if (logo?.Handle != null)
		{
			drawList.AddImageRounded(logo.Handle, logoMin, logoMax, Vector2.Zero, Vector2.One, M3.U32(Vector4.One), M3.ShapeExtraSmall);
		}
		else
		{
			drawList.AddRectFilled(logoMin, logoMax, M3.U32(s.PrimaryContainer), M3.ShapeExtraSmall);
			M3Draw.IconCentered(drawList, brand.FallbackIcon, logoMin, logoMax, s.OnPrimaryContainer);
		}

		using var font = ImRaii.PushFont(M3.TitleMedium);
		var labelSize = ImGui.CalcTextSize(brand.Label);
		drawList.AddText(new Vector2(logoMax.X + (8f * scale), origin.Y + ((height - labelSize.Y) * 0.5f)), M3.U32(s.OnSurface), brand.Label);
	}

	private static bool CaretButton(string id, float angle, string? tooltip)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var size = Vector2.One * IconButtonSize;

		var pressed = ImGui.InvisibleButton(id, size);
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = ImGui.IsItemActive();
		var center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * 0.5f;
		var drawList = ImGui.GetWindowDrawList();
		var content = s.OnSurfaceVariant;

		if (hovered || held)
		{
			drawList.AddCircleFilled(center, size.X * 0.5f, M3.U32(content, held ? M3.StatePressed : M3.StateHover), 32);
		}

		// Wound clockwise, which ImGui's anti-aliased fill needs.
		var halfWidth = 5f * scale;
		var halfHeight = 3.5f * scale;
		var (sin, cos) = MathF.SinCos(angle);
		Vector2 Turn(float px, float py) => center + new Vector2((px * cos) - (py * sin), (px * sin) + (py * cos));
		drawList.AddTriangleFilled(Turn(0f, halfHeight), Turn(-halfWidth, -halfHeight), Turn(halfWidth, -halfHeight), M3.U32(content));
		M3Draw.FocusRing(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), size.X * 0.5f);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			M3Tooltip.Show(tooltip);
		}

		return pressed;
	}

	#endregion

	#region Segmented buttons

	public static float SegmentedHeight => M3.FitText(32f, 6f);

	// Measure with the same label font the buttons will use, or a smaller font gets the width of the body one.
	public static float SegmentedWidth(ReadOnlySpan<M3Segment> segments, ImFontPtr? labelFont = null)
	{
		var scale = M3.Scale;
		var widest = 0f;

		using var font = ImRaii.PushFont(labelFont.GetValueOrDefault(), labelFont.HasValue);
		foreach (var segment in segments)
		{
			var width = ImGui.CalcTextSize(segment.Label).X + (14f * scale * 2f);
			if (segment.Icon != FontAwesomeIcon.None)
			{
				width += M3Draw.MeasureIcon(segment.Icon).X + (6f * scale);
			}

			widest = MathF.Max(widest, width);
		}

		return widest * segments.Length;
	}

	// A label font only changes the text; the buttons keep their height and icons.
	public static int SegmentedButtons(string id, ReadOnlySpan<M3Segment> segments, int selectedIndex, float? width = null, ImFontPtr? labelFont = null, bool enabled = true)
	{
		if (segments.Length == 0)
		{
			return -1;
		}

		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = SegmentedHeight;
		var total = width ?? SegmentedWidth(segments, labelFont);
		var segmentWidth = total / segments.Length;
		var rounding = height * 0.5f;
		var origin = ImGui.GetCursorScreenPos();
		var drawList = ImGui.GetWindowDrawList();
		var result = -1;

		using var idScope = ImRaii.PushId(id);

		for (var i = 0; i < segments.Length; i++)
		{
			var segment = segments[i];
			var selected = i == selectedIndex;
			var segmentMin = new Vector2(origin.X + (segmentWidth * i), origin.Y);

			ImGui.SetCursorScreenPos(segmentMin);
			if (ImGui.InvisibleButton($"##segment{i}", new Vector2(segmentWidth, height)) && !selected && enabled)
			{
				result = i;
			}

			var mouseOver = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
			var hovered = enabled && mouseOver;
			var held = enabled && ImGui.IsItemActive();
			var segmentMax = segmentMin + new Vector2(segmentWidth, height);
			var tone = enabled ? segment.Accent ?? s.Primary : M3.Alpha(s.OnSurface, M3.DisabledContent);

			var corners = segments.Length == 1 ? ImDrawFlags.RoundCornersAll
				: i == 0 ? ImDrawFlags.RoundCornersLeft
				: i == segments.Length - 1 ? ImDrawFlags.RoundCornersRight
				: ImDrawFlags.RoundCornersNone;

			if (selected)
			{
				drawList.AddRectFilled(segmentMin, segmentMax,
					enabled ? M3.U32(s.SecondaryContainer, 0.95f) : M3.U32(s.OnSurface, M3.DisabledContainer), rounding, corners);
			}

			if (hovered || held)
			{
				drawList.AddRectFilled(segmentMin, segmentMax,
					M3.U32(selected ? tone : s.OnSurface, held ? M3.StatePressed : M3.StateHover), rounding, corners);
				ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			}

			var content = selected ? tone
				: enabled ? M3.Alpha(s.OnSurfaceVariant, hovered ? 1f : 0.85f)
				: M3.Alpha(s.OnSurface, M3.DisabledContent);
			var iconWidth = segment.Icon == FontAwesomeIcon.None ? 0f : M3Draw.MeasureIcon(segment.Icon).X;
			var gap = segment.Icon == FontAwesomeIcon.None ? 0f : 6f * scale;
			var iconTop = segmentMin.Y + ((height - ImGui.GetTextLineHeight()) * 0.5f);

			using (ImRaii.PushFont(labelFont.GetValueOrDefault(), labelFont.HasValue))
			{
				var textSize = ImGui.CalcTextSize(segment.Label);
				var cursorX = segmentMin.X + ((segmentWidth - iconWidth - gap - textSize.X) * 0.5f);

				if (segment.Icon != FontAwesomeIcon.None)
				{
					M3Draw.Icon(drawList, segment.Icon, new Vector2(cursorX, iconTop), content);
					cursorX += iconWidth + gap;
				}

				drawList.AddText(new Vector2(cursorX, segmentMin.Y + ((height - textSize.Y) * 0.5f)), M3.U32(content), segment.Label);
			}

			if (i > 0)
			{
				drawList.AddLine(segmentMin + new Vector2(0f, 1f * scale), new Vector2(segmentMin.X, segmentMax.Y - (1f * scale)),
					M3.U32(s.Outline, 0.55f), 1f * scale);
			}

			M3Draw.FocusRing(segmentMin, segmentMax, rounding);

			if (mouseOver)
			{
				M3Tooltip.Show(segment.Tooltip);
			}
		}

		drawList.AddRect(origin, origin + new Vector2(total, height), M3.U32(s.Outline, 0.65f),
			rounding, ImDrawFlags.RoundCornersAll, 1f * scale);

		ImGui.SetCursorScreenPos(origin);
		ImGui.Dummy(new Vector2(total, height));
		return result;
	}

	#endregion

	#region Chips and pills

	public static float ChipHeight => M3.FitText(32f, 6f);

	public static float ChipWidth(string label, FontAwesomeIcon icon = FontAwesomeIcon.None, FontAwesomeIcon trailingIcon = FontAwesomeIcon.None, IDalamudTextureWrap? texture = null)
	{
		var scale = M3.Scale;
		var width = ImGui.CalcTextSize(label).X + (16f * scale * 2f);
		var iconWidth = IconSlotWidth(icon, texture);
		if (iconWidth > 0f)
		{
			width += iconWidth + (8f * scale);
		}

		if (trailingIcon != FontAwesomeIcon.None)
		{
			width += M3Draw.MeasureIcon(trailingIcon).X + (8f * scale);
		}

		return width;
	}

	public static bool Chip(string id, string label, bool selected, FontAwesomeIcon icon = FontAwesomeIcon.None, string? tooltip = null, Vector4? accent = null, FontAwesomeIcon trailingIcon = FontAwesomeIcon.None, bool enabled = true, IDalamudTextureWrap? texture = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = ChipHeight;
		var size = new Vector2(ChipWidth(label, icon, trailingIcon, texture), height);
		var tone = accent ?? s.Primary;

		var pressed = ImGui.InvisibleButton(id, size) && enabled;
		var mouseOver = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var hovered = enabled && mouseOver;
		var held = enabled && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = M3.ShapeSmall;

		var container = selected ? s.SecondaryContainer : M3.Alpha(s.Surface, 0f);
		var content = selected ? s.OnSecondaryContainer : s.OnSurfaceVariant;
		var outline = selected ? (Vector4?)null : M3.Alpha(s.Outline, 0.8f);
		if (!enabled)
		{
			container = selected ? M3.Alpha(s.OnSurface, M3.DisabledContainer) : container;
			content = M3.Alpha(s.OnSurface, M3.DisabledContent);
			tone = content;
			outline = selected ? null : M3.Alpha(s.OnSurface, M3.DisabledContainer);
		}
		else if (hovered || held)
		{
			container = container.W > 0f
				? M3.StateLayer(container, content, hovered, held)
				: M3.Alpha(s.OnSurface, held ? M3.StatePressed : M3.StateHover);
		}

		M3Draw.Container(drawList, min, max, container, rounding, outline, 1f);

		var iconWidth = IconSlotWidth(icon, texture);
		var gap = iconWidth > 0f ? 8f * scale : 0f;
		var trailingWidth = trailingIcon == FontAwesomeIcon.None ? 0f : M3Draw.MeasureIcon(trailingIcon).X + (8f * scale);
		var textSize = ImGui.CalcTextSize(label);
		var cursorX = min.X + ((size.X - (iconWidth + gap + textSize.X + trailingWidth)) * 0.5f);

		if (iconWidth > 0f)
		{
			DrawIconSlot(drawList, icon, texture, cursorX, min.Y, height, selected ? tone : content, enabled ? 1f : M3.DisabledContent);
			cursorX += iconWidth + gap;
		}

		drawList.AddText(new Vector2(cursorX, min.Y + ((height - textSize.Y) * 0.5f)), M3.U32(content), label);

		if (trailingIcon != FontAwesomeIcon.None)
		{
			var trailingSize = M3Draw.MeasureIcon(trailingIcon);
			M3Draw.Icon(drawList, trailingIcon,
				new Vector2(cursorX + textSize.X + (8f * scale), min.Y + ((height - trailingSize.Y) * 0.5f)), content);
		}

		M3Draw.FocusRing(min, max, rounding);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (mouseOver)
		{
			M3Tooltip.Show(tooltip);
		}

		return pressed;
	}

	public static Vector2 PillSize(string label, FontAwesomeIcon icon = FontAwesomeIcon.None)
	{
		var scale = M3.Scale;
		var width = ImGui.CalcTextSize(label).X + (12f * scale * 2f);
		width += icon == FontAwesomeIcon.None
			? (6f * scale) + (6f * scale)
			: M3Draw.MeasureIcon(icon).X + (6f * scale);
		return new Vector2(width, M3.FitText(24f, 3f));
	}

	public static bool Pill(string id, string label, Vector4 accent, FontAwesomeIcon icon = FontAwesomeIcon.None, string? tooltip = null, bool interactive = false)
	{
		var scale = M3.Scale;
		var size = PillSize(label, icon);

		var clicked = false;
		if (interactive)
		{
			clicked = ImGui.InvisibleButton(id, size);
		}
		else
		{
			ImGui.Dummy(size);
		}

		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = size.Y * 0.5f;

		drawList.AddRectFilled(min, max, M3.U32(accent, hovered ? 0.20f : 0.13f), rounding);
		drawList.AddRect(min, max, M3.U32(accent, hovered ? 0.62f : 0.40f), rounding, ImDrawFlags.None, 1f * scale);

		var cursorX = min.X + (12f * scale);
		if (icon == FontAwesomeIcon.None)
		{
			var radius = 3f * scale;
			drawList.AddCircleFilled(new Vector2(cursorX + radius, min.Y + (size.Y * 0.5f)), radius, M3.U32(accent), 12);
			cursorX += (radius * 2f) + (6f * scale);
		}
		else
		{
			var glyphSize = M3Draw.MeasureIcon(icon);
			M3Draw.Icon(drawList, icon, new Vector2(cursorX, min.Y + ((size.Y - glyphSize.Y) * 0.5f)), accent);
			cursorX += glyphSize.X + (6f * scale);
		}

		var textSize = ImGui.CalcTextSize(label);
		drawList.AddText(new Vector2(cursorX, min.Y + ((size.Y - textSize.Y) * 0.5f)), M3.U32(M3.Scheme.OnSurface, 0.95f), label);

		if (interactive)
		{
			M3Draw.FocusRing(min, max, rounding);
		}

		if (hovered)
		{
			M3Tooltip.Show(tooltip);
		}

		return clicked;
	}

	private static float InputChipRemoveWidth => ChipHeight;

	public static float InputChipWidth(string label, FontAwesomeIcon icon = FontAwesomeIcon.None)
	{
		var scale = M3.Scale;
		var width = (12f * scale) + ImGui.CalcTextSize(label).X + InputChipRemoveWidth;
		if (icon != FontAwesomeIcon.None)
		{
			width += M3Draw.MeasureIcon(icon).X + (8f * scale);
		}

		return width;
	}

	public static bool InputChip(string id, string label, out bool removed, FontAwesomeIcon icon = FontAwesomeIcon.None, string? tooltip = null, Vector4? accent = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = ChipHeight;
		var width = InputChipWidth(label, icon);
		var removeWidth = InputChipRemoveWidth;
		var tone = accent ?? s.Primary;

		var pressed = ImGui.InvisibleButton(id, new Vector2(width - removeWidth, height));
		var bodyHovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var bodyHeld = ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = min + new Vector2(width, height);
		M3Draw.FocusRing(min, max, M3.ShapeSmall);

		ImGui.SameLine(0f, 0f);
		removed = ImGui.InvisibleButton($"{id}_remove", new Vector2(removeWidth, height));
		var removeHovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var removeHeld = ImGui.IsItemActive();
		M3Draw.FocusRing(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), height * 0.5f);

		var drawList = ImGui.GetWindowDrawList();
		var rounding = M3.ShapeSmall;
		var content = s.OnSurfaceVariant;

		var container = M3.Alpha(s.Surface, 0f);
		if (bodyHovered || bodyHeld || removeHovered || removeHeld)
		{
			container = M3.Alpha(s.OnSurface, bodyHeld || removeHeld ? M3.StatePressed : M3.StateHover);
		}

		M3Draw.Container(drawList, min, max, container, rounding, M3.Alpha(s.Outline, 0.8f), 1f);

		var cursorX = min.X + (12f * scale);
		if (icon != FontAwesomeIcon.None)
		{
			var iconSize = M3Draw.MeasureIcon(icon);
			M3Draw.Icon(drawList, icon, new Vector2(cursorX, min.Y + ((height - iconSize.Y) * 0.5f)), tone);
			cursorX += iconSize.X + (8f * scale);
		}

		var textSize = ImGui.CalcTextSize(label);
		drawList.AddText(new Vector2(cursorX, min.Y + ((height - textSize.Y) * 0.5f)), M3.U32(content), label);

		var removeMin = new Vector2(max.X - removeWidth, min.Y);
		var removeCenter = removeMin + (new Vector2(removeWidth, height) * 0.5f);
		if (removeHovered || removeHeld)
		{
			drawList.AddCircleFilled(removeCenter, height * 0.36f, M3.U32(s.OnSurface, removeHeld ? M3.StatePressed : M3.StateHover), 24);
		}

		var arm = 4f * scale;
		var crossColor = M3.U32(removeHovered ? s.OnSurface : content);
		drawList.AddLine(removeCenter - new Vector2(arm, arm), removeCenter + new Vector2(arm, arm), crossColor, 1.5f * scale);
		drawList.AddLine(removeCenter + new Vector2(-arm, arm), removeCenter + new Vector2(arm, -arm), crossColor, 1.5f * scale);

		if (bodyHovered || removeHovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (removeHovered)
		{
			M3Tooltip.Show($"Remove {label}");
		}
		else if (bodyHovered)
		{
			M3Tooltip.Show(tooltip);
		}

		return pressed;
	}

	public static Vector2 BadgeSize(string? text)
	{
		var scale = M3.Scale;
		if (string.IsNullOrEmpty(text))
		{
			return new Vector2(6f, 6f) * scale;
		}

		using var font = ImRaii.PushFont(M3.LabelSmall);
		var textSize = ImGui.CalcTextSize(text);
		var height = MathF.Max(16f * scale, textSize.Y + (2f * scale));
		return new Vector2(MathF.Max(height, textSize.X + (8f * scale)), height);
	}

	public static string BadgeCount(int count)
	{
		return count > 999 ? "999+" : count.ToString();
	}

	public static void Badge(string? text = null, Vector4? color = null)
	{
		var size = BadgeSize(text);
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		BadgeAt(new Vector2(max.X - (size.X * 0.5f), min.Y + (size.Y * 0.5f)), text, color);
	}

	public static void BadgeAt(Vector2 center, string? text = null, Vector4? color = null)
	{
		var s = M3.Scheme;
		var drawList = ImGui.GetWindowDrawList();
		var fill = color ?? s.Error;
		var size = BadgeSize(text);

		if (string.IsNullOrEmpty(text))
		{
			drawList.AddCircleFilled(center, size.X * 0.5f, M3.U32(fill), 12);
			return;
		}

		var min = center - (size * 0.5f);
		drawList.AddRectFilled(min, min + size, M3.U32(fill), size.Y * 0.5f);

		using var font = ImRaii.PushFont(M3.LabelSmall);
		var textSize = ImGui.CalcTextSize(text);

		var onFill = color is null ? s.OnError : M3.ContentOn(fill);
		drawList.AddText(center - (textSize * 0.5f), M3.U32(onFill), text);
	}

	#endregion

	#region Progress

	public static void LinearProgress(Vector2 size, float fraction, float? marker = null, Vector4? markerColor = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;

		ImGui.Dummy(size);
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = size.Y * 0.5f;
		var centerY = (min.Y + max.Y) * 0.5f;

		fraction = Math.Clamp(fraction, 0f, 1f);
		var split = float.Lerp(min.X, max.X, fraction);

		var gap = fraction > 0f && fraction < 1f ? MathF.Min(4f * scale, size.Y) : 0f;
		var trackStart = MathF.Min(max.X, split + gap);
		if (max.X - trackStart > 0.5f)
		{
			drawList.AddRectFilled(new Vector2(trackStart, min.Y), max, M3.U32(s.SecondaryContainer), rounding);

			if (max.X - trackStart >= size.Y)
			{
				var stopRadius = MathF.Min(2f * scale, rounding);
				drawList.AddCircleFilled(new Vector2(max.X - rounding, centerY), stopRadius, M3.U32(s.Primary), 12);
			}
		}

		if (split - min.X > 0.5f)
		{
			drawList.AddRectFilled(min, new Vector2(split, max.Y), M3.U32(s.Primary), rounding);
		}

		if (marker is { } at && at > 0f && at < 1f)
		{
			var x = float.Lerp(min.X, max.X, at);
			var overhang = 2f * scale;
			drawList.AddLine(new Vector2(x, min.Y - overhang), new Vector2(x, max.Y + overhang),
				M3.U32(markerColor ?? s.Tertiary), 2f * scale);
		}
	}

	public static void LinearProgressIndeterminate(Vector2 size)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;

		ImGui.Dummy(size);
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = size.Y * 0.5f;
		var gap = MathF.Min(4f * scale, size.Y);

		const float Period = 2f;
		var t = (float)(ImGui.GetTime() % Period) / Period;
		Span<Vector2> segments =
		[
			new(Ease((t - 0.20f) / 0.60f), Ease(t / 0.55f)),
			new(Ease((t - 0.70f) / 0.30f), Ease((t - 0.45f) / 0.40f)),
		];

		if (segments[1].X < segments[0].X)
		{
			(segments[0], segments[1]) = (segments[1], segments[0]);
		}

		var trackStart = min.X;
		foreach (var segment in segments)
		{
			var from = float.Lerp(min.X, max.X, segment.X);
			var to = float.Lerp(min.X, max.X, segment.Y);
			if (to - from < 0.5f)
			{
				continue;
			}

			if (from - gap - trackStart > 0.5f)
			{
				drawList.AddRectFilled(new Vector2(trackStart, min.Y), new Vector2(from - gap, max.Y), M3.U32(s.SecondaryContainer), rounding);
			}

			drawList.AddRectFilled(new Vector2(from, min.Y), new Vector2(to, max.Y), M3.U32(s.Primary), rounding);
			trackStart = to + gap;
		}

		if (max.X - trackStart > 0.5f)
		{
			drawList.AddRectFilled(new Vector2(trackStart, min.Y), max, M3.U32(s.SecondaryContainer), rounding);
		}
	}

	public static void CircularProgress(float diameter, float? fraction = null, float thickness = 4f)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var size = Vector2.One * diameter;

		ImGui.Dummy(size);
		var center = ImGui.GetItemRectMin() + (size * 0.5f);
		var stroke = thickness * scale;
		var radius = MathF.Max(1f, (diameter - stroke) * 0.5f);
		var drawList = ImGui.GetWindowDrawList();

		if (fraction is { } value)
		{
			value = Math.Clamp(value, 0f, 1f);

			var gap = MathF.Min(0.1f, stroke * 1.5f / (MathF.Tau * radius));
			if (value <= 0.001f)
			{
				M3Draw.Arc(drawList, center, radius, 0f, 1f, s.SecondaryContainer, stroke);
				return;
			}

			if (value < 0.999f && 1f - value - (gap * 2f) > 0.001f)
			{
				M3Draw.Arc(drawList, center, radius, value + gap, 1f - gap, s.SecondaryContainer, stroke);
			}

			M3Draw.Arc(drawList, center, radius, 0f, value, s.Primary, stroke);
			if (value < 0.999f)
			{
				RoundCaps(drawList, center, radius, 0f, value, s.Primary, stroke);
			}

			return;
		}

		Spinner(drawList, center, radius, s.Primary, stroke);
	}

	// The indeterminate arc of CircularProgress, drawn without taking up a layout slot.
	public static void Spinner(ImDrawListPtr drawList, Vector2 center, float radius, Vector4 color, float stroke)
	{
		const float Cycle = 1.333f;
		var time = (float)ImGui.GetTime();
		var cycles = MathF.Floor(time / Cycle);
		var phase = (time - (cycles * Cycle)) / Cycle;
		var head = Ease(phase / 0.5f) * 0.75f;
		var tail = Ease((phase - 0.5f) / 0.5f) * 0.75f;
		// Wrapped to one turn so the angle doesn't lose precision over a long session.
		var start = ((time * 0.25f) + (cycles * 0.75f) + tail) % 1f;
		var end = start + (head - tail) + 0.03f;

		M3Draw.Arc(drawList, center, radius, start, end, color, stroke);
		RoundCaps(drawList, center, radius, start, end, color, stroke);
	}

	private static float Ease(float t)
	{
		t = Math.Clamp(t, 0f, 1f);
		return t * t * (3f - (2f * t));
	}

	private static void RoundCaps(ImDrawListPtr drawList, Vector2 center, float radius, float from, float to, Vector4 color, float thickness)
	{
		RoundCap(drawList, center, radius, from, color, thickness);
		RoundCap(drawList, center, radius, to, color, thickness);
	}

	private static void RoundCap(ImDrawListPtr drawList, Vector2 center, float radius, float at, Vector4 color, float thickness)
	{
		var angle = (-MathF.PI * 0.5f) + (MathF.Tau * at);
		var point = center + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
		drawList.AddCircleFilled(point, thickness * 0.5f, M3.U32(color), 12);
	}

	#endregion

	#region Sliders

	private static ImRaii.ColorDisposable PushInvisibleSliderChrome(bool condition)
	{
		return ImRaii.PushColor(ImGuiCol.FrameBg, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.FrameBgHovered, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.FrameBgActive, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.SliderGrab, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.SliderGrabActive, new Vector4(0f, 0f, 0f, 0f), condition)
			.Push(ImGuiCol.Text, new Vector4(0f, 0f, 0f, 0f), condition);
	}

	public static bool Slider(string id, ref float value, float min, float max, string displayValue, float width, bool logarithmic = false, bool enabled = true)
	{
		var scale = M3.Scale;
		// Ctrl+click turns the slider into a text box, which must stay visible.
		var editing = ImGuiP.TempInputIsActive(ImGui.GetID(id));
		var flags = ImGuiSliderFlags.NoRoundToFormat | (logarithmic ? ImGuiSliderFlags.Logarithmic : ImGuiSliderFlags.None);
		bool changed;

		using (ImRaii.Disabled(!enabled))
		using (PushInvisibleSliderChrome(!editing))
		using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(editing ? 8f : 0f, 10f) * scale))
		{
			ImGui.SetNextItemWidth(width);
			changed = ImGui.SliderFloat(id, ref value, min, max, "%.2f", flags);
		}

		// ImGui keeps a logarithmic slider clear of zero by this much, which depends on the format's decimals.
		DrawSliderVisual(value, min, max, displayValue, M3.Scheme, scale, editing, logarithmic ? 0.01f : 0f, enabled);
		return changed;
	}

	public static bool SliderInt(string id, ref int value, int min, int max, string displayValue, float width, bool logarithmic = false, bool enabled = true)
	{
		var scale = M3.Scale;
		var editing = ImGuiP.TempInputIsActive(ImGui.GetID(id));
		var flags = logarithmic ? ImGuiSliderFlags.Logarithmic : ImGuiSliderFlags.None;
		bool changed;

		using (ImRaii.Disabled(!enabled))
		using (PushInvisibleSliderChrome(!editing))
		using (ImRaii.PushStyle(ImGuiStyleVar.FramePadding, new Vector2(editing ? 8f : 0f, 10f) * scale))
		{
			ImGui.SetNextItemWidth(width);
			changed = ImGui.SliderInt(id, ref value, min, max, "%d", flags);
		}

		DrawSliderVisual(value, min, max, displayValue, M3.Scheme, scale, editing, logarithmic ? 0.1f : 0f, enabled);
		return changed;
	}

	// The same mapping ImGui uses for a logarithmic grab, so the drawn handle stays under the mouse.
	private static float LogFraction(float value, float min, float max, float epsilon, float usableWidth)
	{
		if (min == max)
		{
			return 0f;
		}

		var flipped = max < min;
		if (flipped)
		{
			(min, max) = (max, min);
		}

		var clamped = Math.Clamp(value, min, max);
		var minFudged = MathF.Abs(min) < epsilon ? (min < 0f ? -epsilon : epsilon) : min;
		var maxFudged = MathF.Abs(max) < epsilon ? (max < 0f ? -epsilon : epsilon) : max;
		if (min == 0f && max < 0f)
		{
			minFudged = -epsilon;
		}
		else if (max == 0f && min < 0f)
		{
			maxFudged = -epsilon;
		}

		float result;
		if (clamped <= minFudged)
		{
			result = 0f;
		}
		else if (clamped >= maxFudged)
		{
			result = 1f;
		}
		else if (min * max < 0f)
		{
			var zero = -min / (max - min);
			var deadzone = ImGui.GetStyle().LogSliderDeadzone * 0.5f / MathF.Max(usableWidth, 1f);
			var left = zero - deadzone;
			var right = zero + deadzone;
			result = clamped == 0f ? zero
				: clamped < 0f ? (1f - (MathF.Log(-clamped / epsilon) / MathF.Log(-minFudged / epsilon))) * left
				: right + (MathF.Log(clamped / epsilon) / MathF.Log(maxFudged / epsilon) * (1f - right));
		}
		else if (min < 0f || max < 0f)
		{
			result = 1f - (MathF.Log(-clamped / -maxFudged) / MathF.Log(-minFudged / -maxFudged));
		}
		else
		{
			result = MathF.Log(clamped / minFudged) / MathF.Log(maxFudged / minFudged);
		}

		return flipped ? 1f - result : result;
	}

	private static void DrawSliderVisual(float value, float min, float max, string displayValue, M3Scheme s, float scale, bool editing, float logEpsilon, bool enabled)
	{
		var itemMin = ImGui.GetItemRectMin();
		var itemMax = ImGui.GetItemRectMax();
		var centerY = (itemMin.Y + itemMax.Y) * 0.5f;
		var drawList = ImGui.GetWindowDrawList();

		var hasReadout = !string.IsNullOrEmpty(displayValue);
		var textSize = hasReadout ? ImGui.CalcTextSize(displayValue) : Vector2.Zero;
		if (hasReadout)
		{
			drawList.AddText(new Vector2(itemMax.X + (10f * scale), centerY - (textSize.Y * 0.5f)),
				M3.U32(s.OnSurfaceVariant, enabled ? 1f : M3.DisabledContent), displayValue);
		}

		if (editing)
		{
			return;
		}

		var hovered = enabled && ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var active = enabled && ImGui.IsItemActive();

		var trackHeight = 4f * scale;
		var handleRadius = 10f * scale;
		var trackLeft = itemMin.X + handleRadius;
		var trackRight = itemMax.X - handleRadius;
		var fraction = logEpsilon > 0f
			? LogFraction(value, min, max, logEpsilon, trackRight - trackLeft)
			: max > min ? Math.Clamp((value - min) / (max - min), 0f, 1f) : 0f;
		var handleX = float.Lerp(trackLeft, trackRight, fraction);
		var accent = enabled ? s.Primary : M3.Alpha(s.OnSurface, M3.DisabledContent);

		drawList.AddRectFilled(
			new Vector2(trackLeft, centerY - (trackHeight * 0.5f)),
			new Vector2(trackRight, centerY + (trackHeight * 0.5f)),
			enabled ? M3.U32(s.SurfaceContainerHighest) : M3.U32(s.OnSurface, M3.DisabledContainer), trackHeight);

		if (handleX > trackLeft)
		{
			drawList.AddRectFilled(
				new Vector2(trackLeft, centerY - (trackHeight * 0.5f)),
				new Vector2(handleX, centerY + (trackHeight * 0.5f)),
				M3.U32(accent), trackHeight);
		}

		if (hovered || active)
		{
			drawList.AddCircleFilled(new Vector2(handleX, centerY), handleRadius + (8f * scale),
				M3.U32(s.Primary, active ? M3.StatePressed : M3.StateHover), 24);
		}

		drawList.AddCircleFilled(new Vector2(handleX, centerY), handleRadius, M3.U32(accent), 24);
		drawList.AddCircleFilled(new Vector2(handleX, centerY), handleRadius * 0.45f, M3.U32(enabled ? s.OnPrimary : s.Surface, 0.9f), 16);

		if (hasReadout && active)
		{
			var padding = new Vector2(8f, 4f) * scale;
			var bubbleMin = new Vector2(handleX - (textSize.X * 0.5f) - padding.X, itemMin.Y - textSize.Y - (padding.Y * 2f) - (6f * scale));
			var bubbleMax = bubbleMin + textSize + (padding * 2f);
			drawList.AddRectFilled(bubbleMin, bubbleMax, M3.U32(s.InverseSurface), M3.ShapeSmall);
			drawList.AddText(bubbleMin + padding, M3.U32(s.InverseOnSurface), displayValue);
		}
	}

	public static float SliderValueGutter(string longestValue)
	{
		return ImGui.CalcTextSize(longestValue).X + (14f * M3.Scale);
	}

	#endregion

	#region Full-width setting rows

	private static (string Label, string Id) SplitLabel(string label)
	{
		// "##" hides text but keeps the whole label as the ID; only "###" replaces it. Otherwise controls with the same suffix would share an ID.
		var explicitId = label.IndexOf("###", StringComparison.Ordinal);
		if (explicitId >= 0)
		{
			var trailing = label[(explicitId + 3)..];
			return (label[..explicitId], string.IsNullOrEmpty(trailing) ? label : trailing);
		}

		var hidden = label.IndexOf("##", StringComparison.Ordinal);
		return hidden < 0 ? (label, label) : (label[..hidden], label);
	}

	private static string FormatValue(string format, float value)
	{
		if (string.IsNullOrEmpty(format))
		{
			return value.ToString("0.##");
		}

		var percent = -1;
		for (var i = 0; i < format.Length - 1; i++)
		{
			if (format[i] != '%')
			{
				continue;
			}

			if (format[i + 1] == '%')
			{
				i++;
				continue;
			}

			percent = i;
			break;
		}

		if (percent < 0)
		{
			return Unescape(format);
		}

		var cursor = percent + 1;
		var digits = 0;
		var hasDigits = false;

		if (cursor < format.Length && format[cursor] == '.')
		{
			cursor++;
			while (cursor < format.Length && char.IsAsciiDigit(format[cursor]))
			{
				digits = (digits * 10) + (format[cursor] - '0');
				cursor++;
				hasDigits = true;
			}
		}

		if (cursor >= format.Length)
		{
			return Unescape(format);
		}

		var rendered = format[cursor] switch
		{
			'f' => value.ToString($"F{(hasDigits ? digits : 3)}"),
			'd' or 'i' => ((int)value).ToString(),
			_ => null,
		};

		return rendered == null
			? Unescape(format)
			: Unescape(string.Concat(format.AsSpan(0, percent), rendered, format.AsSpan(cursor + 1)));
	}

	private static string Unescape(string text)
	{
		return text.Contains("%%", StringComparison.Ordinal)
			? text.Replace("%%", "%", StringComparison.Ordinal)
			: text;
	}

	public static bool RowSwitch(string label, ref bool value, string? supporting = null, bool enabled = true)
	{
		return RowSwitch(label, ref value, out _, supporting, enabled);
	}

	public static bool RowSwitch(string label, ref bool value, out bool hovered, string? supporting = null, bool enabled = true)
	{
		var (display, id) = SplitLabel(label);
		var row = M3SettingRow.Begin(display, supporting, SwitchSize(), disabled: !enabled);
		hovered = row.Hovered;
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = Switch($"##{id}_switch", ref value, enabled);
		M3SettingRow.End(row);
		return changed;
	}

	public static bool RowDragFloat(string label, ref float value, float min, float max, string format, string? supporting = null, bool logarithmic = false, bool enabled = true)
	{
		var (display, id) = SplitLabel(label);
		var trackWidth = 150f * M3.Scale;
		var readout = FormatValue(format, value);
		var controlSize = new Vector2(trackWidth + SliderValueGutter(FormatValue(format, max)), ButtonHeight);

		var row = M3SettingRow.Begin(display, supporting, controlSize, disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = Slider($"##{id}_slider", ref value, min, max, readout, trackWidth, logarithmic, enabled);
		M3SettingRow.End(row);
		return changed;
	}

	public static bool RowDragInt(string label, ref int value, int min, int max, string? supporting = null, bool logarithmic = false, bool enabled = true)
	{
		var (display, id) = SplitLabel(label);
		var trackWidth = 150f * M3.Scale;
		var controlSize = new Vector2(trackWidth + SliderValueGutter(max.ToString()), ButtonHeight);

		var row = M3SettingRow.Begin(display, supporting, controlSize, disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = SliderInt($"##{id}_slider", ref value, min, max, value.ToString(), trackWidth, logarithmic, enabled);
		M3SettingRow.End(row);
		return changed;
	}

	private static float RowComboWidth(float widestLabel)
	{
		var width = MathF.Max(160f * M3.Scale, MathF.Ceiling(widestLabel) + (ComboTextInset * 2f) + ComboChevronWidth + 1f);
		return MathF.Min(width, MathF.Max(120f * M3.Scale, M3SettingRow.MaxControlWidth() * 0.6f));
	}

	public static bool RowCombo(string label, ref int index, IReadOnlyList<string> items, string? supporting = null, bool search = false, bool enabled = true)
	{
		var widest = 0f;
		foreach (var item in items)
		{
			widest = MathF.Max(widest, ImGui.CalcTextSize(item).X);
		}

		var (display, id) = SplitLabel(label);
		var width = RowComboWidth(widest);
		var row = M3SettingRow.Begin(display, supporting, new Vector2(width, ComboHeight), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = Combo($"##{id}_combo", ref index, items, width, search: search, enabled: enabled);
		M3SettingRow.End(row);
		return changed;
	}

	public static bool RowCombo<T>(string label, ref T value, string? supporting = null, Func<T, string>? itemLabel = null, bool search = false, bool enabled = true)
		where T : struct, Enum
	{
		var values = EnumValues<T>.All;
		var widest = 0f;
		foreach (var item in values)
		{
			widest = MathF.Max(widest, ImGui.CalcTextSize(itemLabel?.Invoke(item) ?? EnumLabel(item)).X);
		}

		var (display, id) = SplitLabel(label);
		var width = RowComboWidth(widest);
		var row = M3SettingRow.Begin(display, supporting, new Vector2(width, ComboHeight), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = Combo($"##{id}_combo", ref value, width, itemLabel, search, enabled);
		M3SettingRow.End(row);
		return changed;
	}

	// For code that only knows the enum's type at run time, such as a settings page built by reflection.
	public static bool RowCombo(string label, Type enumType, ref Enum value, string? supporting = null, Func<Enum, string>? itemLabel = null, bool search = false, bool enabled = true)
	{
		var values = EnumValuesOf(enumType);
		var widest = 0f;
		foreach (var item in values)
		{
			widest = MathF.Max(widest, ImGui.CalcTextSize(itemLabel?.Invoke(item) ?? EnumLabel(item)).X);
		}

		var (display, id) = SplitLabel(label);
		var width = RowComboWidth(widest);
		var row = M3SettingRow.Begin(display, supporting, new Vector2(width, ComboHeight), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = Combo($"##{id}_combo", enumType, ref value, width, itemLabel, search, enabled);
		M3SettingRow.End(row);
		return changed;
	}

	public static bool RowColor(string label, ref Vector4 color, Vector4? defaultColor = null, string? supporting = null, bool alpha = true, bool enabled = true)
	{
		var (display, id) = SplitLabel(label);
		var scale = M3.Scale;
		var swatch = ColorSwatchSize;
		var canReset = defaultColor is { } fallback && fallback != color;
		var resetSize = 32f * scale;
		var controlSize = new Vector2(swatch + (canReset ? M3.Space1 + resetSize : 0f), MathF.Max(swatch, resetSize));

		var row = M3SettingRow.Begin(display, supporting, controlSize, disabled: !enabled);
		var origin = row.ControlPosition;
		ImGui.SetCursorScreenPos(origin + new Vector2(0f, (controlSize.Y - swatch) * 0.5f));
		var changed = ColorSwatch($"##{id}_color", ref color, defaultColor, alpha, enabled);

		if (canReset)
		{
			ImGui.SetCursorScreenPos(origin + new Vector2(swatch + M3.Space1, (controlSize.Y - resetSize) * 0.5f));
			if (IconButton($"##{id}_reset", FontAwesomeIcon.Undo, M3Fork.T("Reset to default"), diameter: resetSize, enabled: enabled))
			{
				color = defaultColor!.Value;
				changed = true;
			}
		}

		M3SettingRow.End(row);
		return changed;
	}

	public static bool RowText(string label, ref string text, string? hint = null, string? supporting = null, int maxLength = 256, bool enabled = true)
	{
		var (display, id) = SplitLabel(label);
		var width = MathF.Min(240f * M3.Scale, M3SettingRow.MaxControlWidth() * 0.6f);

		var row = M3SettingRow.Begin(display, supporting, new Vector2(width, M3TextField.Height), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		bool changed;
		using (ImRaii.Disabled(!enabled))
		{
			changed = M3TextField.Draw($"##{id}_text", hint ?? string.Empty, ref text, width, maxLength);
		}

		M3SettingRow.End(row);
		return changed;
	}

	#endregion

	#region Text fields

	// Busy swaps the search icon for a spinner, for while the results it found are on screen.
	public static bool SearchField(string id, string hint, ref string text, float width, int maxLength = 128, bool busy = false, bool enabled = true)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = M3.FitText(40f, 8f);
		var origin = ImGui.GetCursorScreenPos();
		var drawList = ImGui.GetWindowDrawList();
		var min = origin;
		var max = origin + new Vector2(width, height);
		var hoveringField = enabled && ImGui.IsMouseHoveringRect(min, max);

		drawList.AddRectFilled(min, max, M3.U32(s.SurfaceContainerHigh, !enabled ? 0.5f : hoveringField ? 1f : 0.92f), height * 0.5f);

		var iconPadding = 14f * scale;
		var iconWidth = M3Draw.MeasureIcon(FontAwesomeIcon.Search).X;
		if (busy)
		{
			var stroke = 2f * scale;
			Spinner(drawList, new Vector2(min.X + iconPadding + (iconWidth * 0.5f), min.Y + (height * 0.5f)),
				MathF.Max(1f, (iconWidth - stroke) * 0.5f), s.Primary, stroke);
		}
		else
		{
			M3Draw.Icon(drawList, FontAwesomeIcon.Search,
				new Vector2(min.X + iconPadding, min.Y + ((height - ImGui.GetTextLineHeight()) * 0.5f)),
				M3.Alpha(s.OnSurfaceVariant, 0.9f));
		}

		var fieldStart = min.X + iconPadding + iconWidth + (10f * scale);
		var hasText = !string.IsNullOrEmpty(text);
		var clearWidth = hasText ? 32f * scale : 0f;
		var fieldWidth = MathF.Max(24f * scale, max.X - fieldStart - (14f * scale) - clearWidth);

		ImGui.SetCursorScreenPos(new Vector2(fieldStart, min.Y + ((height - ImGui.GetFrameHeight()) * 0.5f)));
		ImGui.SetNextItemWidth(fieldWidth);

		bool changed;
		using (ImRaii.Disabled(!enabled))
		using (PushClearFrame())
		{
			changed = ImGui.InputTextWithHint(id, hint, ref text, maxLength, ImGuiInputTextFlags.AutoSelectAll);
		}

		if (hasText)
		{
			ImGui.SetCursorScreenPos(new Vector2(max.X - clearWidth - (6f * scale), min.Y + ((height - (26f * scale)) * 0.5f)));
			if (IconButton($"{id}_clear", FontAwesomeIcon.Times, M3Fork.T("Clear search"), diameter: 26f * scale, enabled: enabled))
			{
				text = string.Empty;
				changed = true;
			}
		}

		ImGui.SetCursorScreenPos(new Vector2(min.X, max.Y));
		ImGui.Dummy(new Vector2(width, 0f));
		return changed;
	}

	public static float TextFieldHeight => M3.FitText(48f, 12f);

	// Pass an id starting with "##" to hide ImGui's own label.
	public static bool TextField(string id, string label, ref string text, float width, int maxLength = 256, string? supporting = null, bool error = false, FontAwesomeIcon leadingIcon = FontAwesomeIcon.None, ImGuiInputTextFlags flags = ImGuiInputTextFlags.None, bool enabled = true)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = TextFieldHeight;
		var paddingX = 16f * scale;
		var framePadding = ImGui.GetStyle().FramePadding.X;
		var bodyFontSize = ImGui.GetFontSize();

		float smallFontSize;
		using (ImRaii.PushFont(M3.LabelSmall))
		{
			smallFontSize = ImGui.GetFontSize();
		}

		var origin = ImGui.GetCursorScreenPos();
		var min = origin + new Vector2(0f, smallFontSize * 0.5f);
		var max = min + new Vector2(width, height);
		var drawList = ImGui.GetWindowDrawList();
		var hovered = ImGui.IsMouseHoveringRect(min, max);

		var textLeft = min.X + paddingX;
		if (leadingIcon != FontAwesomeIcon.None)
		{
			var iconSize = M3Draw.MeasureIcon(leadingIcon);
			M3Draw.Icon(drawList, leadingIcon, new Vector2(textLeft, min.Y + ((height - iconSize.Y) * 0.5f)),
				error ? s.Error : s.OnSurfaceVariant);
			textLeft += iconSize.X + (12f * scale);
		}

		var trailingWidth = error ? M3Draw.MeasureIcon(FontAwesomeIcon.ExclamationCircle).X + (12f * scale) : 0f;
		var textRight = max.X - paddingX - trailingWidth;

		var inputLeft = textLeft - framePadding;
		ImGui.SetCursorScreenPos(new Vector2(inputLeft, min.Y + ((height - ImGui.GetFrameHeight()) * 0.5f)));
		ImGui.SetNextItemWidth(MathF.Max(24f * scale, textRight + framePadding - inputLeft));

		bool changed;
		using (ImRaii.Disabled(!enabled))
		using (PushClearFrame())
		{
			changed = ImGui.InputText(id, ref text, maxLength, flags);
		}

		var focused = ImGui.IsItemActive();
		var floating = M3Motion.Approach($"{id}_label", focused || !string.IsNullOrEmpty(text) ? 1f : 0f, M3Motion.FastDuration);

		var dim = enabled ? 1f : M3.DisabledContent;
		var accent = M3.Alpha(error ? s.Error : focused ? s.Primary : hovered && enabled ? s.OnSurface : s.Outline, dim);
		var labelColor = M3.Alpha(error ? s.Error : focused ? s.Primary : s.OnSurfaceVariant, dim);

		var hasLabel = !string.IsNullOrEmpty(label);
		var floatingX = min.X + paddingX;
		var notchStart = floatingX - (4f * scale);
		var notchEnd = hasLabel
			? notchStart + (((ImGui.CalcTextSize(label).X * (smallFontSize / bodyFontSize)) + (8f * scale)) * floating)
			: notchStart;
		OutlineWithNotch(drawList, min, max, M3.ShapeExtraSmall, notchStart, notchEnd, M3.U32(accent), (focused || error ? 2f : 1f) * scale);

		if (hasLabel)
		{
			var restingPosition = new Vector2(textLeft, min.Y + ((height - bodyFontSize) * 0.5f));
			var floatingPosition = new Vector2(floatingX, min.Y - (smallFontSize * 0.5f));
			drawList.AddText(ImGui.GetFont(), float.Lerp(bodyFontSize, smallFontSize, floating),
				Vector2.Lerp(restingPosition, floatingPosition, floating), M3.U32(labelColor), label);
		}

		if (error)
		{
			var iconSize = M3Draw.MeasureIcon(FontAwesomeIcon.ExclamationCircle);
			M3Draw.Icon(drawList, FontAwesomeIcon.ExclamationCircle,
				new Vector2(max.X - paddingX - iconSize.X, min.Y + ((height - iconSize.Y) * 0.5f)), s.Error);
		}

		var bottom = max.Y;
		if (!string.IsNullOrEmpty(supporting))
		{
			using var font = ImRaii.PushFont(M3.LabelSmall);
			bottom = M3Draw.WrappedText(supporting, new Vector2(min.X + paddingX, max.Y + (4f * scale)),
				MathF.Max(32f * scale, width - (paddingX * 2f)), error ? s.Error : M3.Alpha(s.OnSurfaceVariant, 0.9f));
		}

		ImGui.SetCursorScreenPos(new Vector2(origin.X, bottom));
		ImGui.Dummy(new Vector2(width, 0f));
		return changed;
	}

	private static void OutlineWithNotch(ImDrawListPtr drawList, Vector2 min, Vector2 max, float rounding, float notchStart, float notchEnd, uint color, float thickness)
	{
		if (notchEnd - notchStart < 1f)
		{
			drawList.AddRect(min, max, color, rounding, ImDrawFlags.None, thickness);
			return;
		}

		// PathArcToFast angles are in twelfths of a turn: 0 right, 3 down, 6 left, 9 up.
		drawList.PathLineTo(new Vector2(notchEnd, min.Y));
		drawList.PathArcToFast(new Vector2(max.X - rounding, min.Y + rounding), rounding, 9, 12);
		drawList.PathArcToFast(new Vector2(max.X - rounding, max.Y - rounding), rounding, 0, 3);
		drawList.PathArcToFast(new Vector2(min.X + rounding, max.Y - rounding), rounding, 3, 6);
		drawList.PathArcToFast(new Vector2(min.X + rounding, min.Y + rounding), rounding, 6, 9);
		drawList.PathLineTo(new Vector2(notchStart, min.Y));
		drawList.PathStroke(color, ImDrawFlags.None, thickness);
	}

	#endregion

	#region Structure

	public static void Divider(float verticalPadding = 8f)
	{
		var scale = M3.Scale;
		var pad = verticalPadding * scale;
		ImGui.Dummy(new Vector2(0f, pad));
		var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
		var origin = ImGui.GetCursorScreenPos();
		ImGui.GetWindowDrawList().AddLine(origin, origin + new Vector2(width, 0f), M3.U32(M3.Scheme.OutlineVariant, 0.7f), 1f * scale);
		ImGui.Dummy(new Vector2(width, pad));
	}

	public static void SectionLabel(string text, Vector4? accent = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var tone = accent ?? s.Primary;
		var label = text.ToUpperInvariant();

		using var font = ImRaii.PushFont(M3.LabelSmall);
		var textSize = ImGui.CalcTextSize(label);
		var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
		var height = textSize.Y + (14f * scale);

		ImGui.Dummy(new Vector2(width, height));
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var textY = min.Y + (10f * scale);
		var lineY = textY + (textSize.Y * 0.5f);

		drawList.AddText(new Vector2(min.X, textY), M3.U32(tone, 0.95f), label);

		var lineStart = min.X + textSize.X + (10f * scale);
		if (max.X > lineStart)
		{
			drawList.AddLine(new Vector2(lineStart, lineY), new Vector2(max.X, lineY), M3.U32(s.OutlineVariant, 0.55f), 1f * scale);
		}
	}

	public static float HelpMarkerWidth(FontAwesomeIcon icon = FontAwesomeIcon.InfoCircle)
	{
		return M3Draw.MeasureIcon(icon).X;
	}

	// A small icon that explains something on hover. It's as tall as a frame, to line up with controls and frame-aligned text.
	public static void HelpMarker(string text, FontAwesomeIcon icon = FontAwesomeIcon.InfoCircle, Vector4? color = null)
	{
		var size = new Vector2(HelpMarkerWidth(icon), ImGui.GetFrameHeight());
		ImGui.Dummy(size);
		var min = ImGui.GetItemRectMin();
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);
		var tone = color ?? M3.Scheme.OnSurfaceVariant;

		M3Draw.IconCentered(ImGui.GetWindowDrawList(), icon, min, min + size, M3.Alpha(tone, hovered ? 1f : 0.7f * tone.W));
		if (hovered)
		{
			M3Tooltip.Show(text);
		}
	}

	public static bool Banner(string id, string message, M3Severity severity, FontAwesomeIcon icon, string? actionLabel = null, string? tooltip = null)
	{
		return Banner(id, message, severity, icon, out _, actionLabel, tooltip);
	}

	public static bool Banner(string id, string message, M3Severity severity, FontAwesomeIcon icon, out bool hovered, string? actionLabel = null, string? tooltip = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var accent = M3.Severity(severity);
		var container = M3.SeverityContainer(severity);
		var padding = new Vector2(16f, 12f) * scale;
		var width = MathF.Max(64f * scale, ImGui.GetContentRegionAvail().X);

		var iconWidth = M3Draw.MeasureIcon(icon).X + (12f * scale);
		var actionWidth = string.IsNullOrEmpty(actionLabel) ? 0f : ButtonWidth(FontAwesomeIcon.None, actionLabel) + (12f * scale);
		var textWidth = MathF.Max(32f * scale, width - (padding.X * 2f) - iconWidth - actionWidth);
		var textSize = ImGui.CalcTextSize(message, false, textWidth);
		var height = MathF.Max(textSize.Y, string.IsNullOrEmpty(actionLabel) ? 0f : ButtonHeight) + (padding.Y * 2f);

		var origin = ImGui.GetCursorScreenPos();
		var min = origin;
		var max = origin + new Vector2(width, height);
		var drawList = ImGui.GetWindowDrawList();
		hovered = ImGui.IsMouseHoveringRect(min, max, false) && ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows);

		drawList.AddRectFilled(min, max, M3.U32(container, 0.55f), M3.ShapeMedium);
		M3Draw.AccentRail(drawList, min.X + (2f * scale), 3f * scale, min.Y + (6f * scale), max.Y - (6f * scale), M3.Alpha(accent, 0.9f),
			M3Draw.ResolveFade(40f, 0.35f, height));

		M3Draw.Icon(drawList, icon, new Vector2(min.X + padding.X, min.Y + padding.Y + ((ImGui.GetTextLineHeight() * 0.1f))), accent);
		_ = M3Draw.WrappedText(message, new Vector2(min.X + padding.X + iconWidth, min.Y + padding.Y), textWidth, M3.Alpha(s.OnSurface, 0.94f));

		var clicked = false;
		if (!string.IsNullOrEmpty(actionLabel))
		{
			ImGui.SetCursorScreenPos(new Vector2(max.X - padding.X - actionWidth + (12f * scale), min.Y + ((height - ButtonHeight) * 0.5f)));
			clicked = Button($"{id}_action", actionLabel, M3ButtonStyle.Text);
		}

		ImGui.SetCursorScreenPos(new Vector2(min.X, max.Y));
		ImGui.Dummy(new Vector2(width, 0f));

		if (hovered)
		{
			M3Tooltip.Show(tooltip);
		}

		return clicked;
	}

	public static bool EmptyState(string id, string headline, string? supporting = null, FontAwesomeIcon icon = FontAwesomeIcon.Inbox, string? actionLabel = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var width = MathF.Max(64f * scale, ImGui.GetContentRegionAvail().X);
		var textWidth = MathF.Min(width, 360f * scale);
		var origin = ImGui.GetCursorScreenPos();
		var centerX = origin.X + (width * 0.5f);
		var drawList = ImGui.GetWindowDrawList();
		var y = origin.Y + (16f * scale);

		var diameter = 56f * scale;
		var circleMin = new Vector2(centerX - (diameter * 0.5f), y);
		var circleMax = circleMin + new Vector2(diameter, diameter);
		drawList.AddCircleFilled((circleMin + circleMax) * 0.5f, diameter * 0.5f, M3.U32(s.SecondaryContainer, 0.8f), 48);
		M3Draw.IconCentered(drawList, icon, circleMin, circleMax, s.OnSecondaryContainer);
		y = circleMax.Y + (12f * scale);

		using (ImRaii.PushFont(M3.TitleMedium))
		{
			y = CenteredBlock(headline, centerX, y, textWidth, M3.Alpha(s.OnSurface, 0.95f));
		}

		if (!string.IsNullOrEmpty(supporting))
		{
			y = CenteredBlock(supporting, centerX, y + (4f * scale), textWidth, M3.Alpha(s.OnSurfaceVariant, 0.9f));
		}

		var clicked = false;
		if (!string.IsNullOrEmpty(actionLabel))
		{
			var buttonWidth = ButtonWidth(FontAwesomeIcon.None, actionLabel);
			ImGui.SetCursorScreenPos(new Vector2(centerX - (buttonWidth * 0.5f), y + (12f * scale)));
			clicked = Button($"{id}_action", actionLabel, M3ButtonStyle.Tonal);
			y = ImGui.GetItemRectMax().Y;
		}

		ImGui.SetCursorScreenPos(new Vector2(origin.X, y + (16f * scale)));
		ImGui.Dummy(new Vector2(width, 0f));
		return clicked;
	}

	private static float CenteredBlock(string text, float centerX, float top, float maxWidth, Vector4 color)
	{
		var blockWidth = ImGui.CalcTextSize(text, false, maxWidth).X;
		return M3Draw.WrappedText(text, new Vector2(centerX - (blockWidth * 0.5f), top), maxWidth, color);
	}

	#endregion

	#region Select

	public static float ComboHeight => M3.FitText(38f, 8f);

	private static float ComboTextInset => 12f * M3.Scale;
	private static float ComboChevronWidth => 24f * M3.Scale;

	private static readonly Dictionary<uint, (int Count, float Widest)> _comboWidths = [];
	private static readonly Dictionary<uint, string> _comboQueries = [];
	private static readonly List<int> _comboMatches = [];

	public static float ComboWidthFor(string label)
	{
		return MathF.Ceiling(ImGui.CalcTextSize(label).X) + (ComboTextInset * 2f) + ComboChevronWidth + 1f;
	}

	public static bool Combo(string id, ref int index, IReadOnlyList<string> items, float width, string? emptyText = null, bool search = false, bool enabled = true, string? tooltip = null)
	{
		return Combo(id, ref index, items.Count, i => items[i], width, emptyText, search, enabled, tooltip);
	}

	// Search adds a filter box to the menu. Only the rows in view are drawn, so a list of thousands stays cheap.
	public static bool Combo(string id, ref int index, int count, Func<int, string> itemLabel, float width, string? emptyText = null, bool search = false, bool enabled = true, string? tooltip = null,
		Func<int, string?>? itemTooltip = null, Func<int, bool>? itemEnabled = null, Func<int, bool>? itemVisible = null)
	{
		var scale = M3.Scale;
		var label = index >= 0 && index < count ? itemLabel(index) : emptyText ?? string.Empty;
		var popupId = $"{id}_menu";
		var key = ImGui.GetID(popupId);

		var opening = ComboField(id, popupId, label, width, enabled, tooltip);
		if (opening)
		{
			ImGui.OpenPopup(popupId);
		}

		// The menu fits its widest item. Measured once as it opens, since long lists make that slow to do every frame.
		var popupWidth = MathF.Max(width, 160f * scale);
		if (opening || ImGui.IsPopupOpen(popupId))
		{
			if (opening || !_comboWidths.TryGetValue(key, out var measured) || measured.Count != count)
			{
				var widest = 0f;
				for (var i = 0; i < count; i++)
				{
					widest = MathF.Max(widest, ImGui.CalcTextSize(itemLabel(i)).X);
				}

				measured = (count, widest);
				_comboWidths[key] = measured;
			}

			popupWidth = MathF.Max(popupWidth, measured.Widest + (56f * scale));
		}

		popupWidth = MathF.Min(popupWidth, 560f * scale);
		ImGui.SetNextWindowSizeConstraints(new Vector2(popupWidth, 0f), new Vector2(popupWidth, 460f * scale));
		using var menu = M3Menu.Begin(popupId);
		if (!menu.IsOpen)
		{
			return false;
		}

		var query = string.Empty;
		var pickFirst = false;
		if (search)
		{
			_ = _comboQueries.TryGetValue(key, out query);
			query ??= string.Empty;
			if (ImGui.IsWindowAppearing())
			{
				query = string.Empty;
				ImGui.SetKeyboardFocusHere();
			}

			// Enter ends typing within the field's own call, so check whether it's being typed in before drawing it.
			var typing = ImGuiP.GetActiveID() == ImGui.GetID("##search");
			_ = SearchField("##search", M3Fork.T("Search"), ref query, ImGui.GetContentRegionAvail().X);
			pickFirst = typing && (ImGui.IsKeyPressed(ImGuiKey.Enter) || ImGui.IsKeyPressed(ImGuiKey.KeypadEnter));
			_comboQueries[key] = query;
			ImGui.Dummy(new Vector2(0f, M3.Space1));
		}

		_comboMatches.Clear();
		for (var i = 0; i < count; i++)
		{
			if (itemVisible?.Invoke(i) == false)
			{
				continue;
			}

			if (query.Length > 0 && !itemLabel(i).Contains(query, StringComparison.OrdinalIgnoreCase))
			{
				continue;
			}

			_comboMatches.Add(i);
		}

		if (_comboMatches.Count == 0)
		{
			M3Text.Draw(query.Length > 0 ? M3Fork.T("No matches") : M3Fork.T("Nothing to pick"), M3TextStyle.Muted);
			return false;
		}

		var changed = false;
		if (pickFirst)
		{
			foreach (var match in _comboMatches)
			{
				if (itemEnabled?.Invoke(match) != false)
				{
					index = match;
					changed = true;
					ImGui.CloseCurrentPopup();
					return true;
				}
			}
		}

		var rowHeight = MenuItemHeight;
		var listHeight = MathF.Min(_comboMatches.Count * rowHeight, 360f * scale);
		using var list = ImRaii.Child("##items", new Vector2(0f, listHeight), false);
		if (!list)
		{
			return false;
		}

		if (ImGui.IsWindowAppearing() && index >= 0)
		{
			var row = _comboMatches.IndexOf(index);
			if (row >= 0)
			{
				ImGui.SetScrollY(MathF.Max(0f, (row - 2) * rowHeight));
			}
		}

		var clipper = ImGui.ImGuiListClipper();
		clipper.Begin(_comboMatches.Count, rowHeight);
		while (clipper.Step())
		{
			for (var row = clipper.DisplayStart; row < clipper.DisplayEnd; row++)
			{
				var i = _comboMatches[row];
				if (MenuItemCore($"##item{i}", itemLabel(i), i == index, FontAwesomeIcon.None, itemEnabled?.Invoke(i) ?? true, null, itemTooltip?.Invoke(i), null, true))
				{
					index = i;
					changed = true;
					ImGui.CloseCurrentPopup();
				}
			}
		}

		clipper.End();
		clipper.Destroy();
		return changed;
	}

	public static bool Combo<T>(string id, ref T value, float width, Func<T, string>? itemLabel = null, bool search = false, bool enabled = true, string? tooltip = null,
		Func<T, string?>? itemTooltip = null, Func<T, bool>? itemVisible = null)
		where T : struct, Enum
	{
		var values = EnumValues<T>.All;
		var labels = itemLabel ?? (static item => EnumLabel(item));
		var index = Array.IndexOf(values, value);
		if (!Combo(id, ref index, values.Length, i => labels(values[i]), width, labels(value), search, enabled, tooltip,
			itemTooltip == null ? null : i => itemTooltip(values[i]),
			null,
			itemVisible == null ? null : i => itemVisible(values[i])))
		{
			return false;
		}

		value = values[index];
		return true;
	}

	// For code that only knows the enum's type at run time, such as a settings page built by reflection.
	public static bool Combo(string id, Type enumType, ref Enum value, float width, Func<Enum, string>? itemLabel = null, bool search = false, bool enabled = true, string? tooltip = null)
	{
		var values = EnumValuesOf(enumType);
		var labels = itemLabel ?? EnumLabel;
		var index = Array.IndexOf(values, value);
		if (!Combo(id, ref index, values.Length, i => labels(values[i]), width, labels(value), search, enabled, tooltip))
		{
			return false;
		}

		value = values[index];
		return true;
	}

	private static bool ComboField(string id, string popupId, string label, float width, bool enabled, string? tooltip)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = ComboHeight;

		var clicked = ImGui.InvisibleButton(id, new Vector2(width, height)) && enabled;
		var mouseOver = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var hovered = enabled && mouseOver;
		var held = enabled && ImGui.IsItemActive();
		var open = ImGui.IsPopupOpen(popupId);
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = M3.ShapeSmall;
		var dim = enabled ? 1f : M3.DisabledContent;

		var fill = M3.Alpha(s.SurfaceContainerHighest, enabled ? 0.55f : 0.25f);
		if (hovered || held)
		{
			fill = M3.StateLayer(fill, s.OnSurface, hovered, held);
		}

		drawList.AddRectFilled(min, max, M3.U32(fill), rounding);
		drawList.AddRect(min, max, M3.U32(open ? s.Primary : s.Outline, (open ? 1f : 0.75f) * dim), rounding, ImDrawFlags.None, (open ? 2f : 1f) * scale);

		var textWidth = MathF.Max(8f * scale, width - (ComboTextInset * 2f) - ComboChevronWidth);
		var display = M3Navigation.Truncate(label, textWidth);
		var textSize = ImGui.CalcTextSize(display);
		drawList.AddText(new Vector2(min.X + ComboTextInset, min.Y + ((height - textSize.Y) * 0.5f)), M3.U32(s.OnSurface, 0.95f * dim), display);

		var chevronCenter = new Vector2(max.X - (16f * scale), min.Y + (height * 0.5f));
		var arm = 4.5f * scale;
		var chevronColor = M3.U32(s.OnSurfaceVariant, (hovered || open ? 1f : 0.8f) * dim);
		drawList.AddLine(chevronCenter + new Vector2(-arm, -arm * 0.5f), chevronCenter + new Vector2(0f, arm * 0.6f), chevronColor, 2f * scale);
		drawList.AddLine(chevronCenter + new Vector2(0f, arm * 0.6f), chevronCenter + new Vector2(arm, -arm * 0.5f), chevronColor, 2f * scale);
		M3Draw.FocusRing(min, max, rounding);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (mouseOver)
		{
			M3Tooltip.Show(tooltip ?? (display != label ? label : null));
		}

		return clicked;
	}

	private static class EnumValues<T> where T : struct, Enum
	{
		public static readonly T[] All = Enum.GetValues<T>();
	}

	private static readonly Dictionary<Type, Enum[]> _enumValues = [];
	private static readonly Dictionary<Enum, string> _enumLabels = [];

	private static Enum[] EnumValuesOf(Type type)
	{
		if (!_enumValues.TryGetValue(type, out var values))
		{
			var raw = Enum.GetValues(type);
			values = new Enum[raw.Length];
			for (var i = 0; i < raw.Length; i++)
			{
				values[i] = (Enum)raw.GetValue(i)!;
			}

			_enumValues[type] = values;
		}

		return values;
	}

	// The value's name with its words spaced out, so LowestHp reads "Lowest Hp" and Phase2 reads "Phase 2".
	public static string EnumLabel(Enum value)
	{
		if (_enumLabels.TryGetValue(value, out var cached))
		{
			return cached;
		}

		var name = value.ToString();
		var builder = new System.Text.StringBuilder(name.Length + 4);
		for (var i = 0; i < name.Length; i++)
		{
			var c = name[i];
			if (c == '_')
			{
				builder.Append(' ');
				continue;
			}

			if (i > 0 && builder.Length > 0 && builder[^1] != ' ')
			{
				var previous = name[i - 1];
				var nextIsLower = i + 1 < name.Length && char.IsLower(name[i + 1]);
				var wordStart = char.IsUpper(c) && (char.IsLower(previous) || char.IsDigit(previous) || (char.IsUpper(previous) && nextIsLower));
				var numberStart = char.IsDigit(c) && char.IsLetter(previous);
				if (wordStart || numberStart)
				{
					builder.Append(' ');
				}
			}

			builder.Append(c);
		}

		var label = builder.ToString();
		_enumLabels[value] = label;
		return label;
	}

	public static float MenuItemHeight => M3.FitText(34f, 7f);

	public static bool MenuItem(string id, string label, bool selected, FontAwesomeIcon icon = FontAwesomeIcon.None, bool enabled = true, string? shortcut = null, string? tooltip = null, IDalamudTextureWrap? texture = null)
	{
		return MenuItemCore(id, label, selected, icon, enabled, shortcut, tooltip, texture, false);
	}

	// Fit keeps the row inside the menu and shortens a long label, for menus of a fixed width.
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

	#endregion

	#region Misc

	public static float ColorSwatchSize => 28f * M3.Scale;

	// With a default colour, the picker gets a reset button. Alpha false hides the alpha bar and keeps the colour opaque.
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

	internal static void Reset()
	{
		_comboQueries.Clear();
		_comboMatches.Clear();
		_comboWidths.Clear();
		_holdProgress.Clear();
		_enumLabels.Clear();
		_enumValues.Clear();
		ResetHotkeys();
		ResetReorder();
	}

	#endregion
}
