using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace RebornMaterial;

public abstract class M3FoldWindow : M3Window
{
	private const ImGuiWindowFlags FoldFlags = ImGuiWindowFlags.NoTitleBar
		| ImGuiWindowFlags.NoCollapse
		| ImGuiWindowFlags.NoScrollbar
		| ImGuiWindowFlags.NoScrollWithMouse;

	private static M3FoldWindow? _snackbarHost;

	private readonly M3WindowFold _fold = new();
	private readonly string _brandLabel;
	private readonly Vector2 _defaultSize;
	private readonly WindowSizeConstraints? _defaultConstraints;
	private int _shownActions = int.MaxValue;

	protected M3FoldWindow(string name, string brandLabel, Vector2 initialSize, WindowSizeConstraints? constraints = null)
		: base(name, FoldFlags)
	{
		_brandLabel = brandLabel;
		_defaultSize = initialSize;
		_defaultConstraints = constraints;
		Size = initialSize;
		SizeCondition = ImGuiCond.FirstUseEver;
		SizeConstraints = constraints;
	}

	protected string BrandLabel => _brandLabel;

	protected M3WindowBrand Brand => M3Brand.For(_brandLabel);

	protected virtual M3WindowAction[] WindowActions => [];

	protected virtual int MinimumActions => 0;

	protected virtual ImGuiWindowFlags ExtraFlags => ImGuiWindowFlags.None;

	public bool IsMinimized => _fold.IsMinimized;

	protected int ShownActions => Math.Min(_shownActions, WindowActions.Length);

	public void Restore()
	{
		_fold.Restore();
	}

	public void Minimize()
	{
		_fold.Minimize();
	}

	public void OpenAndRestore()
	{
		_fold.Restore();
		if (IsOpen)
		{
			BringToFront();
		}
		else
		{
			IsOpen = true;
		}
	}

	protected sealed override void PreDrawThemed()
	{
		Flags = FoldFlags | ExtraFlags;
		PreDrawFold();
		if (_fold.Prepare(this, ShownActions, Brand))
		{
			Position = null;
			Size = _defaultSize;
			SizeCondition = ImGuiCond.FirstUseEver;
			SizeConstraints = _defaultConstraints;
		}
	}

	protected sealed override void PostDrawThemed()
	{
		_fold.PopStyle();
		PostDrawFold();
	}

	protected virtual void PreDrawFold()
	{
	}

	protected virtual void PostDrawFold()
	{
	}

	public override void OnClose()
	{
		if (_snackbarHost == this)
		{
			_snackbarHost = null;
			M3Snackbar.Clear();
		}

		_fold.Reset();
		base.OnClose();
	}

	protected void DrawSnackbar(Vector2 areaMin, Vector2 areaMax)
	{
		if (_snackbarHost == null || !_snackbarHost.IsOpen || _snackbarHost.IsMinimized || ImGui.IsWindowFocused(ImGuiFocusedFlags.RootAndChildWindows))
		{
			_snackbarHost = this;
		}

		if (_snackbarHost == this)
		{
			M3Snackbar.Draw(areaMin, areaMax);
		}
	}

	public sealed override void Draw()
	{
		_fold.BlocksGameInput = BlocksGameInput;
		_fold.BeginDraw();
		DrawAlways();

		if (_fold.Amount < 1f)
		{
			using var alpha = ImRaii.PushStyle(ImGuiStyleVar.Alpha, _fold.ContentAlpha);
			DrawBackdrop(_fold.Rounding);

			using var content = _fold.BeginContent("##fold_content");
			if (content)
			{
				DrawOpen();
			}
		}

		var actions = WindowActions;
		var first = actions.Length - Math.Min(_shownActions, actions.Length);
		var pressed = _fold.DrawBar("##fold_actions", actions.AsSpan(first), Brand, out var closed, M3.Scheme.SurfaceContainerHigh);
		if (pressed >= 0)
		{
			OnWindowAction(first + pressed);
		}

		if (closed)
		{
			OnCloseRequested();
		}

		DrawLast();
	}

	protected abstract void DrawOpen();

	protected virtual void DrawAlways()
	{
	}

	protected virtual void DrawLast()
	{
	}

	protected virtual void DrawBackdrop(float rounding)
	{
		M3Draw.WindowBackdrop(rounding);
	}

	protected virtual void OnWindowAction(int index)
	{
	}

	protected virtual void OnCloseRequested()
	{
		IsOpen = false;
	}

	protected void SetActionBar(int shownActions, float barTop)
	{
		_shownActions = shownActions;
		_fold.BarTop = barTop;
	}

	internal static void ResetSnackbarHost()
	{
		_snackbarHost = null;
	}
}
