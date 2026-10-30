using Dalamud.Game.ClientState.Keys;
using Dalamud.Interface.Utility;

namespace RebornMaterial;

public readonly record struct M3Hotkey(VirtualKey Key, bool Ctrl = false, bool Shift = false, bool Alt = false)
{
	public static readonly M3Hotkey None = new(VirtualKey.NO_KEY);

	public bool IsEmpty => Key == VirtualKey.NO_KEY;

	public override string ToString()
	{
		if (IsEmpty)
		{
			return M3Fork.T("None");
		}

		var prefix = (Ctrl ? "Ctrl+" : string.Empty) + (Shift ? "Shift+" : string.Empty) + (Alt ? "Alt+" : string.Empty);
		return prefix + Key.GetFancyName();
	}
}

public static partial class M3Widgets
{
	private static uint _listeningHotkey;
	private static int _listeningFrame;
	private static ImGuiKey[]? _hotkeyCandidates;

	public static float HotkeyFieldHeight => ComboHeight;

	public static bool HotkeyField(string id, ref M3Hotkey hotkey, float width, bool modifiers = true, bool enabled = true, string? tooltip = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = HotkeyFieldHeight;
		var key = ImGui.GetID(id);
		var frame = ImGui.GetFrameCount();
		var listening = _listeningHotkey == key && _listeningFrame >= frame - 1;
		if (_listeningHotkey == key && !listening)
		{
			_listeningHotkey = 0;
		}

		if (listening)
		{
			_listeningFrame = frame;
		}

		var clearWidth = hotkey.IsEmpty || listening ? 0f : height;

		var clicked = ImGui.InvisibleButton(id, new Vector2(width - clearWidth, height)) && enabled;
		var mouseOver = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var hovered = enabled && mouseOver;
		var held = enabled && ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = min + new Vector2(width, height);
		M3Draw.FocusRing(min, ImGui.GetItemRectMax(), M3.ShapeSmall);

		if (clicked)
		{
			listening = !listening;
			_listeningHotkey = listening ? key : 0;
			_listeningFrame = frame;
		}

		var changed = false;
		if (listening)
		{
			if (!enabled || ImGui.IsKeyPressed(ImGuiKey.Escape) || (ImGui.IsMouseClicked(ImGuiMouseButton.Left) && !mouseOver))
			{
				_listeningHotkey = 0;
				listening = false;
			}
			else if (TryReadHotkey(modifiers, out var pressed))
			{
				hotkey = pressed;
				changed = true;
				_listeningHotkey = 0;
				listening = false;
			}
		}

		var drawList = ImGui.GetWindowDrawList();
		var dim = enabled ? 1f : M3.DisabledContent;
		var fill = M3.Alpha(s.SurfaceContainerHighest, enabled ? 0.55f : 0.25f);
		if (hovered || held)
		{
			fill = M3.StateLayer(fill, s.OnSurface, hovered, held);
		}

		drawList.AddRectFilled(min, max, M3.U32(fill), M3.ShapeSmall);
		drawList.AddRect(min, max, M3.U32(listening ? s.Primary : s.Outline, (listening ? 1f : 0.75f) * dim), M3.ShapeSmall,
			ImDrawFlags.None, (listening ? 2f : 1f) * scale);

		var iconSize = M3Draw.MeasureIcon(FontAwesomeIcon.Keyboard);
		var textX = min.X + (12f * scale);
		M3Draw.Icon(drawList, FontAwesomeIcon.Keyboard, new Vector2(textX, min.Y + ((height - iconSize.Y) * 0.5f)),
			M3.Alpha(listening ? s.Primary : s.OnSurfaceVariant, dim));
		textX += iconSize.X + (10f * scale);

		var text = listening ? M3Fork.T("Press a key") : hotkey.ToString();
		var color = listening ? M3.Alpha(s.Primary, 0.65f + (0.35f * MathF.Sin((float)ImGui.GetTime() * 5f)))
			: hotkey.IsEmpty ? M3.Alpha(s.OnSurfaceVariant, 0.8f * dim)
			: M3.Alpha(s.OnSurface, 0.95f * dim);
		var display = M3Navigation.Truncate(text, MathF.Max(8f * scale, max.X - clearWidth - textX - (8f * scale)));
		var textSize = ImGui.CalcTextSize(display);
		drawList.AddText(new Vector2(textX, min.Y + ((height - textSize.Y) * 0.5f)), M3.U32(color), display);

		if (clearWidth > 0f)
		{
			var diameter = height - (12f * scale);
			ImGui.SetCursorScreenPos(new Vector2(max.X - clearWidth + ((clearWidth - diameter) * 0.5f), min.Y + ((height - diameter) * 0.5f)));
			if (IconButton($"{id}_clear", FontAwesomeIcon.Times, M3Fork.T("Clear"), diameter: diameter, enabled: enabled))
			{
				hotkey = M3Hotkey.None;
				changed = true;
			}
		}

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		if (mouseOver && !listening)
		{
			M3Tooltip.Show(tooltip);
		}

		ImGui.SetCursorScreenPos(min);
		ImGui.Dummy(new Vector2(width, height));
		return changed;
	}

	public static bool RowHotkey(string label, ref M3Hotkey hotkey, string? supporting = null, bool modifiers = true, bool enabled = true)
	{
		var (display, id) = SplitLabel(label);
		var width = MathF.Min(200f * M3.Scale, M3SettingRow.MaxControlWidth() * 0.6f);
		var row = M3SettingRow.Begin(display, supporting, new Vector2(width, HotkeyFieldHeight), disabled: !enabled);
		ImGui.SetCursorScreenPos(row.ControlPosition);
		var changed = HotkeyField($"##{id}_hotkey", ref hotkey, width, modifiers, enabled);
		M3SettingRow.End(row);
		return changed;
	}

	private static bool TryReadHotkey(bool modifiers, out M3Hotkey hotkey)
	{
		hotkey = default;
		_hotkeyCandidates ??= HotkeyCandidates();

		var io = ImGui.GetIO();
		foreach (var candidate in _hotkeyCandidates)
		{
			if (!ImGui.IsKeyPressed(candidate, false))
			{
				continue;
			}

			var virtualKey = ImGuiHelpers.ImGuiKeyToVirtualKey(candidate);
			if (virtualKey == VirtualKey.NO_KEY)
			{
				continue;
			}

			hotkey = modifiers ? new M3Hotkey(virtualKey, io.KeyCtrl, io.KeyShift, io.KeyAlt) : new M3Hotkey(virtualKey);
			return true;
		}

		return false;
	}

	private static ImGuiKey[] HotkeyCandidates()
	{
		ImGuiKey[] skipped =
		[
			ImGuiKey.Escape,
			ImGuiKey.LeftCtrl, ImGuiKey.RightCtrl,
			ImGuiKey.LeftShift, ImGuiKey.RightShift,
			ImGuiKey.LeftAlt, ImGuiKey.RightAlt,
			ImGuiKey.LeftSuper, ImGuiKey.RightSuper,
		];

		var keys = new List<ImGuiKey>();
		foreach (var key in Enum.GetValues<ImGuiKey>())
		{
			if (key >= ImGuiKey.Tab && key < ImGuiKey.GamepadStart && Array.IndexOf(skipped, key) < 0)
			{
				keys.Add(key);
			}
		}

		return [.. keys];
	}

	private static void ResetHotkeys()
	{
		_listeningHotkey = 0;
	}
}
