using Dalamud.Interface.Windowing;

namespace RebornMaterial;

public abstract class M3Window(string name, ImGuiWindowFlags flags = ImGuiWindowFlags.None, bool forceMainWindow = false) : Window(name, flags, forceMainWindow)
{
	private M3Style.Scope _theme;

	public static M3Density DefaultDensity { get; set; } = M3Density.Compact;

	protected virtual M3Density Density => DefaultDensity;

	public bool BlocksGameInput { get; set; } = true;

	public sealed override void PreDraw()
	{
		_theme = M3.IsInitialized ? M3Style.Push(Density) : default;
		PreDrawThemed();
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
