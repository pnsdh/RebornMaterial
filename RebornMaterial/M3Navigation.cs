using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

// Section puts a heading above the first item of each group in the drawer, and a divider in the rail.
public readonly record struct M3NavItem(
	string Id,
	string Label,
	FontAwesomeIcon Icon,
	bool Selected,
	string? Tooltip = null,
	Vector4? Accent = null,
	string? Badge = null,
	bool SeparatorAfter = false,
	string? Section = null,
	IDalamudTextureWrap? Texture = null);

public readonly record struct M3Tab(
	string Label,
	FontAwesomeIcon Icon = FontAwesomeIcon.None,
	string? Badge = null,
	string? Tooltip = null);

public static class M3Navigation
{
	public const float DrawerBreakpoint = 128f;

	private static readonly Dictionary<uint, float> _tabScroll = [];
	private static readonly Dictionary<uint, int> _tabShown = [];

	private static float DrawerRowHeight => M3Style.Density == M3Density.Tight ? M3.FitText(36f, 8f) : M3.FitText(48f, 12f);

	private static float RailItemHeight
	{
		get
		{
			using var font = ImRaii.PushFont(M3.LabelSmall);
			return MathF.Max(58f * M3.Scale, (44f * M3.Scale) + ImGui.GetTextLineHeight());
		}
	}

	public static string? Draw(string id, IReadOnlyList<M3NavItem> items, bool expanded)
	{
		string? clicked = null;
		string? section = null;

		using var idScope = ImRaii.PushId(id);
		for (var i = 0; i < items.Count; i++)
		{
			var item = items[i];
			if (item.Section != null && item.Section != section)
			{
				SectionHeader(item.Section, expanded, i > 0 && !items[i - 1].SeparatorAfter);
			}

			section = item.Section;

			if (expanded ? DrawerItem(item) : RailItem(item))
			{
				clicked = item.Id;
			}

			if (item.SeparatorAfter)
			{
				M3Widgets.Divider(6f);
			}
		}

		return clicked;
	}

