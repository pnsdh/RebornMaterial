namespace RebornMaterial;

public static partial class M3Widgets
{
	public static bool RowButton(string label, string buttonLabel, string? supporting = null, M3ButtonStyle style = M3ButtonStyle.Tonal, FontAwesomeIcon icon = FontAwesomeIcon.None, bool enabled = true, string? tooltip = null, float? width = null)
	{
		var (display, id) = RowParts(label, "_button");
		var natural = width is > 0f ? width.Value : ButtonWidth(icon, buttonLabel);
		var w = MathF.Min(natural, M3SettingRow.MaxControlWidth());

		var row = M3SettingRow.Begin(display, supporting, new Vector2(w, ButtonHeight), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var pressed = Button(id, buttonLabel, style, icon, w, enabled, tooltip);
		M3SettingRow.End(row);
		return pressed;
	}

	public static bool RowHoldButton(string label, string buttonLabel, string? supporting = null, float seconds = 1f, M3ButtonStyle style = M3ButtonStyle.Danger, FontAwesomeIcon icon = FontAwesomeIcon.None, bool enabled = true, string? tooltip = null)
	{
		var (display, id) = RowParts(label, "_hold");
		var w = MathF.Min(ButtonWidth(icon, buttonLabel), M3SettingRow.MaxControlWidth());

		var row = M3SettingRow.Begin(display, supporting, new Vector2(w, ButtonHeight), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var done = HoldButton(id, buttonLabel, seconds, style, icon, w, enabled, tooltip);
		M3SettingRow.End(row);
		return done;
	}

	public static bool RowIconButton(string label, FontAwesomeIcon icon, string? supporting = null, string? tooltip = null, M3ButtonStyle style = M3ButtonStyle.Text, bool enabled = true)
	{
		var (display, id) = RowParts(label, "_icon");

		var row = M3SettingRow.Begin(display, supporting, new Vector2(IconButtonSize), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var pressed = IconButton(id, icon, tooltip, style, null, null, enabled);
		M3SettingRow.End(row);
		return pressed;
	}

	public static void RowValue(string label, string value, string? supporting = null, Vector4? color = null, bool enabled = true)
	{
		var display = RowParts(label, string.Empty).Label;
		var maxWidth = M3SettingRow.MaxControlWidth() * 0.6f;
		var size = string.IsNullOrEmpty(value) ? Vector2.Zero : ImGui.CalcTextSize(value, false, maxWidth);
		var width = MathF.Ceiling(size.X);

		var row = M3SettingRow.Begin(display, supporting, new Vector2(width, size.Y), disabled: !enabled);
		if (width > 0f)
		{
			_ = M3Draw.WrappedText(value, row.ControlPosition, width + 1f,
				M3.Alpha(color ?? M3.Scheme.OnSurfaceVariant, enabled ? 1f : M3.DisabledContent));
		}

		M3SettingRow.End(row);
	}
}
