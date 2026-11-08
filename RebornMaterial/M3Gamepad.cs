using Dalamud.Game.ClientState.GamePad;

namespace RebornMaterial;

public static class M3Gamepad
{
	private const long FocusTimeoutMilliseconds = 250;

	private static readonly GamepadButtons[] _buttons = Array.FindAll(Enum.GetValues<GamepadButtons>(), button => button != GamepadButtons.None);

	private static GamepadButtons _down;
	private static GamepadButtons _previous;
	private static bool _navActive;

	private static bool _focusReported;
	private static bool _focusBlocks;
	private static volatile bool _blockGame;
	private static long _blockEvaluatedAt;

	public static GamepadButtons ContextMenuButton { get; set; } = GamepadButtons.West;

	public static GamepadButtons PreviousPageButton { get; set; } = GamepadButtons.L1;

	public static GamepadButtons NextPageButton { get; set; } = GamepadButtons.R1;

	public static bool NavActive => _navActive;

	public static bool IsHeld(GamepadButtons button)
	{
		return (_down & button) != 0;
	}

	public static bool JustPressed(GamepadButtons button)
	{
		return (_down & button) != 0 && (_previous & button) == 0;
	}

	public static bool JustReleased(GamepadButtons button)
	{
		return (_down & button) == 0 && (_previous & button) != 0;
	}

	public static bool ContextMenuRequested()
	{
		return _navActive && JustPressed(ContextMenuButton) && (ImGui.IsItemFocused() || ImGui.IsItemHovered());
	}

	public static int PageStep()
	{
		if (!_navActive || !ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
		{
			return 0;
		}

		if (JustPressed(PreviousPageButton))
		{
			return -1;
		}

		return JustPressed(NextPageButton) ? 1 : 0;
	}

	public static void ReportFocus(bool blocksGameInput = true)
	{
		if (!ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
		{
			return;
		}

		_focusReported = true;
		_focusBlocks |= blocksGameInput;
	}

	public static bool InputBlockingEnabled => M3GamepadBlocker.IsEnabled;

	public static void EnableInputBlocking(bool movement = true, bool camera = true, bool buttons = true)
	{
		M3GamepadBlocker.Enable(movement, camera, buttons);
	}

	public static void DisableInputBlocking()
	{
		M3GamepadBlocker.Disable();
	}

	internal static bool ShouldBlockGame => _blockGame && Environment.TickCount64 - Volatile.Read(ref _blockEvaluatedAt) < FocusTimeoutMilliseconds;

	internal static void BeginFrame()
	{
		var io = ImGui.GetIO();
		_navActive = io.NavActive && (io.ConfigFlags & ImGuiConfigFlags.NavEnableGamepad) != 0;

		_blockGame = _navActive && _focusReported && _focusBlocks;
		Volatile.Write(ref _blockEvaluatedAt, Environment.TickCount64);
		_focusReported = false;
		_focusBlocks = false;

		_previous = _down;
		_down = GamepadButtons.None;
		var state = M3Services.GamepadState;
		if (state == null)
		{
			return;
		}

		foreach (var button in _buttons)
		{
			if (state.Raw(button) > 0f)
			{
				_down |= button;
			}
		}
	}

	internal static void Reset()
	{
		M3GamepadBlocker.Dispose();
		_down = GamepadButtons.None;
		_previous = GamepadButtons.None;
		_navActive = false;
		_focusReported = false;
		_focusBlocks = false;
		_blockGame = false;
	}
}
