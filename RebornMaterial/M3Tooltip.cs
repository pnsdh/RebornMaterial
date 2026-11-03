using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public static class M3Tooltip
{
	private const float WrapEms = 28f;

	public static Action<string>? Handler { get; set; }

	public static void Hovered(string? text)
	{
		if (ImGui.IsItemHovered(ImGuiHoveredFlags.AllowWhenDisabled))
		{
			Show(text);
		}
	}

	public static void Show(string? text)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		if (Handler != null)
		{
			Handler(text);
			return;
		}

		using var tooltip = ImRaii.Tooltip();
		using var wrap = ImRaii.TextWrapPos(ImGui.GetFontSize() * WrapEms);
		ImGui.TextUnformatted(text);
	}
}
