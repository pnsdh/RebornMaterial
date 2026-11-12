using Dalamud.Interface.Textures.TextureWraps;

namespace RebornMaterial;

public static class M3Menu
{
	private const int StyleVars = 3;

	private static readonly Dictionary<uint, Vector2> _subSizes = [];
	private static int _depth;
	private static bool _closeAll;

	public static void Open(string id)
	{
		ImGui.OpenPopup(id);
	}

	public static Scope Begin(string id)
	{
		PushStyle();
		return Finish(ImGui.BeginPopup(id));
	}

	internal static Scope Begin(ImU8String id)
	{
		PushStyle();
		return Finish(ImGui.BeginPopup(id));
	}

	public static Scope BeginContext(string id)
	{
		if (M3Gamepad.ContextMenuRequested())
		{
			ImGui.OpenPopup(id);
		}

		PushStyle();
		return Finish(ImGui.BeginPopupContextItem(id));
	}

	public static Scope BeginSub(string id, string label, FontAwesomeIcon icon = FontAwesomeIcon.None, bool enabled = true, IDalamudTextureWrap? texture = null)
	{
		var scale = M3.Scale;
		var key = ImGui.GetID($"{id}_sub");
		var open = ImGui.IsPopupOpen($"{id}_sub");
		var parentMin = ImGui.GetWindowPos();
		var parentMax = parentMin + ImGui.GetWindowSize();

		var pressed = M3Widgets.MenuItemCore(id, label, false, icon, enabled, null, null, texture, false, true, open);
		var rowHovered = ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenBlockedByActiveItem);
		var rowMin = ImGui.GetItemRectMin();
		var rowMax = ImGui.GetItemRectMax();

		if (enabled && !open && (pressed || rowHovered))
		{
			ImGui.OpenPopup($"{id}_sub");
		}

		var size = _subSizes.TryGetValue(key, out var known) ? known : new Vector2(200f * scale, 0f);
		var viewport = ImGui.GetWindowViewport();
		var viewMin = viewport.WorkPos;
		var viewMax = viewMin + viewport.WorkSize;
		var x = parentMax.X + (2f * scale);
		if (x + size.X > viewMax.X)
		{
			x = parentMin.X - size.X - (2f * scale);
		}

		var y = Math.Clamp(rowMin.Y - (6f * scale), viewMin.Y, MathF.Max(viewMin.Y, viewMax.Y - size.Y));

		ImGui.SetNextWindowPos(new Vector2(x, y));
		PushStyle();
		var scope = Finish(ImGui.BeginPopup($"{id}_sub"));
		if (!scope.IsOpen)
		{
			return scope;
		}

		if (!ImGui.IsWindowAppearing())
		{
			_subSizes[key] = ImGui.GetWindowSize();
		}

		var mouse = ImGui.GetMousePos();
		var overParent = mouse.X >= parentMin.X && mouse.X <= parentMax.X && mouse.Y >= parentMin.Y && mouse.Y <= parentMax.Y;
		var overRow = mouse.Y >= rowMin.Y && mouse.Y <= rowMax.Y;
		if (overParent && !overRow && !ImGui.IsWindowHovered(ImGuiHoveredFlags.ChildWindows))
		{
			ImGui.CloseCurrentPopup();
		}

		return scope;
	}

	public static bool Item(string id, string label, FontAwesomeIcon icon = FontAwesomeIcon.None, bool enabled = true, bool selected = false, string? shortcut = null, string? tooltip = null, IDalamudTextureWrap? texture = null)
	{
		if (!M3Widgets.MenuItem(id, label, selected, icon, enabled, shortcut, tooltip, texture))
		{
			return false;
		}

		Close();
		return true;
	}

	public static void Close()
	{
		if (_depth > 0)
		{
			_closeAll = true;
		}

		ImGui.CloseCurrentPopup();
	}

	public static void Heading(string text)
	{
		var scale = M3.Scale;
		ImGui.Dummy(new Vector2(0f, 4f * scale));
		ImGui.SetCursorPosX(ImGui.GetCursorPosX() + (12f * scale));
		M3Text.Draw(text, M3TextStyle.Label);
	}

	public static void Divider()
	{
		M3Widgets.Divider(4f);
	}

	private static void PushStyle()
	{
		var scale = M3.Scale;
		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, new Vector2(4f, 6f) * scale);
		ImGui.PushStyleVar(ImGuiStyleVar.PopupRounding, M3.ShapeSmall);
		ImGui.PushStyleVar(ImGuiStyleVar.ItemSpacing, ImGui.GetStyle().ItemSpacing with { Y = 0f });
		ImGui.PushStyleColor(ImGuiCol.PopupBg, M3.Scheme.SurfaceContainer);
	}

	private static void PopStyle()
	{
		ImGui.PopStyleColor();
		ImGui.PopStyleVar(StyleVars);
	}

	private static Scope Finish(bool open)
	{
		if (!open)
		{
			PopStyle();
			return default;
		}

		_depth++;
		return new Scope(true);
	}

	internal static void Reset()
	{
		_subSizes.Clear();
		_depth = 0;
		_closeAll = false;
	}

	public readonly struct Scope(bool open) : IDisposable
	{
		public bool IsOpen => open;

		public void Dispose()
		{
			if (!open)
			{
				return;
			}

			if (_closeAll)
			{
				ImGui.CloseCurrentPopup();
			}

			ImGui.EndPopup();
			PopStyle();

			_depth--;
			if (_depth <= 0)
			{
				_depth = 0;
				_closeAll = false;
			}
		}
	}
}
