using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public enum M3TextStyle
{
	Body,

	Muted,

	Faint,

	Accent,

	Title,

	Heading,

	Label,

	Error,

	Warning,

	Success,

	Info,
}

public static class M3Text
{
	public static void Draw(string text, M3TextStyle style = M3TextStyle.Body, bool wrap = false)
	{
		using var scope = Push(style);
		if (wrap)
		{
			ImGui.TextWrapped(text);
		}
		else
		{
			ImGui.TextUnformatted(text);
		}
	}

	// A line of text after a dot. Wrapped lines hang under the text, not the dot.
	public static void Bullet(string text, M3TextStyle style = M3TextStyle.Body, bool wrap = true, Vector4? dot = null)
	{
		var scale = M3.Scale;
		var indent = 22f * scale;

		using var scope = Push(style);
		var origin = ImGui.GetCursorScreenPos();
		ImGui.GetWindowDrawList().AddCircleFilled(
			new Vector2(origin.X + (9f * scale), origin.Y + (ImGui.GetTextLineHeight() * 0.5f)),
			2.5f * scale, M3.U32(dot ?? M3.Scheme.Primary, 0.9f), 12);

		ImGui.Indent(indent);
		if (wrap)
		{
			ImGui.TextWrapped(text);
		}
		else
		{
			ImGui.TextUnformatted(text);
		}

		ImGui.Unindent(indent);
	}

	public static Vector2 Measure(string text, M3TextStyle style = M3TextStyle.Body)
	{
		var font = FontOf(style);
		using var pushed = ImRaii.PushFont(font.GetValueOrDefault(), font.HasValue);
		return ImGui.CalcTextSize(text);
	}

	public static Vector4 ColorOf(M3TextStyle style)
	{
		var s = M3.Scheme;
		return style switch
		{
			M3TextStyle.Muted or M3TextStyle.Label => s.OnSurfaceVariant,
			M3TextStyle.Faint => M3.Alpha(s.OnSurfaceVariant, 0.6f),
			M3TextStyle.Accent => s.Primary,
			M3TextStyle.Error => s.Error,
			M3TextStyle.Warning => s.Warning,
			M3TextStyle.Success => s.Success,
			M3TextStyle.Info => s.Info,
			_ => s.OnSurface,
		};
	}

	public static ImFontPtr? FontOf(M3TextStyle style)
	{
		return style switch
		{
			M3TextStyle.Title => M3.TitleMedium,
			M3TextStyle.Heading => M3.TitleLarge,
			M3TextStyle.Label => M3.LabelSmall,
			_ => null,
		};
	}

	// Applies a style to any ImGui text drawn inside it, such as BulletText or table cells.
	public static Scope Push(M3TextStyle style)
	{
		var font = FontOf(style);
		if (font is { } pushed)
		{
			ImGui.PushFont(pushed);
		}

		ImGui.PushStyleColor(ImGuiCol.Text, ColorOf(style));
		return new Scope(true, font.HasValue);
	}

	public readonly struct Scope(bool color, bool font) : IDisposable
	{
		public void Dispose()
		{
			if (color)
			{
				ImGui.PopStyleColor();
			}

			if (font)
			{
				ImGui.PopFont();
			}
		}
	}
}
