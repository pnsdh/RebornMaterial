using Dalamud.Interface.Utility;
using Dalamud.Plugin;

namespace RebornMaterial;

public static class M3
{
	public static readonly Vector4 DefaultSeed = M3ColorMath.FromRgb(0xB0201F);

	private static IDalamudPluginInterface? _pluginInterface;

	private static Vector4 _configuredSeed = DefaultSeed;
	private static M3Scheme _scheme = M3Scheme.FromSeed(DefaultSeed);

	private static float _windowScale = 1f;

	private static float _textScale = 1f;

	private static float _elementScale = 1f;

	private static float _paddingScale = 1f;

	private const int MaxCustomColors = 128;
	private static readonly Dictionary<(Vector4 Color, bool Harmonize), M3ColorRoles> _customColors = [];
	private static M3Scheme? _customColorScheme;

	public static IM3Settings Settings { get; set; } = new M3Settings();

	public static float ElementBaseline { get; set; } = 1f;

	public static bool IsInitialized => _pluginInterface != null;

	internal static IDalamudPluginInterface PluginInterface => _pluginInterface
		?? throw new InvalidOperationException("Call M3.Initialize from the plugin's constructor before drawing any RebornMaterial component.");

	public static void Initialize(IDalamudPluginInterface pluginInterface, IM3Settings? settings = null)
	{
		_pluginInterface = pluginInterface;
		_ = pluginInterface.Create<M3Services>();
		if (settings != null)
		{
			Settings = settings;
		}

		ReadSizes();
	}

	public static void Dispose()
	{
		M3Fonts.DisposeAll();
		M3Fork.DisposeAll();
		M3Snackbar.Clear();
		M3Motion.Reset();
		M3CardHost.Reset();
		M3Widgets.Reset();
		M3Menu.Reset();
		M3Tree.Reset();
		M3Navigation.Reset();
		M3Tooltip.Handler = null;
		_customColors.Clear();
		_customColorScheme = null;
		_pluginInterface = null;
	}

	public static float Scale => ImGuiHelpers.GlobalScale * _elementScale * _windowScale;

	public static float PaddingScale => Scale * _paddingScale;

	public static void BeginFrame()
	{
		if (!ImGui.IsAnyItemActive())
		{
			ReadSizes();
		}
	}

	private static void ReadSizes()
	{
		_textScale = Math.Clamp(Settings.UiTextScale, 0.5f, 3f);
		_elementScale = ElementBaseline * Math.Clamp(Settings.UiElementScale, 0.5f, 2.5f);
		_paddingScale = Math.Clamp(Settings.UiPaddingScale, 0f, 3f);
	}

	public static float TextScale => _textScale * _windowScale;

	public static WindowScaleScope PushWindowScale(float scale)
	{
		var previous = _windowScale;
		_windowScale = previous * Math.Clamp(scale, 0.1f, 10f);
		return new WindowScaleScope(previous);
	}

	public static M3Scheme Scheme
	{
		get
		{
			var seed = Settings.UiAccentColor;
			if (seed.X != _configuredSeed.X || seed.Y != _configuredSeed.Y || seed.Z != _configuredSeed.Z)
			{
				_configuredSeed = seed;

				M3ColorMath.ToLch(seed, out var lightness, out var chroma, out _);
				_scheme = M3Scheme.FromSeed(lightness < 5f || chroma < 2f ? DefaultSeed : seed);
			}

			return _scheme;
		}
	}

	public static float ShapeExtraSmall => 4f * Scale;
	public static float ShapeSmall => 8f * Scale;
	public static float ShapeMedium => 12f * Scale;
	public static float ShapeLarge => 16f * Scale;
	public static float ShapeExtraLarge => 28f * Scale;
	public static float ShapeFull => 999f;

	public static float Space1 => 4f * PaddingScale;
	public static float Space2 => 8f * PaddingScale;
	public static float Space3 => 12f * PaddingScale;

	public const float StateHover = 0.08f;
	public const float StatePressed = 0.10f;
	public const float DisabledContent = 0.38f;
	public const float DisabledContainer = 0.12f;

	public static ImFontPtr Body => M3Fonts.GetDefaultFont(TextScale);
	// Fork: the default font at ratios of its size, not Axis at fixed pixels (see M3Fork).
	public static ImFontPtr HeadlineSmall => M3Fonts.GetDefaultFont(M3Fork.HeadlineSmallRatio * TextScale);
	public static ImFontPtr TitleLarge => M3Fonts.GetDefaultFont(M3Fork.TitleLargeRatio * TextScale);
	public static ImFontPtr TitleMedium => M3Fonts.GetDefaultFont(M3Fork.TitleMediumRatio * TextScale);
	public static ImFontPtr LabelSmall => M3Fonts.GetDefaultFont(M3Fork.LabelSmallRatio * TextScale);

