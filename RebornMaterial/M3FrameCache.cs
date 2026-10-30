using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace RebornMaterial;

internal sealed class M3FrameCache<T>(int capacity)
{
	private readonly Dictionary<uint, (T Value, int Frame)> _entries = [];
	private int _limit;

	public bool TryGet(uint key, out T value)
	{
		ref var entry = ref CollectionsMarshal.GetValueRefOrNullRef(_entries, key);
		if (Unsafe.IsNullRef(ref entry))
		{
			value = default!;
			return false;
		}

		entry.Frame = ImGui.GetFrameCount();
		value = entry.Value;
		return true;
	}

	public void Set(uint key, T value)
	{
		var frame = ImGui.GetFrameCount();
		if (_entries.Count >= Math.Max(capacity, _limit) && !_entries.ContainsKey(key))
		{
			foreach (var (stale, entry) in _entries)
			{
				if (entry.Frame < frame - 1)
				{
					_ = _entries.Remove(stale);
				}
			}

			_limit = _entries.Count * 2;
		}

		_entries[key] = (value, frame);
	}

	public void Clear()
	{
		_entries.Clear();
		_limit = 0;
	}
}
