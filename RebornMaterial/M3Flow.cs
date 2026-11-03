namespace RebornMaterial;

public struct M3Flow(float? gap = null, float? width = null)
{
	private readonly float _gap = gap ?? M3.Space2;
	private readonly float _available = width ?? ImGui.GetContentRegionAvail().X;
	private float _used;

	public M3Flow()
		: this(null, null)
	{
	}

	public void Next(float width)
	{
		if (_used > 0f && _used + _gap + width <= _available)
		{
			ImGui.SameLine(0f, _gap);
			_used += _gap + width;
		}
		else
		{
			_used = width;
		}
	}
}
