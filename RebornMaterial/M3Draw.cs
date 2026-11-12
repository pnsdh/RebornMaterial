using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;
using System.Diagnostics.CodeAnalysis;

namespace RebornMaterial;

public static class M3Draw
{
	private const int RailFadeBands = 6;

	private static readonly Dictionary<FontAwesomeIcon, string> _glyphs = [];
	private static readonly Dictionary<FontAwesomeIcon, Vector2> _iconSizes = [];
	private static (nint Font, float Size, int Scale) _iconSizesFor;

	public static bool MouseOver(Vector2 min, Vector2 max, bool whileActive = false)
	{
		var flags = ImGuiHoveredFlags.ChildWindows | (whileActive ? ImGuiHoveredFlags.AllowWhenBlockedByActiveItem : ImGuiHoveredFlags.None);
		return ImGui.IsMouseHoveringRect(min, max, false) && ImGui.IsWindowHovered(flags);
	}

	public static bool Usable([NotNullWhen(true)] IDalamudTextureWrap? texture)
	{
		if (texture == null)
		{
			return false;
		}

		try
		{
			return !texture.Handle.IsNull;
		}
		catch (ObjectDisposedException)
		{
			return false;
		}
	}

	public static void Container(ImDrawListPtr drawList, Vector2 min, Vector2 max, Vector4 fill, float rounding, Vector4? outline = null, float outlineSize = 1f)
	{
		if (fill.W > 0f)
		{
			drawList.AddRectFilled(min, max, M3.U32(fill), rounding);
		}

		if (outline is { } stroke && stroke.W > 0f)
		{
			drawList.AddRect(min, max, M3.U32(stroke), rounding, ImDrawFlags.None, outlineSize * M3.Scale);
		}
	}

	public static void Elevation(ImDrawListPtr drawList, Vector2 min, Vector2 max, float rounding, int level)
	{
		if (level <= 0)
		{
			return;
		}

		var scale = M3.Scale;
		var shadow = M3.Scheme.Shadow;
		var spread = level * 2f * scale;
		var offset = new Vector2(0f, level * 0.75f * scale);
		const int steps = 4;

		for (var i = steps; i >= 1; i--)
		{
			var t = (float)i / steps;
			var grow = spread * t;
			var alpha = 0.16f * (1f - t) * (1f - t);
			drawList.AddRectFilled(
				min - new Vector2(grow, grow) + offset,
				max + new Vector2(grow, grow) + offset,
				M3.U32(shadow, alpha),
				rounding + grow);
		}
	}

	public static void AccentRail(ImDrawListPtr drawList, float x, float width, float top, float bottom, Vector4 color, float fadeLength)
	{
		var height = bottom - top;
		if (width <= 0f || height <= 0f)
		{
			return;
		}

		var right = x + width;
		var fade = Math.Clamp(fadeLength, 0f, height * 0.5f);
		if (fade <= 0f)
		{
			drawList.AddRectFilled(new Vector2(x, top), new Vector2(right, bottom), M3.U32(color), width * 0.5f);
			return;
		}

		var solidTop = top + fade;
		var solidBottom = bottom - fade;
		if (solidBottom > solidTop)
		{
			drawList.AddRectFilled(new Vector2(x, solidTop), new Vector2(right, solidBottom), M3.U32(color));
		}

		for (var band = 0; band < RailFadeBands; band++)
		{
			var near = (float)band / RailFadeBands;
			var far = (float)(band + 1) / RailFadeBands;
			var nearColor = M3.U32(color, color.W * SmoothFade(near));
			var farColor = M3.U32(color, color.W * SmoothFade(far));

			drawList.AddRectFilledMultiColor(
				new Vector2(x, solidTop - (fade * far)),
				new Vector2(right, solidTop - (fade * near)),
				farColor, farColor, nearColor, nearColor);

			drawList.AddRectFilledMultiColor(
				new Vector2(x, solidBottom + (fade * near)),
				new Vector2(right, solidBottom + (fade * far)),
				nearColor, nearColor, farColor, farColor);
		}
	}

	public static float ResolveFade(float fadeLength, float fraction, float railHeight)
	{
		return MathF.Max(0f, MathF.Min(fadeLength * M3.Scale, railHeight * fraction));
	}

