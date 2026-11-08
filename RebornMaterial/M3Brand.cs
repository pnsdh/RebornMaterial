using System.Reflection;
using Dalamud.Interface.Textures.TextureWraps;

namespace RebornMaterial;

public static class M3Brand
{
	private static Assembly? _logoAssembly;
	private static string? _logoResource;
	private static int _logoFrame = -1;
	private static IDalamudTextureWrap? _logo;

	public static string Name { get; set; } = string.Empty;

	public static string Version { get; set; } = string.Empty;

	public static FontAwesomeIcon FallbackIcon { get; set; } = FontAwesomeIcon.Fire;

	public static float? LogoCornerFraction { get; set; }

	public static Func<IDalamudTextureWrap?>? LogoProvider { get; set; }

	public static void SetLogoResource(Assembly assembly, string resourceName)
	{
		_logoAssembly = assembly;
		_logoResource = resourceName;
		_logoFrame = -1;
	}

	public static IDalamudTextureWrap? Logo
	{
		get
		{
			var frame = ImGui.GetFrameCount();
			if (frame == _logoFrame)
			{
				return _logo;
			}

			_logoFrame = frame;
			try
			{
				if (LogoProvider != null)
				{
					_logo = LogoProvider();
				}
				else if (_logoAssembly != null && _logoResource != null
					&& M3Services.TextureProvider.GetFromManifestResource(_logoAssembly, _logoResource).TryGetWrap(out var logo, out _))
				{
					_logo = logo;
				}
				else
				{
					_logo = null;
				}
			}
			catch
			{
				_logo = null;
			}

			return _logo;
		}
	}

	public static M3WindowBrand For(string label)
	{
		return new M3WindowBrand(Logo, label, FallbackIcon);
	}

	public static float LogoRounding(float size)
	{
		return LogoCornerFraction is { } fraction ? MathF.Max(1f, size * fraction) : M3.ShapeSmall;
	}

	public static void DrawLogo(ImDrawListPtr drawList, Vector2 min, float size)
	{
		var max = min + new Vector2(size);
		var rounding = LogoRounding(size);
		if (M3ActionIcon.Image(drawList, Logo, min, max, rounding))
		{
			return;
		}

		var s = M3.Scheme;
		drawList.AddRectFilled(min, max, M3.U32(s.PrimaryContainer), rounding);
		drawList.AddRect(min, max, M3.U32(s.OutlineVariant, 0.8f), rounding, ImDrawFlags.None, MathF.Max(1f, size * 0.02f));
		M3Draw.IconCentered(drawList, FallbackIcon, min, max, s.OnPrimaryContainer);
	}

	internal static void Reset()
	{
		_logo = null;
		_logoFrame = -1;
	}
}
