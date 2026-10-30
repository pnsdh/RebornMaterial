using Dalamud.Interface.Textures.TextureWraps;
using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public readonly record struct M3WindowAction(string Id, FontAwesomeIcon Icon, string Tooltip);

public readonly record struct M3WindowBrand(IDalamudTextureWrap? Logo, string Label, FontAwesomeIcon FallbackIcon = FontAwesomeIcon.Fire);

public static partial class M3Widgets
{
	private static float WindowActionsPadding => 4f * M3.Scale;
	private static float WindowActionsGap => 2f * M3.Scale;
	private static float WindowActionsDividerGap => 8f * M3.Scale;
	private static float WindowBrandLogoSize => 24f * M3.Scale;

	public static Vector2 WindowActionsSize(int actionCount)
	{
		return WindowActionsSize(ActionsWidth(actionCount), 1);
	}

	public static Vector2 WindowActionsSize(int actionCount, in M3WindowBrand brand, float minimized)
	{
		return WindowActionsSize(LeadingWidth(actionCount, brand, minimized), 2);
	}

	public static int WindowActionsThatFit(int actionCount, float available, int minimum = 0)
	{
		var shown = actionCount;
		while (shown > minimum && WindowActionsSize(shown).X > available)
		{
			shown--;
		}

		return shown;
	}

	public static int WindowActionsThatFit(int actionCount, in M3WindowBrand brand, float available, int minimum = 0)
	{
		var shown = actionCount;
		while (shown > minimum && WindowActionsSize(shown, brand, 0f).X > available)
		{
			shown--;
		}

		return shown;
	}

	public static int WindowActions(string id, Vector2 topRight, ReadOnlySpan<M3WindowAction> actions, out bool closed, Vector4? fill = null, string closeTooltip = "Close")
	{
		return DrawWindowActions(id, topRight, actions, null, 0f, out _, out closed, fill, closeTooltip);
	}

	public static int WindowActions(string id, Vector2 topRight, ReadOnlySpan<M3WindowAction> actions, in M3WindowBrand brand, float minimized, out bool toggled, out bool closed, Vector4? fill = null, string closeTooltip = "Close")
	{
		return DrawWindowActions(id, topRight, actions, brand, minimized, out toggled, out closed, fill, closeTooltip);
	}

	private static int DrawWindowActions(string id, Vector2 topRight, ReadOnlySpan<M3WindowAction> actions, M3WindowBrand? brand, float minimized, out bool toggled, out bool closed, Vector4? fill, string closeTooltip)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var padding = WindowActionsPadding;
		var gap = WindowActionsGap;
		var buttonSize = IconButtonSize;

		minimized = brand is null ? 0f : Math.Clamp(minimized, 0f, 1f);
		var leading = brand is { } measured ? LeadingWidth(actions.Length, measured, minimized) : ActionsWidth(actions.Length);
		var size = WindowActionsSize(leading, brand is null ? 1 : 2);

		var min = new Vector2(topRight.X - size.X, topRight.Y);
		var drawList = ImGui.GetWindowDrawList();
		drawList.AddRectFilled(min, min + size, M3.U32(fill ?? M3.Alpha(s.Surface, 0.72f)), M3.ShapeFull);

		using var idScope = ImRaii.PushId(id);
		var result = -1;
		var x = min.X + padding;
		var y = min.Y + padding;

