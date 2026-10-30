using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static partial class M3Widgets
{
	public static float ComboHeight => M3.FitText(38f, 8f);

	private static float ComboTextInset => 12f * M3.Scale;
	private static float ComboChevronWidth => 24f * M3.Scale;

	private static readonly Dictionary<uint, (int Count, float Widest)> _comboWidths = [];
	private static readonly Dictionary<uint, string> _comboQueries = [];
	private static readonly List<int> _comboMatches = [];

	private static void ResetCombos()
	{
		_comboQueries.Clear();
		_comboMatches.Clear();
		_comboWidths.Clear();
		_enumLabels.Clear();
		_enumValues.Clear();
	}

	public static float ComboWidthFor(string label)
	{
		return MathF.Ceiling(ImGui.CalcTextSize(label).X) + (ComboTextInset * 2f) + ComboChevronWidth + 1f;
	}

	public static bool Combo(string id, ref int index, IReadOnlyList<string> items, float width, string? emptyText = null, bool search = false, bool enabled = true, string? tooltip = null)
	{
		return Combo(id, ref index, items.Count, i => items[i], width, emptyText, search, enabled, tooltip);
	}

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

		if (pickFirst)
		{
			foreach (var match in _comboMatches)
			{
				if (itemEnabled?.Invoke(match) != false)
				{
					index = match;
					ImGui.CloseCurrentPopup();
					return true;
				}
			}
		}

		var changed = false;

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
		var index = Array.IndexOf(values, value);
		Func<int, string> labels = itemLabel == null ? static i => EnumValues<T>.Labels[i] : i => itemLabel(values[i]);
		var unlisted = index < 0 ? itemLabel?.Invoke(value) ?? EnumLabel(value) : null;
		if (!Combo(id, ref index, values.Length, labels, width, unlisted, search, enabled, tooltip,
			itemTooltip == null ? null : i => itemTooltip(values[i]),
			null,
			itemVisible == null ? null : i => itemVisible(values[i])))
		{
			return false;
		}

		value = values[index];
		return true;
	}

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

		public static readonly string[] Labels = Array.ConvertAll(All, value => EnumLabel(value));
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
}
