using Dalamud.Interface.Textures.TextureWraps;

namespace RebornMaterial;

public static class M3Tree
{
	private static readonly Dictionary<uint, bool> _open = [];

	public static float RowHeight => M3.FitText(32f, 6f);

	private static float ChevronSlot => 20f * M3.Scale;

	public static Scope Node(string id, string label, bool selected = false, bool leaf = false, FontAwesomeIcon icon = FontAwesomeIcon.None,
		IDalamudTextureWrap? texture = null, Vector4? color = null, bool defaultOpen = false, string? tooltip = null, string? trailing = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var height = RowHeight;
		var width = MathF.Max(32f * scale, ImGui.GetContentRegionAvail().X);
		var key = ImGui.GetID(id);
		if (!_open.TryGetValue(key, out var open))
		{
			open = defaultOpen;
			_open[key] = open;
		}

		var clicked = ImGui.InvisibleButton(id, new Vector2(width, height));
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = ImGui.IsItemActive();
		var doubleClicked = hovered && ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left);
		var rightClicked = ImGui.IsItemClicked(ImGuiMouseButton.Right);
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();

		if (!leaf)
		{
			var io = ImGui.GetIO();
			var toggle = clicked && !(io.MouseReleased[0] && io.MouseClickedLastCount[0] >= 2);
			if (ImGui.IsItemFocused())
			{
				toggle |= (!open && ImGui.IsKeyPressed(ImGuiKey.RightArrow)) || (open && ImGui.IsKeyPressed(ImGuiKey.LeftArrow));
			}

			if (toggle)
			{
				open = !open;
				_open[key] = open;
			}
		}

		var drawList = ImGui.GetWindowDrawList();
		if (selected)
		{
			drawList.AddRectFilled(min, max, M3.U32(s.SecondaryContainer, 0.85f), M3.ShapeSmall);
		}

		if (hovered || held)
		{
			drawList.AddRectFilled(min, max, M3.U32(s.OnSurface, held ? M3.StatePressed : M3.StateHover), M3.ShapeSmall);
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
		}

		var content = selected ? s.OnSecondaryContainer : s.OnSurfaceVariant;
		var chevronCenter = new Vector2(min.X + (ChevronSlot * 0.5f) + (2f * scale), min.Y + (height * 0.5f));
		if (!leaf)
		{
			var turn = M3Motion.Approach(ImGui.GetID($"{id}_chevron"), open ? 1f : 0f, M3Motion.FastDuration);
			DrawChevron(drawList, chevronCenter, turn, M3.Alpha(content, hovered ? 1f : 0.8f));
		}

		var x = min.X + ChevronSlot + (6f * scale);
		var iconSlot = MathF.Max(18f * scale, ImGui.GetTextLineHeight());
		if (texture != null && M3Draw.Texture(drawList, texture, new Vector2(x, min.Y + ((height - iconSlot) * 0.5f)), iconSlot))
		{
			x += iconSlot + (8f * scale);
		}
		else if (icon != FontAwesomeIcon.None)
		{
			var iconSize = M3Draw.MeasureIcon(icon);
			M3Draw.Icon(drawList, icon, new Vector2(x, min.Y + ((height - iconSize.Y) * 0.5f)), color ?? (selected ? s.OnSecondaryContainer : s.Primary));
			x += iconSize.X + (8f * scale);
		}

		var trailingSize = string.IsNullOrEmpty(trailing) ? Vector2.Zero : ImGui.CalcTextSize(trailing);
		var room = max.X - x - (8f * scale) - (trailingSize.X > 0f ? trailingSize.X + (12f * scale) : 0f);
		var text = M3Navigation.Truncate(label, MathF.Max(8f * scale, room));
		var textSize = ImGui.CalcTextSize(text);
		drawList.AddText(new Vector2(x, min.Y + ((height - textSize.Y) * 0.5f)),
			M3.U32(color ?? (selected ? s.OnSecondaryContainer : s.OnSurface), 0.95f), text);

		if (trailingSize.X > 0f)
		{
			drawList.AddText(new Vector2(max.X - (8f * scale) - trailingSize.X, min.Y + ((height - trailingSize.Y) * 0.5f)),
				M3.U32(s.OnSurfaceVariant, 0.85f), trailing);
		}

		M3Draw.FocusRing(min, max, M3.ShapeSmall);

		if (hovered)
		{
			M3Tooltip.Show(tooltip ?? (text != label ? label : null));
		}

		ImGui.PushID(id);
		var branchOpen = open && !leaf;
		var indent = ChevronSlot;
		if (branchOpen)
		{
			ImGui.Indent(indent);
		}

		return new Scope(true, branchOpen, clicked, doubleClicked, rightClicked, indent, chevronCenter.X, max.Y);
	}

	public static void SetOpen(string id, bool open)
	{
		_open[ImGui.GetID(id)] = open;
	}

	public static bool IsOpen(string id)
	{
		return _open.TryGetValue(ImGui.GetID(id), out var open) && open;
	}

	internal static void Reset()
	{
		_open.Clear();
	}

	private static void DrawChevron(ImDrawListPtr drawList, Vector2 center, float turn, Vector4 color)
	{
		var scale = M3.Scale;
		var arm = 3.5f * scale;
		var (sin, cos) = MathF.SinCos(turn * MathF.PI * 0.5f);

		Vector2 Rotate(float px, float py)
		{
			return center + new Vector2((px * cos) - (py * sin), (px * sin) + (py * cos));
		}

		var tip = Rotate(arm * 0.6f, 0f);
		drawList.AddLine(Rotate(-arm * 0.6f, -arm), tip, M3.U32(color), 1.75f * scale);
		drawList.AddLine(tip, Rotate(-arm * 0.6f, arm), M3.U32(color), 1.75f * scale);
	}

	public readonly struct Scope(bool active, bool open, bool clicked, bool doubleClicked, bool rightClicked, float indent, float guideX, float guideTop) : IDisposable
	{
		public bool Open => open;

		public bool Clicked => clicked;

		public bool DoubleClicked => doubleClicked;

		public bool RightClicked => rightClicked;

		public void Dispose()
		{
			if (!active)
			{
				return;
			}

			if (open)
			{
				ImGui.Unindent(indent);

				var bottom = ImGui.GetCursorScreenPos().Y - (4f * M3.Scale);
				if (bottom > guideTop)
				{
					ImGui.GetWindowDrawList().AddLine(new Vector2(guideX, guideTop), new Vector2(guideX, bottom),
						M3.U32(M3.Scheme.OutlineVariant, 0.6f), 1f * M3.Scale);
				}
			}

			ImGui.PopID();
		}
	}
}
