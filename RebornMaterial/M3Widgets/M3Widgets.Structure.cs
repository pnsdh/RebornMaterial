using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static partial class M3Widgets
{
	private const int MaxSectionLabels = 256;

	private static readonly Dictionary<string, string> _sectionLabels = [];

	private static void ResetStructure()
	{
		_sectionLabels.Clear();
	}

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

		if (!_sectionLabels.TryGetValue(text, out var label))
		{
			if (_sectionLabels.Count >= MaxSectionLabels)
			{
				_sectionLabels.Clear();
			}

			label = text.ToUpperInvariant();
			_sectionLabels[text] = label;
		}

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

	public static void HelpMarker(string text, FontAwesomeIcon icon = FontAwesomeIcon.InfoCircle, Vector4? color = null)
	{
		var size = new Vector2(HelpMarkerWidth(icon), ImGui.GetFrameHeight());
		ImGui.Dummy(size);
		var min = ImGui.GetItemRectMin();
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled);
		var tone = color ?? M3.Scheme.OnSurfaceVariant;

		M3Draw.IconCentered(ImGui.GetWindowDrawList(), icon, min, min + size, M3.Alpha(tone, (hovered ? 1f : 0.7f) * tone.W));
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
		hovered = M3Draw.MouseOver(min, max);

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
}