		ImGui.PushClipRect(new Vector2(x, y), new Vector2(x + leading, y + buttonSize), true);
		var actionsAlpha = Math.Clamp(1f - (minimized * 2f), 0f, 1f);
		if (actionsAlpha > 0f)
		{
			using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * actionsAlpha);
			var actionX = x;
			for (var i = 0; i < actions.Length; i++)
			{
				var action = actions[i];
				ImGui.SetCursorScreenPos(new Vector2(actionX, y));
				if (IconButton(action.Id, action.Icon, action.Tooltip))
				{
					result = i;
				}

				actionX += buttonSize + gap;
			}
		}

		if (brand is { } shown && minimized > 0.5f)
		{
			using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, ImGui.GetStyle().Alpha * ((minimized * 2f) - 1f));
			DrawWindowBrand(drawList, shown, new Vector2(x, y), buttonSize);
		}

		ImGui.PopClipRect();
		x += leading;

		var dividerRoom = DividerRoom(leading);
		if (dividerRoom > 0f)
		{
			x += dividerRoom;
			drawList.AddLine(new Vector2(x, y + (8f * scale)), new Vector2(x, y + buttonSize - (8f * scale)),
				M3.U32(s.OutlineVariant, 0.9f * dividerRoom / WindowActionsDividerGap), 1f * scale);
			x += dividerRoom;
		}

		toggled = false;
		if (brand is not null)
		{
			ImGui.SetCursorScreenPos(new Vector2(x, y));
			toggled = CaretButton("##minimize", -MathF.PI * 0.5f * minimized, minimized < 0.5f ? M3Fork.T("Minimize") : M3Fork.T("Restore"));
			x += buttonSize + gap;
		}

		ImGui.SetCursorScreenPos(new Vector2(x, y));
		closed = IconButton("##close", FontAwesomeIcon.Times, closeTooltip);
		return result;
	}

	private static Vector2 WindowActionsSize(float leading, int controls)
	{
		var padding = WindowActionsPadding;
		var width = (padding * 2f) + leading + (DividerRoom(leading) * 2f)
			+ (IconButtonSize * controls) + (WindowActionsGap * (controls - 1));
		return new Vector2(width, IconButtonSize + (padding * 2f));
	}

	private static float DividerRoom(float leading)
	{
		return WindowActionsDividerGap * Math.Clamp(leading / WindowActionsDividerGap, 0f, 1f);
	}

	private static float ActionsWidth(int count)
	{
		return count <= 0 ? 0f : (IconButtonSize * count) + (WindowActionsGap * (count - 1));
	}

	private static float LeadingWidth(int actionCount, in M3WindowBrand brand, float minimized)
	{
		return minimized <= 0f ? ActionsWidth(actionCount)
			: minimized >= 1f ? BrandWidth(brand)
			: float.Lerp(ActionsWidth(actionCount), BrandWidth(brand), minimized);
	}

	private static float BrandWidth(in M3WindowBrand brand)
	{
		var scale = M3.Scale;
		using var font = ImRaii.PushFont(M3.TitleMedium);
		return (6f * scale) + WindowBrandLogoSize + (8f * scale) + ImGui.CalcTextSize(brand.Label).X + (4f * scale);
	}

	private static void DrawWindowBrand(ImDrawListPtr drawList, in M3WindowBrand brand, Vector2 origin, float height)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var logoSize = WindowBrandLogoSize;
		var logoMin = new Vector2(origin.X + (6f * scale), origin.Y + ((height - logoSize) * 0.5f));
		var logoMax = logoMin + new Vector2(logoSize, logoSize);

		if (!M3ActionIcon.Image(drawList, brand.Logo, logoMin, logoMax, M3.ShapeExtraSmall))
		{
			drawList.AddRectFilled(logoMin, logoMax, M3.U32(s.PrimaryContainer), M3.ShapeExtraSmall);
			M3Draw.IconCentered(drawList, brand.FallbackIcon, logoMin, logoMax, s.OnPrimaryContainer);
		}

		using var font = ImRaii.PushFont(M3.TitleMedium);
		var labelSize = ImGui.CalcTextSize(brand.Label);
		drawList.AddText(new Vector2(logoMax.X + (8f * scale), origin.Y + ((height - labelSize.Y) * 0.5f)), M3.U32(s.OnSurface), brand.Label);
	}

	private static bool CaretButton(string id, float angle, string? tooltip)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var size = Vector2.One * IconButtonSize;

		var pressed = ImGui.InvisibleButton(id, size);
		var hovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var held = ImGui.IsItemActive();
		var center = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * 0.5f;
		var drawList = ImGui.GetWindowDrawList();
		var content = s.OnSurfaceVariant;

		if (hovered || held)
		{
			drawList.AddCircleFilled(center, size.X * 0.5f, M3.U32(content, held ? M3.StatePressed : M3.StateHover), 32);
		}

		var halfWidth = 5f * scale;
		var halfHeight = 3.5f * scale;
		var (sin, cos) = MathF.SinCos(angle);
		Vector2 Turn(float px, float py) => center + new Vector2((px * cos) - (py * sin), (px * sin) + (py * cos));
		drawList.AddTriangleFilled(Turn(0f, halfHeight), Turn(-halfWidth, -halfHeight), Turn(halfWidth, -halfHeight), M3.U32(content));
		M3Draw.FocusRing(ImGui.GetItemRectMin(), ImGui.GetItemRectMax(), size.X * 0.5f);

		if (hovered)
		{
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			M3Tooltip.Show(tooltip);
		}

		return pressed;
	}
}