	private static void SectionHeader(string text, bool expanded, bool divider)
	{
		if (divider)
		{
			M3Widgets.Divider(6f);
		}

		if (!expanded)
		{
			return;
		}

		var scale = M3.Scale;
		ImGui.Dummy(new Vector2(0f, 2f * scale));
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + M3Style.Spacing(16f, 12f));
		M3Text.Draw(Truncate(text, ImGui.GetContentRegionAvail().X - M3Style.Spacing(16f, 12f)), M3TextStyle.Muted);
		ImGui.Dummy(new Vector2(0f, 2f * scale));
	}

	private static Vector2 ItemIconSize(in M3NavItem item, float textureSize)
	{
		return item.Texture != null ? new Vector2(textureSize, textureSize) : M3Draw.MeasureIcon(item.Icon);
	}

	private static void ItemIcon(ImDrawListPtr drawList, in M3NavItem item, Vector2 position, float textureSize, Vector4 color)
	{
		if (item.Texture == null || !M3Draw.Texture(drawList, item.Texture, position, textureSize))
		{
			M3Draw.Icon(drawList, item.Icon, position, color);
		}
	}

	private static bool DrawerItem(in M3NavItem item)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var tone = item.Accent ?? s.Primary;
		var width = MathF.Max(32f * scale, ImGui.GetContentRegionAvail().X);
		var height = DrawerRowHeight;
		var textureSize = MathF.Max(20f * scale, ImGui.GetTextLineHeight());

		var pressed = ImGui.InvisibleButton($"nav_{item.Id}", new Vector2(width, height));
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = height * 0.5f;

		var selection = M3Motion.Approach($"nav_sel_{item.Id}", item.Selected ? 1f : 0f, M3Motion.EmphasisedDuration);

		if (selection > 0.01f)
		{
			drawList.AddRectFilled(min, max, M3.U32(s.SecondaryContainer, 0.95f * selection), rounding);
		}

		if (hovered || held)
		{
			drawList.AddRectFilled(min, max, M3.U32(s.OnSurface, held ? M3.StatePressed : M3.StateHover), rounding);
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		var iconColor = M3ColorMath.Mix(s.OnSurfaceVariant, tone, selection);
		var textColor = M3ColorMath.Mix(M3.Alpha(s.OnSurfaceVariant, 0.95f), s.OnSecondaryContainer, selection);

		var iconSize = ItemIconSize(item, textureSize);
		var iconX = min.X + M3Style.Spacing(16f, 12f);
		ItemIcon(drawList, item, new Vector2(iconX, min.Y + ((height - iconSize.Y) * 0.5f)), textureSize, iconColor);

		var textX = iconX + MathF.Max(iconSize.X, 18f * scale) + M3Style.Spacing(14f, 10f);
		var available = MathF.Max(16f * scale, max.X - textX - (12f * scale));
		var label = Truncate(item.Label, available);
		var labelSize = ImGui.CalcTextSize(label);
		drawList.AddText(new Vector2(textX, min.Y + ((height - labelSize.Y) * 0.5f)), M3.U32(textColor), label);

		if (!string.IsNullOrEmpty(item.Badge))
		{
			using var badgeFont = ImRaii.PushFont(M3.LabelSmall);
			var badgeSize = ImGui.CalcTextSize(item.Badge);
			var padding = new Vector2(6f, 2f) * scale;
			var badgeMax = new Vector2(max.X - (12f * scale), min.Y + ((height + badgeSize.Y + (padding.Y * 2f)) * 0.5f));
			var badgeMin = badgeMax - badgeSize - (padding * 2f);
			drawList.AddRectFilled(badgeMin, badgeMax, M3.U32(s.Error, 0.9f), M3.ShapeFull);
			drawList.AddText(badgeMin + padding, M3.U32(s.OnError), item.Badge);
		}

		M3Draw.FocusRing(min, max, rounding);

		if (hovered)
		{
			M3Tooltip.Show(item.Tooltip ?? (label != item.Label ? item.Label : null));
		}

		return pressed;
	}

	private static bool RailItem(in M3NavItem item)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var tone = item.Accent ?? s.Primary;
		var width = MathF.Max(32f * scale, ImGui.GetContentRegionAvail().X);
		var height = RailItemHeight;

		var pressed = ImGui.InvisibleButton($"nav_{item.Id}", new Vector2(width, height));
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = ImGui.IsItemActive();
		var min = ImGui.GetItemRectMin();
		var drawList = ImGui.GetWindowDrawList();
		var selection = M3Motion.Approach($"nav_sel_{item.Id}", item.Selected ? 1f : 0f, M3Motion.EmphasisedDuration);

		var indicatorSize = new Vector2(MathF.Min(56f * scale, width - (4f * scale)), 32f * scale);
		var indicatorMin = new Vector2(min.X + ((width - indicatorSize.X) * 0.5f), min.Y + (4f * scale));
		var indicatorMax = indicatorMin + indicatorSize;

		if (selection > 0.01f)
		{
			var grow = (1f - selection) * 6f * scale;
			drawList.AddRectFilled(
				indicatorMin + new Vector2(grow, 0f),
				indicatorMax - new Vector2(grow, 0f),
				M3.U32(s.SecondaryContainer, 0.95f * selection), indicatorSize.Y * 0.5f);
		}

		if (hovered || held)
		{
			drawList.AddRectFilled(indicatorMin, indicatorMax,
				M3.U32(s.OnSurface, held ? M3.StatePressed : M3.StateHover), indicatorSize.Y * 0.5f);
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		var iconColor = M3ColorMath.Mix(s.OnSurfaceVariant, tone, selection);
		var textureSize = 24f * scale;
		var iconSize = ItemIconSize(item, textureSize);
		ItemIcon(drawList, item, indicatorMin + ((indicatorSize - iconSize) * 0.5f), textureSize, iconColor);

		using (ImRaii.PushFont(M3.LabelSmall))
		{
			var label = Truncate(item.Label, width - (4f * scale));
			var labelSize = ImGui.CalcTextSize(label);
			var textColor = M3ColorMath.Mix(M3.Alpha(s.OnSurfaceVariant, 0.9f), s.OnSurface, selection);
			drawList.AddText(
				new Vector2(min.X + ((width - labelSize.X) * 0.5f), indicatorMax.Y + (4f * scale)),
				M3.U32(textColor), label);
		}

		M3Draw.FocusRing(indicatorMin, indicatorMax, indicatorSize.Y * 0.5f);

		if (hovered)
		{
			var tip = string.IsNullOrEmpty(item.Tooltip) ? item.Label : $"{item.Label}\n \n{item.Tooltip}";
			M3Tooltip.Show(tip);
		}

		return pressed;
	}

	private static float TabContentWidth(in M3Tab tab, bool stacked)
	{
		var scale = M3.Scale;
		var label = ImGui.CalcTextSize(tab.Label).X;
		var icon = tab.Icon == FontAwesomeIcon.None ? 0f : M3Draw.MeasureIcon(tab.Icon).X;
		var content = stacked ? MathF.Max(label, icon) : label + (icon > 0f ? icon + (8f * scale) : 0f);
		var badge = string.IsNullOrEmpty(tab.Badge) ? 0f : (M3Widgets.BadgeSize(tab.Badge).X + (4f * scale)) * 2f;
		return content + badge;
	}

	// When the labels won't fit in equal shares, the row scrolls instead of shortening them.
	public static int Tabs(string id, ReadOnlySpan<M3Tab> tabs, int selectedIndex, bool secondary = false, float? width = null)
	{
		if (tabs.Length == 0)
		{
			return -1;
		}

		var s = M3.Scheme;
		var scale = M3.Scale;

		var stacked = false;
		foreach (var tab in tabs)
		{
			stacked |= !secondary && tab.Icon != FontAwesomeIcon.None;
		}

		var lineHeight = ImGui.GetTextLineHeight();
		var height = stacked
			? MathF.Max(64f * scale, (lineHeight * 2f) + (26f * scale))
			: M3.FitText(48f, 12f);
		var total = width ?? MathF.Max(32f * scale, ImGui.GetContentRegionAvail().X);
		var origin = ImGui.GetCursorScreenPos();
		var drawList = ImGui.GetWindowDrawList();
		var result = -1;

		using var idScope = ImRaii.PushId(id);
		var key = ImGui.GetID("##strip");

		Span<float> widths = tabs.Length <= 64 ? stackalloc float[tabs.Length] : new float[tabs.Length];
		var share = total / tabs.Length;
		var scrolling = false;
		for (var i = 0; i < tabs.Length; i++)
		{
			var content = TabContentWidth(tabs[i], stacked);
			scrolling |= content + (16f * scale) > share;
			widths[i] = MathF.Max(72f * scale, content + (32f * scale));
		}

		var naturalTotal = 0f;
		foreach (var tabWidth in widths)
		{
			naturalTotal += tabWidth;
		}

		if (!scrolling)
		{
			widths.Fill(share);
		}
		else if (naturalTotal <= total || tabs.Length == 1)
		{
			for (var i = 0; i < tabs.Length; i++)
			{
				widths[i] *= total / naturalTotal;
			}

			scrolling = false;
		}

		var arrow = scrolling ? 32f * scale : 0f;
		var viewLeft = origin.X + arrow;
		var viewWidth = MathF.Max(1f, total - (arrow * 2f));

		Span<float> lefts = tabs.Length <= 64 ? stackalloc float[tabs.Length] : new float[tabs.Length];
		var contentWidth = 0f;
		for (var i = 0; i < tabs.Length; i++)
		{
			lefts[i] = contentWidth;
			contentWidth += widths[i];
		}

		var maxScroll = MathF.Max(0f, contentWidth - viewWidth);
		_ = _tabScroll.TryGetValue(key, out var target);

		var selectionChanged = !_tabShown.TryGetValue(key, out var shown) || shown != selectedIndex;
		_tabShown[key] = selectedIndex;
		if (scrolling && selectionChanged && selectedIndex >= 0 && selectedIndex < tabs.Length)
		{
			target = ScrollToShow(target, lefts[selectedIndex], widths[selectedIndex], viewWidth);
		}

		if (scrolling && ImGui.IsMouseHoveringRect(origin, origin + new Vector2(total, height)) && ImGui.IsWindowHovered())
		{
			var io = ImGui.GetIO();
			var wheel = io.MouseWheelH != 0f ? io.MouseWheelH : ImGui.GetScrollMaxY() <= 0f ? -io.MouseWheel : 0f;
			target += wheel * 60f * scale;
		}

		if (scrolling)
		{
			var diameter = 28f * scale;
			var top = origin.Y + ((height - diameter) * 0.5f);
			ImGui.SetCursorScreenPos(new Vector2(origin.X + ((arrow - diameter) * 0.5f), top));
			if (M3Widgets.IconButton("##scroll_left", FontAwesomeIcon.ChevronLeft, null, diameter: diameter, enabled: target > 0.5f))
			{
				target -= viewWidth * 0.7f;
			}

			ImGui.SetCursorScreenPos(new Vector2(origin.X + total - arrow + ((arrow - diameter) * 0.5f), top));
			if (M3Widgets.IconButton("##scroll_right", FontAwesomeIcon.ChevronRight, null, diameter: diameter, enabled: target < maxScroll - 0.5f))
			{
				target += viewWidth * 0.7f;
			}
		}

		target = Math.Clamp(target, 0f, maxScroll);
		_tabScroll[key] = target;
		var scroll = scrolling ? M3Motion.Approach(key, target, M3Motion.EmphasisedDuration) : 0f;
		var contentOrigin = viewLeft - scroll;

		var indicatorX = 0f;
		var indicatorWidth = 0f;

		ImGui.PushClipRect(new Vector2(viewLeft, origin.Y), new Vector2(viewLeft + viewWidth, origin.Y + height), true);
		for (var i = 0; i < tabs.Length; i++)
		{
			var tab = tabs[i];
			var tabWidth = widths[i];
			var selected = i == selectedIndex;
			var tabMin = new Vector2(contentOrigin + lefts[i], origin.Y);
			var tabMax = tabMin + new Vector2(tabWidth, height);

			ImGui.SetCursorScreenPos(tabMin);
			if (ImGui.InvisibleButton($"##tab{i}", new Vector2(tabWidth, height)) && !selected)
			{
				result = i;
			}

			var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
			var held = ImGui.IsItemActive();
			if (hovered || held)
			{
				drawList.AddRectFilled(tabMin, tabMax, M3.U32(selected && !secondary ? s.Primary : s.OnSurface,
					held ? M3.StatePressed : M3.StateHover));
				ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			}

			var content = !selected ? s.OnSurfaceVariant : secondary ? s.OnSurface : s.Primary;
			var hasIcon = tab.Icon != FontAwesomeIcon.None;
			var iconSize = hasIcon ? M3Draw.MeasureIcon(tab.Icon) : Vector2.Zero;
			var inlineGap = hasIcon && !stacked ? 8f * scale : 0f;
			var inlineIcon = hasIcon && !stacked ? iconSize.X + inlineGap : 0f;

			var badgeSize = string.IsNullOrEmpty(tab.Badge) ? Vector2.Zero : M3Widgets.BadgeSize(tab.Badge);
			var badgeRoom = badgeSize.X > 0f ? (badgeSize.X + (4f * scale)) * 2f : 0f;
			var label = Truncate(tab.Label, MathF.Max(8f * scale, tabWidth - (16f * scale) - inlineIcon - badgeRoom));
			var labelSize = ImGui.CalcTextSize(label);

			float contentLeft;
			float contentSpan;
			Vector2 labelPosition;
			if (stacked)
			{
				var blockHeight = (hasIcon ? iconSize.Y + (4f * scale) : 0f) + labelSize.Y;
				var top = tabMin.Y + ((height - blockHeight) * 0.5f) - (1.5f * scale);
				if (hasIcon)
				{
					M3Draw.Icon(drawList, tab.Icon, new Vector2(tabMin.X + ((tabWidth - iconSize.X) * 0.5f), top), content);
					top += iconSize.Y + (4f * scale);
				}

				labelPosition = new Vector2(tabMin.X + ((tabWidth - labelSize.X) * 0.5f), top);
				contentSpan = MathF.Max(labelSize.X, iconSize.X);
				contentLeft = tabMin.X + ((tabWidth - contentSpan) * 0.5f);
			}
			else
			{
				contentSpan = inlineIcon + labelSize.X;
				contentLeft = tabMin.X + ((tabWidth - contentSpan) * 0.5f);
				if (hasIcon)
				{
					M3Draw.Icon(drawList, tab.Icon, new Vector2(contentLeft, tabMin.Y + ((height - iconSize.Y) * 0.5f)), content);
				}

				labelPosition = new Vector2(contentLeft + inlineIcon, tabMin.Y + ((height - labelSize.Y) * 0.5f));
			}

			drawList.AddText(labelPosition, M3.U32(content), label);

			if (badgeSize.X > 0f)
			{
				M3Widgets.BadgeAt(new Vector2(labelPosition.X + labelSize.X + (4f * scale) + (badgeSize.X * 0.5f), labelPosition.Y + (badgeSize.Y * 0.5f)), tab.Badge);
			}

			// Kept relative to the start of the tabs, so the indicator doesn't trail behind while they scroll.
			if (selected)
			{
				indicatorWidth = secondary ? tabWidth : MathF.Max(contentSpan, 24f * scale);
				indicatorX = secondary ? lefts[i] : contentLeft - contentOrigin + ((contentSpan - indicatorWidth) * 0.5f);
			}

			M3Draw.FocusRing(tabMin, tabMax, M3.ShapeExtraSmall);
			if (scrolling && ImGui.IsItemFocused())
			{
				_tabScroll[key] = Math.Clamp(ScrollToShow(target, lefts[i], tabWidth, viewWidth), 0f, maxScroll);
			}

			if (hovered)
			{
				M3Tooltip.Show(tab.Tooltip ?? (label != tab.Label ? tab.Label : null));
			}
		}

		if (selectedIndex >= 0 && selectedIndex < tabs.Length)
		{
			var x = M3Motion.Approach("indicator_x", indicatorX, M3Motion.EmphasisedDuration);
			var w = M3Motion.Approach("indicator_w", indicatorWidth, M3Motion.EmphasisedDuration);
			var thickness = (secondary ? 2f : 3f) * scale;
			var indicatorMin = new Vector2(contentOrigin + x, origin.Y + height - thickness);
			drawList.AddRectFilled(indicatorMin, indicatorMin + new Vector2(w, thickness), M3.U32(s.Primary),
				secondary ? 0f : thickness, secondary ? ImDrawFlags.None : ImDrawFlags.RoundCornersTop);
		}

		ImGui.PopClipRect();

		var divider = 1f * scale;
		drawList.AddLine(new Vector2(origin.X, origin.Y + height - (divider * 0.5f)), new Vector2(origin.X + total, origin.Y + height - (divider * 0.5f)),
			M3.U32(s.OutlineVariant, 0.8f), divider);

		ImGui.SetCursorScreenPos(origin);
		ImGui.Dummy(new Vector2(total, height));
		return result;
	}

	private static float ScrollToShow(float scroll, float left, float width, float viewWidth)
	{
		var margin = 24f * M3.Scale;
		if (left < scroll)
		{
			return left - margin;
		}

		return left + width > scroll + viewWidth ? left + width - viewWidth + margin : scroll;
	}

	internal static void Reset()
	{
		_tabScroll.Clear();
		_tabShown.Clear();
	}

	public static string Truncate(string text, float maxWidth)
	{
		const string Ellipsis = "…";
		if (string.IsNullOrEmpty(text) || ImGui.CalcTextSize(text).X <= maxWidth)
		{
			return text;
		}

		var room = maxWidth - ImGui.CalcTextSize(Ellipsis).X;
		var low = 0;
		var high = text.Length - 1;
		while (low < high)
		{
			var mid = (low + high + 1) / 2;
			if (ImGui.CalcTextSize(text.AsSpan(0, mid)).X <= room)
			{
				low = mid;
			}
			else
			{
				high = mid - 1;
			}
		}

		if (low > 0 && char.IsHighSurrogate(text[low - 1]))
		{
			low--;
		}

		var kept = text.AsSpan(0, low).TrimEnd();
		return kept.IsEmpty ? Ellipsis : string.Concat(kept, Ellipsis);
	}
}
