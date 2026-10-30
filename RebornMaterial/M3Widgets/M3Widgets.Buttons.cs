using Dalamud.Interface.Textures.TextureWraps;

namespace RebornMaterial;

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
	public static float ButtonHeight => M3.FitText(40f, 8f);

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

	private static void ResetButtons()
	{
		_holdProgress.Clear();
	}

	public static bool HoldButton(string id, string label, float seconds = 1f, M3ButtonStyle style = M3ButtonStyle.Danger, FontAwesomeIcon icon = FontAwesomeIcon.None, float? width = null, bool enabled = true, string? tooltip = null)
	{
		_ = Button(id, label, style, icon, width, enabled, tooltip ?? M3Fork.T("Hold to confirm"));
		var key = ImGuiP.GetItemID();
		var held = enabled && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var delta = ImGui.GetIO().DeltaTime;

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
			_ => (M3.Alpha(s.Surface, 0f), s.OnSurfaceVariant),
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
}
