namespace RebornMaterial;

public interface IM3Settings
{
	Vector4 UiAccentColor { get; set; }

	float UiTextScale { get; set; }

	float UiElementScale { get; set; }

	float UiPaddingScale
	{
		get => 1f;
		set { }
	}

	float UiMotionScale
	{
		get => 1f;
		set { }
	}
}

public sealed class M3Settings : IM3Settings
{
	public Vector4 UiAccentColor { get; set; } = M3.DefaultSeed;

	public float UiTextScale { get; set; } = 1f;

	public float UiElementScale { get; set; } = 1f;

	public float UiPaddingScale { get; set; } = 1f;

	public float UiMotionScale { get; set; } = 1f;
}
