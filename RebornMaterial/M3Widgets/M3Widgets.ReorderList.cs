using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static partial class M3Widgets
{
	private static uint _reorderList;
	private static int _reorderIndex = -1;

	public static float ReorderRowHeight => M3.FitText(36f, 8f);

	public static bool ReorderList<T>(string id, IList<T> items, Func<T, string> label, ref int selected, Func<T, string?>? tooltip = null, bool enabled = true)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = ReorderRowHeight;
		var gap = 2f * scale;
		var width = MathF.Max(32f * scale, ImGui.GetContentRegionAvail().X);
		var listKey = ImGui.GetID(id);
		var ours = _reorderList == listKey && _reorderIndex >= 0;
		var dragging = ours && ImGui.IsMouseDragging(ImGuiMouseButton.Left, 4f * scale);
		var moved = false;

		using var idScope = ImRaii.PushId(id);
		using var spacing = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing with { Y = gap });
		var top = ImGui.GetCursorScreenPos().Y;
		var drawList = ImGui.GetWindowDrawList();
		var gripWidth = M3Draw.MeasureIcon(FontAwesomeIcon.GripVertical).X;

		for (var i = 0; i < items.Count; i++)
		{
			var item = items[i];
			var pressed = ImGui.InvisibleButton($"##row{i}", new Vector2(width, height), (ImGuiButtonFlags)ImGuiButtonFlagsPrivate.PressedOnClick) && enabled;
			var mouseOver = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
			var hovered = enabled && mouseOver && !dragging;
			var focused = ImGui.IsItemFocused();
			var min = ImGui.GetItemRectMin();
			var max = ImGui.GetItemRectMax();
			var lifted = dragging && i == _reorderIndex;

			if (pressed)
			{
				selected = i;
				_reorderList = listKey;
				_reorderIndex = i;
			}

			if (enabled && focused && !moved && ImGui.GetIO().KeyCtrl)
			{
				var target = ImGui.IsKeyPressed(ImGuiKey.UpArrow) ? i - 1 : ImGui.IsKeyPressed(ImGuiKey.DownArrow) ? i + 1 : i;
				if (target != i && target >= 0 && target < items.Count)
				{
					Move(items, i, target, ref selected);
					ImGuiP.SetFocusID(ImGui.GetID($"##row{target}"), ImGuiP.GetCurrentWindow());
					moved = true;
				}
			}

			if (lifted)
			{
				M3Draw.Elevation(drawList, min, max, M3.ShapeSmall, 2);
				drawList.AddRectFilled(min, max, M3.U32(s.SurfaceContainerHighest), M3.ShapeSmall);
			}
			else if (i == selected)
			{
				drawList.AddRectFilled(min, max, M3.U32(s.SecondaryContainer, enabled ? 0.8f : 0.3f), M3.ShapeSmall);
			}

			if (hovered)
			{
				drawList.AddRectFilled(min, max, M3.U32(s.OnSurface, M3.StateHover), M3.ShapeSmall);
				ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			}

			var dim = enabled ? 1f : M3.DisabledContent;
			var gripX = min.X + (10f * scale);
			M3Draw.IconCentered(drawList, FontAwesomeIcon.GripVertical, new Vector2(gripX, min.Y), new Vector2(gripX + gripWidth, max.Y),
				M3.Alpha(s.OnSurfaceVariant, (hovered || lifted ? 1f : 0.6f) * dim));

			var textX = gripX + gripWidth + (12f * scale);
			var text = M3Navigation.Truncate(label(item), MathF.Max(8f * scale, max.X - textX - (8f * scale)));
			var textSize = ImGui.CalcTextSize(text);
			var textColor = i == selected && !lifted ? s.OnSecondaryContainer : s.OnSurface;
			drawList.AddText(new Vector2(textX, min.Y + ((height - textSize.Y) * 0.5f)), M3.U32(textColor, 0.95f * dim), text);
			M3Draw.FocusRing(min, max, M3.ShapeSmall);

			if (mouseOver && !dragging && tooltip != null)
			{
				M3Tooltip.Show(tooltip(item));
			}
		}

		if (_reorderList == listKey && _reorderIndex >= 0)
		{
			if (!ImGui.IsMouseDown(ImGuiMouseButton.Left))
			{
				_reorderList = 0;
				_reorderIndex = -1;
			}
			else if (dragging && enabled && items.Count > 0)
			{
				ImGui.SetMouseCursor(ImGuiMouseCursor.ResizeNs);
				var target = Math.Clamp((int)MathF.Floor((ImGui.GetMousePos().Y - top) / (height + gap)), 0, items.Count - 1);
				if (target != _reorderIndex && _reorderIndex < items.Count)
				{
					Move(items, _reorderIndex, target, ref selected);
					_reorderIndex = target;
					moved = true;
				}
			}
		}

		return moved;
	}

	private static void Move<T>(IList<T> items, int from, int to, ref int selected)
	{
		var item = items[from];
		items.RemoveAt(from);
		items.Insert(to, item);

		if (selected == from)
		{
			selected = to;
		}
		else if (from < selected && selected <= to)
		{
			selected--;
		}
		else if (to <= selected && selected < from)
		{
			selected++;
		}
	}

	private static void ResetReorder()
	{
		_reorderList = 0;
		_reorderIndex = -1;
	}
}