	public static void Arc(ImDrawListPtr drawList, Vector2 center, float radius, float from, float to, Vector4 color, float thickness)
	{
		if (to - from <= 0.001f)
		{
			return;
		}

		const float top = -MathF.PI * 0.5f;
		var segments = Math.Max(4, (int)MathF.Ceiling(48f * (to - from)));
		drawList.PathArcTo(center, radius, top + (MathF.Tau * from), top + (MathF.Tau * to), segments);
		drawList.PathStroke(M3.U32(color), ImDrawFlags.None, thickness);
	}

	public static void VerticalGradient(ImDrawListPtr drawList, Vector2 min, Vector2 max, Vector4 top, Vector4 bottom)
	{
		var topColor = M3.U32(top);
		var bottomColor = M3.U32(bottom);
		drawList.AddRectFilledMultiColor(min, max, topColor, topColor, bottomColor, bottomColor);
	}

	public static void WindowBackdrop(float rounding, float height = 200f)
	{
		var s = M3.Scheme;
		var drawList = ImGui.GetWindowDrawList();
		var windowPos = ImGui.GetWindowPos();
		var windowSize = ImGui.GetWindowSize();
		var top = M3.Alpha(s.SurfaceContainer, 0.65f);
		var max = new Vector2(windowPos.X + windowSize.X, windowPos.Y + MathF.Min(height * M3.Scale, windowSize.Y));
		rounding = MathF.Min(rounding, (max.Y - windowPos.Y) * 0.5f);

		drawList.PushClipRect(windowPos, windowPos + windowSize, false);
		if (rounding > 0f)
		{
			drawList.AddRectFilled(windowPos, new Vector2(max.X, windowPos.Y + rounding), M3.U32(top), rounding, ImDrawFlags.RoundCornersTop);
		}

		VerticalGradient(drawList, new Vector2(windowPos.X, windowPos.Y + rounding), max, top, M3.Alpha(s.Surface, 0f));
		drawList.PopClipRect();
	}

	public static string Glyph(FontAwesomeIcon icon)
	{
		if (!_glyphs.TryGetValue(icon, out var glyph))
		{
			glyph = icon.ToIconString();
			_glyphs[icon] = glyph;
		}

		return glyph;
	}

	public static unsafe Vector2 MeasureIcon(FontAwesomeIcon icon)
	{
		var iconFont = M3Fork.IconFont; // Fork: the text-sized icon font
		var measuredFor = ((nint)iconFont.Handle, iconFont.FontSize, (int)MathF.Round(ImGui.GetFontSize() / ImGui.GetFont().FontSize * 1000f));
		if (measuredFor != _iconSizesFor)
		{
			_iconSizes.Clear();
			_iconSizesFor = measuredFor;
		}

		if (_iconSizes.TryGetValue(icon, out var size))
		{
			return size;
		}

		using var font = ImRaii.PushFont(iconFont);
		size = ImGui.CalcTextSize(Glyph(icon));
		_iconSizes[icon] = size;
		return size;
	}

	// Fork: draws in the box MeasureIcon gives at position, with the inked shape centred in it.
	public static void Icon(ImDrawListPtr drawList, FontAwesomeIcon icon, Vector2 position, Vector4 color)
	{
		IconCentered(drawList, icon, position, position + MeasureIcon(icon), color);
	}

	// Fork: text-sized icon font, centred on the glyph's inked shape rather than its advance box.
	public static void IconCentered(ImDrawListPtr drawList, FontAwesomeIcon icon, Vector2 min, Vector2 max, Vector4 color, float scale = 1f)
	{
		var iconFont = M3Fork.IconFont;
		using var font = ImRaii.PushFont(iconFont);
		var text = Glyph(icon);
		var fontSize = ImGui.GetFontSize() * scale;
		var position = M3Fork.InkCenteredPosition(iconFont, fontSize, icon, (min + max) * 0.5f, ImGui.CalcTextSize(text) * scale);
		drawList.AddText(iconFont, fontSize, position, M3.U32(color), text);
	}

