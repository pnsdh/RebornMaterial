namespace RebornMaterial;

public static partial class M3Widgets
{
	private const double CopiedSeconds = 1.5;

	private static readonly Dictionary<uint, double> _copiedAt = [];

	private static void ResetCopy()
	{
		_copiedAt.Clear();
	}

	private static bool IsShowingCopied(uint key)
	{
		if (!_copiedAt.TryGetValue(key, out var at))
		{
			return false;
		}

		if (ImGui.GetTime() - at >= CopiedSeconds)
		{
			_ = _copiedAt.Remove(key);
			return false;
		}

		return true;
	}

	private static bool Copy(uint key, string text, string? snackbar)
	{
		try
		{
			ImGui.SetClipboardText(text);
		}
		catch (Exception)
		{
			return false;
		}

		var now = ImGui.GetTime();
		_copiedAt[key] = now;

		if (_copiedAt.Count > 64)
		{
			foreach (var (entry, at) in _copiedAt)
			{
				if (now - at >= CopiedSeconds)
				{
					_ = _copiedAt.Remove(entry);
				}
			}
		}

		if (snackbar != null)
		{
			M3Snackbar.Show(snackbar);
		}

		return true;
	}

	// Fork: the labels go through M3Fork.T at use, so the "Copy"/"Copied" defaults can be translated.
	public static float CopyButtonWidth(string label = "Copy", string copiedLabel = "Copied")
	{
		return MathF.Max(ButtonWidth(FontAwesomeIcon.Copy, M3Fork.T(label)), ButtonWidth(FontAwesomeIcon.Check, M3Fork.T(copiedLabel)));
	}

	public static bool CopyButton(string id, string text, string label = "Copy", string copiedLabel = "Copied", M3ButtonStyle style = M3ButtonStyle.Tonal, float? width = null, bool enabled = true, string? tooltip = null, string? snackbar = null)
	{
		var key = ImGui.GetID(id);
		var copied = IsShowingCopied(key);
		var w = width ?? CopyButtonWidth(label, copiedLabel);

		var pressed = Button(id, M3Fork.T(copied ? copiedLabel : label), style, copied ? FontAwesomeIcon.Check : FontAwesomeIcon.Copy, w, enabled, tooltip);
		return pressed && Copy(key, text, snackbar);
	}

	public static bool CopyIconButton(string id, string text, string? tooltip = "Copy to clipboard", M3ButtonStyle style = M3ButtonStyle.Text, float? diameter = null, bool enabled = true, string? snackbar = null)
	{
		var key = ImGui.GetID(id);
		var copied = IsShowingCopied(key);

		var pressed = IconButton(id, copied ? FontAwesomeIcon.Check : FontAwesomeIcon.Copy, copied ? M3Fork.T("Copied") : tooltip is null ? null : M3Fork.T(tooltip), style, copied ? M3.Scheme.Success : null, diameter, enabled);
		return pressed && Copy(key, text, snackbar);
	}

	public static bool RowCopy(string label, string text, string? supporting = null, bool iconOnly = false, bool enabled = true, string? snackbar = null)
	{
		var (display, id) = RowParts(label, "_copy");

		bool copied;
		M3RowInfo row;
		if (iconOnly)
		{
			row = M3SettingRow.Begin(display, supporting, new Vector2(IconButtonSize), disabled: !enabled);
			ImGui.SetCursorScreenPos(row.ControlPosition);
			copied = CopyIconButton(id, text, enabled: enabled, snackbar: snackbar);
		}
		else
		{
			var w = CopyButtonWidth();
			row = M3SettingRow.Begin(display, supporting, new Vector2(w, ButtonHeight), disabled: !enabled);
			ImGui.SetCursorScreenPos(row.ControlPosition);
			copied = CopyButton(id, text, width: w, enabled: enabled, snackbar: snackbar);
		}

		M3SettingRow.End(row);
		return copied;
	}
}