	public static FontScope PushBody()
	{
		if (MathF.Abs(TextScale - 1f) < 0.005f)
		{
			return default;
		}

		ImGui.PushFont(Body);
		return new FontScope(true);
	}

	public static float FitText(float height, float padding)
	{
		return MathF.Max(height * Scale, ImGui.GetTextLineHeight() + (padding * 2f * Scale));
	}

	public static Vector4 Alpha(Vector4 color, float alpha)
	{
		return color with { W = alpha };
	}

	public static Vector4 StateLayer(Vector4 container, Vector4 content, bool hovered, bool active)
	{
		if (!hovered && !active)
		{
			return container;
		}

		var opacity = active ? StatePressed + StateHover : StateHover;
		var mixed = M3ColorMath.Mix(container, content, opacity);
		return mixed with { W = container.W };
	}

	public static Vector4 ContentOn(Vector4 fill)
	{
		M3ColorMath.ToLch(fill, out var lightness, out _, out _);
		return lightness > 60f ? Scheme.Surface : Scheme.OnSurface;
	}

	public static uint U32(Vector4 color)
	{
		return ImGui.GetColorU32(color);
	}

	public static uint U32(Vector4 color, float alpha)
	{
		return ImGui.GetColorU32(color with { W = alpha });
	}

	public static Vector4 Harmonize(Vector4 color)
	{
		M3ColorMath.ToLch(color, out var lightness, out var chroma, out var hue);
		M3ColorMath.ToLch(Scheme.Primary, out _, out _, out var target);

		var difference = M3ColorMath.WrapHue(target - hue + 180f) - 180f;
		var turn = MathF.Min(MathF.Abs(difference) * 0.5f, 15f) * MathF.Sign(difference);
		return M3ColorMath.FromLch(lightness, chroma, M3ColorMath.WrapHue(hue + turn)) with { W = color.W };
	}

	public static M3ColorRoles CustomColor(Vector4 color, bool harmonize = true)
	{
		var scheme = Scheme;
		if (!ReferenceEquals(_customColorScheme, scheme) || _customColors.Count >= MaxCustomColors)
		{
			_customColors.Clear();
			_customColorScheme = scheme;
		}

		if (_customColors.TryGetValue((color, harmonize), out var cached))
		{
			return cached;
		}

		var source = harmonize ? Harmonize(color) : color;
		M3ColorMath.ToLch(source, out _, out var chroma, out var hue);
		var palette = new TonalPalette(hue, MathF.Max(chroma, 16f));
		var roles = new M3ColorRoles(palette[80], palette[20], palette[30], palette[90]);
		_customColors[(color, harmonize)] = roles;
		return roles;
	}

	public static Vector4 AccentFor(int index)
	{
		var scheme = Scheme;
		return (index % 3) switch
		{
			0 => scheme.Primary,
			1 => scheme.Tertiary,
			_ => scheme.Secondary,
		};
	}

	public static Vector4 Severity(M3Severity severity)
	{
		var scheme = Scheme;
		return severity switch
		{
			M3Severity.Error => scheme.Error,
			M3Severity.Warning => scheme.Warning,
			M3Severity.Success => scheme.Success,
			M3Severity.Info => scheme.Info,
			_ => scheme.Primary,
		};
	}

	public static FontAwesomeIcon SeverityIcon(M3Severity severity)
	{
		return severity switch
		{
			M3Severity.Error => FontAwesomeIcon.ExclamationCircle,
			M3Severity.Warning => FontAwesomeIcon.ExclamationTriangle,
			M3Severity.Success => FontAwesomeIcon.CheckCircle,
			M3Severity.Info => FontAwesomeIcon.InfoCircle,
			_ => FontAwesomeIcon.None,
		};
	}

	public static Vector4 SeverityContainer(M3Severity severity)
	{
		var scheme = Scheme;
		return severity switch
		{
			M3Severity.Error => scheme.ErrorContainer,
			M3Severity.Warning => scheme.WarningContainer,
			M3Severity.Success => scheme.SuccessContainer,
			M3Severity.Info => scheme.SecondaryContainer,
			_ => scheme.PrimaryContainer,
		};
	}

	public readonly struct WindowScaleScope(float previous) : IDisposable
	{
		public void Dispose()
		{
			if (previous > 0f)
			{
				_windowScale = previous;
			}
		}
	}

	public readonly struct FontScope(bool pushed) : IDisposable
	{
		public void Dispose()
		{
			if (pushed)
			{
				ImGui.PopFont();
			}
		}
	}
}

public readonly record struct M3ColorRoles(Vector4 Color, Vector4 OnColor, Vector4 Container, Vector4 OnContainer);

public enum M3Severity
{
	Neutral,
	Info,
	Success,
	Warning,
	Error,
}
