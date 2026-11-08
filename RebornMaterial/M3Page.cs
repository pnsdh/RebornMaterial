namespace RebornMaterial;

public sealed class M3Page(
	string id,
	string label,
	FontAwesomeIcon icon,
	string section,
	string? description,
	Action draw,
	bool scrolls = true,
	Func<bool>? visible = null,
	Func<string?>? badge = null,
	bool tool = false,
	string? tooltip = null)
{
	private readonly Func<bool>? _visible = visible;
	private readonly Func<string?>? _badge = badge;

	public string Id { get; } = id;

	public string Label { get; } = label;

	public FontAwesomeIcon Icon { get; } = icon;

	public string Section { get; } = section;

	public string? Description { get; } = description;

	public Action Draw { get; } = draw;

	public bool Scrolls { get; } = scrolls;

	public bool Tool { get; } = tool;

	public string? Tooltip { get; } = tooltip;

	public bool IsVisible => _visible?.Invoke() ?? true;

	public string? Badge => _badge?.Invoke();
}
