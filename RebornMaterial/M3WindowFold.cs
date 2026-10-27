using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace RebornMaterial;

public sealed class M3WindowFold
{
	private static readonly Vector2 MaximumSize = new(10000f, 10000f);

	private bool _minimized;
	private float _time;

	private bool _layout;
	private bool _settled;
	private bool _checkedLeftover;

	private Vector2 _restoreSize;
	private Vector2 _anchorOpen;
	private Vector2 _anchorFolded;

	private Vector2 _windowPos;
	private Vector2 _windowSize;
	private Vector2 _openPadding;
	private float _rounding;
	private bool _stylePushed;
	private bool _pendingMinimize;

	public bool IsMinimized => _minimized;

	public bool IsActive => _layout;

	public float Amount
	{
		get
		{
			var t = _time;
			return t < 0.5f ? 4f * t * t * t : 1f - (MathF.Pow((-2f * t) + 2f, 3f) * 0.5f);
		}
	}

	public float BarTop { get; set; }

	public Vector2 OpenPadding => _openPadding;

	public float Rounding => _rounding;

	// Style alpha for the open window's contents. They fade out faster than the window folds, so they're gone before the pill closes over them.
	public float ContentAlpha => ImGui.GetStyle().Alpha * (1f - MathF.Min(1f, Amount * 1.4f));

	// ImGui truncates window positions and sizes, so anchors stay on whole pixels or each fold drifts the window.
	private Vector2 AnchorInset => Vector2.Round(new Vector2(_openPadding.X, _openPadding.Y + BarTop));

	public void Restore()
	{
		_pendingMinimize = false;
		if (!_minimized)
		{
			return;
		}

		if (_time >= 1f)
		{
			_anchorOpen = OpenAnchorNear(_anchorFolded);
		}

		_minimized = false;
	}

	// Folds the window from code. Before the window has drawn once, it folds on its first frame instead.
	public void Minimize()
	{
		if (_minimized)
		{
			return;
		}

		if (_windowSize == Vector2.Zero)
		{
			_pendingMinimize = true;
			return;
		}

		var inset = AnchorInset;
		Toggle(new Vector2(_windowPos.X + _windowSize.X - inset.X, _windowPos.Y + inset.Y));
	}

	public void Reset()
	{
		if (!_layout)
		{
			return;
		}

		Restore();
		_time = 0f;
	}

