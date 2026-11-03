using Dalamud.IoC;
using Dalamud.Plugin.Services;

namespace RebornMaterial;

internal sealed class M3Services
{
	[PluginService]
	internal static ITextureProvider TextureProvider { get; private set; } = null!;
}
