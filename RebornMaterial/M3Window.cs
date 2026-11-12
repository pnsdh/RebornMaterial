using Dalamud.Interface.Windowing;

namespace RebornMaterial;

public abstract class M3Window(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false) : Window(name, flags, forceMainWindow)
{
	private M3Style.Scope _theme;
	private bool _closeHotkeyHeld;
	private bool _closeHotkeyWanted;

	public static M3Density DefaultDensity { get; set; } = M3Density.Compact;

	protected virtual M3Density Density => DefaultDensity;

	public bool BlocksGameInput { get; set; } = true;

	public sealed override void PreDraw()
	{
		HoldCloseHotkey(M3.EscapeCaptured);
		_theme = M3.IsInitialized ? M3Style.Push(Density) : default;
		PreDrawThemed();
	}

	private void HoldCloseHotkey(bool hold)
	{
		if (hold == _closeHotkeyHeld)
		{
			return;
		}

		if (hold)
		{
			_closeHotkeyWanted = RespectCloseHotkey;
			RespectCloseHotkey = false;
		}
		else
		{
			RespectCloseHotkey = _closeHotkeyWanted;
		}

		_closeHotkeyHeld = hold;
	}

	public sealed override void PostDraw()
	{
		try
		{
			PostDrawThemed();
		}
		finally
		{
			_theme.Dispose();
			_theme = default;
		}
	}

	protected virtual void PreDrawThemed()
	{
	}

	protected virtual void PostDrawThemed()
	{
	}

	protected void ReportFocus()
	{
		M3Gamepad.ReportFocus(BlocksGameInput);
	}
}