	public bool Prepare(Window window, int actionCount, in M3WindowBrand brand)
	{
		var style = ImGui.GetStyle();
		_openPadding = style.WindowPadding;
		_rounding = style.WindowRounding;

		if (!_checkedLeftover)
		{
			_checkedLeftover = true;
			ReopenLeftover(window);
		}

		var resting = _minimized && _time >= 1f;

		var target = _minimized ? 1f : 0f;
		if (_time != target)
		{
			var step = ImGui.GetIO().DeltaTime / M3Motion.EmphasisedDuration;
			_time = _minimized ? MathF.Min(1f, _time + step) : MathF.Max(0f, _time - step);
		}

		if (!_layout)
		{
			return false;
		}

		if (_settled && !_minimized && _time <= 0f)
		{
			_layout = false;
			_settled = false;
			return true;
		}

		var folded = Amount;
		var barSize = M3Widgets.WindowActionsSize(actionCount, brand, folded);
		var anchor = Vector2.Lerp(_anchorOpen, _anchorFolded, folded);
		var inset = AnchorInset * (1f - folded);
		var size = Vector2.Round(Vector2.Lerp(_restoreSize, barSize, folded));

		window.Position = resting ? null : Vector2.Round(new Vector2(anchor.X + inset.X - size.X, anchor.Y - inset.Y));
		window.PositionCondition = ImGuiCond.Always;
		// The half pixel stops Dalamud's GlobalScale round trip from truncating a pixel away.
		window.Size = (size + new Vector2(0.5f)) / ImGuiHelpers.GlobalScale;
		window.SizeCondition = ImGuiCond.Always;
		window.SizeConstraints = new WindowSizeConstraints()
		{
			MinimumSize = Vector2.One,
			MaximumSize = MaximumSize,
		};

		// Don't save settings while folded, so the window reopens at its real size.
		window.Flags |= ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoSavedSettings
			| (resting ? ImGuiWindowFlags.None : ImGuiWindowFlags.NoMove);
		_settled = !_minimized && _time <= 0f;

		_rounding = float.Lerp(_rounding, barSize.Y * 0.5f, folded);
		ImGui.PushStyleVar(ImGuiStyleVar.WindowRounding, _rounding);
		ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Lerp(_openPadding, Vector2.Zero, folded));
		_stylePushed = true;
		return false;
	}

	public void PopStyle()
	{
		if (_stylePushed)
		{
			ImGui.PopStyleVar(2);
			_stylePushed = false;
		}
	}

	public void BeginDraw()
	{
		PopStyle();
		_windowPos = ImGui.GetWindowPos();
		_windowSize = ImGui.GetWindowSize();

		if (_pendingMinimize)
		{
			_pendingMinimize = false;
			Minimize();
		}
	}

	public (Vector2 Pos, Vector2 Size) OpenRect()
	{
		if (!_layout)
		{
			return (_windowPos, _windowSize);
		}

		var inset = AnchorInset;
		return (new Vector2(_anchorOpen.X + inset.X - _restoreSize.X, _anchorOpen.Y - inset.Y), _restoreSize);
	}

	// A child over the open window's contents, inside its padding, so they keep their layout while the window folds.
	// Call it while Amount is below 1, inside a push of ContentAlpha. Draw anything window-wide, like a backdrop, before it.
	public ImRaii.ChildDisposable BeginContent(string id)
	{
		var (pos, size) = OpenRect();
		ImGui.SetCursorScreenPos(pos + _openPadding);
		return ImRaii.Child(id, Vector2.Max(Vector2.One, size - (_openPadding * 2f)), false,
			ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground);
	}

	public int DrawBar(string id, ReadOnlySpan<M3WindowAction> actions, in M3WindowBrand brand, out bool closed, Vector4? fill = null, string closeTooltip = "Close")
	{
		var folded = Amount;

		Vector2 anchor;
		if (!_layout)
		{
			var inset = AnchorInset;
			anchor = new Vector2(_windowPos.X + _windowSize.X - inset.X, _windowPos.Y + inset.Y);
		}
		else
		{
			if (_minimized && _time >= 1f)
			{
				_anchorFolded = new Vector2(_windowPos.X + _windowSize.X, _windowPos.Y);
			}

			anchor = Vector2.Lerp(_anchorOpen, _anchorFolded, folded);
		}

		var barSize = M3Widgets.WindowActionsSize(actions.Length, brand, folded);
		ImGui.SetCursorScreenPos(new Vector2(anchor.X - barSize.X, anchor.Y));
		using var bar = ImRaii.Child(id, barSize, false,
			ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse | ImGuiWindowFlags.NoBackground);
		if (!bar)
		{
			closed = false;
			return -1;
		}

		var pressed = M3Widgets.WindowActions(id, anchor, actions, brand, folded, out var toggled, out closed, fill, closeTooltip);
		if (toggled)
		{
			Toggle(anchor);
		}

		return pressed;
	}

	private void Toggle(Vector2 anchor)
	{
		if (_minimized)
		{
			Restore();
			return;
		}

		if (!_layout)
		{
			_restoreSize = _windowSize;
			_anchorOpen = anchor;
			_anchorFolded = anchor;
			_layout = true;
		}

		_minimized = true;
	}

	// ImGui keeps a window's size across a plugin reload, so one left folded by the last instance would come back pill-sized.
	// Folding stops ImGui saving the window, so its saved size is still the open one.
	private void ReopenLeftover(Window window)
	{
		var existing = ImGuiP.FindWindowByName(window.WindowName);
		if (existing.IsNull
			|| !existing.Flags.HasFlag(ImGuiWindowFlags.NoSavedSettings)
			|| window.Flags.HasFlag(ImGuiWindowFlags.NoSavedSettings))
		{
			return;
		}

		var saved = ImGuiP.FindWindowSettings(existing.ID);
		var size = saved.IsNull ? Vector2.Zero : new Vector2(saved.Size.X, saved.Size.Y);
		if (size.X <= 0f || size.Y <= 0f)
		{
			if (window.Size is not { } fallback)
			{
				return;
			}

			size = Vector2.Round(fallback * ImGuiHelpers.GlobalScale);
		}

		var corner = new Vector2(existing.Pos.X + existing.SizeFull.X, existing.Pos.Y);
		ImGuiP.SetWindowPos(existing, OpenPositionNear(corner, size), ImGuiCond.Always);
		ImGuiP.SetWindowSize(existing, size, ImGuiCond.Always);
	}

	private Vector2 OpenAnchorNear(Vector2 folded)
	{
		var pos = OpenPositionNear(folded, _restoreSize);
		var inset = AnchorInset;
		return new Vector2(pos.X + _restoreSize.X - inset.X, pos.Y + inset.Y);
	}

	// Where a window of this size opens out of a pill whose top-right corner is at folded. It stays on screen if the pill is.
	private Vector2 OpenPositionNear(Vector2 folded, Vector2 size)
	{
		var inset = AnchorInset;
		var pos = new Vector2(folded.X + inset.X - size.X, folded.Y - inset.Y);

		var viewport = ImGui.GetMainViewport();
		var min = viewport.WorkPos;
		var max = viewport.WorkPos + viewport.WorkSize;
		if (folded.X >= min.X && folded.X <= max.X && folded.Y >= min.Y && folded.Y <= max.Y)
		{
			pos.X = Math.Clamp(pos.X, min.X, MathF.Max(min.X, max.X - size.X));
			pos.Y = Math.Clamp(pos.Y, min.Y, MathF.Max(min.Y, max.Y - size.Y));
		}

		return pos;
	}
}
