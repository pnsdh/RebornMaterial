using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static partial class M3Widgets
{
	private static float TextureIconSize => MathF.Max(18f * M3.Scale, ImGui.GetTextLineHeight());

	private static float IconSlotWidth(FontAwesomeIcon icon, IDalamudTextureWrap? texture)
	{
		return texture != null ? TextureIconSize : icon == FontAwesomeIcon.None ? 0f : M3Draw.MeasureIcon(icon).X;
	}

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

	private static ImRaii.ColorDisposable PushClearFrame()
	{
		return ImRaii.PushColor(ImGuiCol.FrameBg, new Vector4(0f, 0f, 0f, 0f))
			.Push(ImGuiCol.FrameBgHovered, new Vector4(0f, 0f, 0f, 0f))
			.Push(ImGuiCol.FrameBgActive, new Vector4(0f, 0f, 0f, 0f));
	}

	private static (string Label, string Id) SplitLabel(string label)
	{
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
		var plus = false;

		while (cursor < format.Length && (format[cursor] is '-' or '+' or ' ' or '#' || char.IsAsciiDigit(format[cursor])))
		{
			plus |= format[cursor] == '+';
			cursor++;
		}

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

		while (cursor < format.Length && format[cursor] is 'l' or 'h')
		{
			cursor++;
		}

		if (cursor >= format.Length)
		{
			return Unescape(format);
		}

		var rendered = format[cursor] switch
		{
			'f' or 'F' => value.ToString($"F{(hasDigits ? digits : 3)}"),
			'g' or 'G' => value.ToString($"G{(hasDigits ? Math.Max(1, digits) : 6)}"),
			'd' or 'i' or 'u' => ((int)value).ToString(),
			_ => null,
		};

		if (rendered != null && plus && value >= 0f)
		{
			rendered = "+" + rendered;
		}

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

	internal static void Reset()
	{
		ResetButtons();
		ResetCombos();
		ResetRows();
		ResetStructure();
		ResetHotkeys();
		ResetReorder();
	}
}
