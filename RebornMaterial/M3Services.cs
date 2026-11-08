using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace RebornMaterial;

internal sealed class M3Services
{
	[PluginService]
	internal static ITextureProvider TextureProvider { get; private set; } = null!;

	[PluginService]
	internal static IGamepadState GamepadState { get; private set; } = null!;

	[PluginService]
	internal static IGameInteropProvider GameInterop { get; private set; } = null!;

	[PluginService]
	internal static IPluginLog Log { get; private set; } = null!;
}