	public static void FocusRing(Vector2 min, Vector2 max, float rounding)
	{
		if (!ImGui.IsItemFocused() || !ImGui.GetIO().NavVisible)
		{
			return;
		}

		var scale = M3.Scale;
		var gap = 2f * scale;
		ImGui.GetWindowDrawList().AddRect(min - new Vector2(gap), max + new Vector2(gap), M3.U32(M3.Scheme.Secondary),
			rounding + gap, ImDrawFlags.None, 2.5f * scale);
	}

	public static bool Texture(ImDrawListPtr drawList, IDalamudTextureWrap? texture, Vector2 min, float size, float alpha = 1f)
	{
		return M3ActionIcon.Image(drawList, texture, min, min + new Vector2(size, size), M3.ShapeExtraSmall, alpha);
	}

	public static bool StatusDot(ImDrawListPtr drawList, Vector2 center, Vector4 color, float radius = 0f)
	{
		var scale = M3.Scale;
		radius = radius > 0f ? radius : MathF.Max(2.5f, 4f * scale);
		drawList.AddCircleFilled(center, radius, M3.U32(color), 16);
		var reach = new Vector2(radius + (3f * scale));
		return MouseOver(center - reach, center + reach);
	}

	public static void Chevron(ImDrawListPtr drawList, Vector2 center, float turn, Vector4 color, float arm = 0f)
	{
		var scale = M3.Scale;
		arm = arm > 0f ? arm : 3.5f * scale;
		var (sin, cos) = MathF.SinCos(turn * MathF.PI * 0.5f);

		Vector2 Rotate(float px, float py)
		{
			return center + new Vector2((px * cos) - (py * sin), (px * sin) + (py * cos));
		}

		var tip = Rotate(arm * 0.6f, 0f);
		var thickness = MathF.Max(1f, 1.75f * scale);
		drawList.AddLine(Rotate(-arm * 0.6f, -arm), tip, M3.U32(color), thickness);
		drawList.AddLine(tip, Rotate(-arm * 0.6f, arm), M3.U32(color), thickness);
	}

	public static float WrappedText(string text, Vector2 position, float wrapWidth, Vector4 color)
	{
		ImGui.SetCursorScreenPos(position);
		using (ImRaii.PushColor(ImGuiCol.Text, color))
		{
			ImGui.PushTextWrapPos(ImGui.GetCursorPosX() + wrapWidth);
			ImGui.TextUnformatted(text);
			ImGui.PopTextWrapPos();
		}

		return ImGui.GetItemRectMax().Y;
	}

	private static float SmoothFade(float t)
	{
		return 1f - (t * t * (3f - (2f * t)));
	}
}

public static class M3Motion
{
	private const int MaxTrackedValues = 512;
	private static readonly M3FrameCache<float> _values = new(MaxTrackedValues);
	public const float FastDuration = 0.14f;
	public const float EmphasisedDuration = 0.28f;

	public static float Scaled(float duration)
	{
		return duration * M3.MotionScale;
	}

	public static float Approach(uint id, float target, float duration)
	{
		duration = Scaled(duration);
		if (_values.TryGet(id, out var current))
		{
			if (current == target)
			{
				return target;
			}

			if (MathF.Abs(target - current) >= 0.001f)
			{
				var delta = ImGui.GetIO().DeltaTime;
				var step = duration <= 0f ? 1f : 1f - MathF.Exp(-delta / (duration * 0.35f));
				current = float.Lerp(current, target, Math.Clamp(step, 0f, 1f));
				_values.Set(id, current);
				return current;
			}
		}

		_values.Set(id, target);
		return target;
	}

	public static float Approach(string id, float target, float duration)
	{
		return Approach(ImGui.GetID(id), target, duration);
	}

	public static float Step(float current, float target, float duration)
	{
		duration = Scaled(duration);
		if (duration <= 0f || MathF.Abs(target - current) < 0.001f)
		{
			return target;
		}

		var step = 1f - MathF.Exp(-ImGui.GetIO().DeltaTime / (duration * 0.35f));
		return float.Lerp(current, target, Math.Clamp(step, 0f, 1f));
	}

	public static void Reset()
	{
		_values.Clear();
	}
}
