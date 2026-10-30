namespace RebornMaterial;

public static partial class M3Widgets
{
	private static Vector2 BarSize(Vector2 size)
	{
		return size.X > 0f ? size : size with { X = MathF.Max(1f, ImGui.GetContentRegionAvail().X) };
	}

	public static void LinearProgress(Vector2 size, float fraction, float? marker = null, Vector4? markerColor = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;

		size = BarSize(size);
		ImGui.Dummy(size);
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = size.Y * 0.5f;
		var centerY = (min.Y + max.Y) * 0.5f;

		fraction = Math.Clamp(fraction, 0f, 1f);
		var split = float.Lerp(min.X, max.X, fraction);

		var gap = fraction > 0f && fraction < 1f ? MathF.Min(4f * scale, size.Y) : 0f;
		var trackStart = MathF.Min(max.X, split + gap);
		if (max.X - trackStart > 0.5f)
		{
			drawList.AddRectFilled(new Vector2(trackStart, min.Y), max, M3.U32(s.SecondaryContainer), rounding);

			if (max.X - trackStart >= size.Y)
			{
				var stopRadius = MathF.Min(2f * scale, rounding);
				drawList.AddCircleFilled(new Vector2(max.X - rounding, centerY), stopRadius, M3.U32(s.Primary), 12);
			}
		}

		if (split - min.X > 0.5f)
		{
			drawList.AddRectFilled(min, new Vector2(split, max.Y), M3.U32(s.Primary), rounding);
		}

		if (marker is { } at && at > 0f && at < 1f)
		{
			var x = float.Lerp(min.X, max.X, at);
			var overhang = 2f * scale;
			drawList.AddLine(new Vector2(x, min.Y - overhang), new Vector2(x, max.Y + overhang),
				M3.U32(markerColor ?? s.Tertiary), 2f * scale);
		}
	}

	public static void LinearProgressIndeterminate(Vector2 size)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;

		size = BarSize(size);
		ImGui.Dummy(size);
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var rounding = size.Y * 0.5f;
		var gap = MathF.Min(4f * scale, size.Y);

		const float Period = 2f;
		var t = (float)(ImGui.GetTime() % Period) / Period;
		Span<Vector2> segments =
		[
			new(Ease((t - 0.20f) / 0.60f), Ease(t / 0.55f)),
			new(Ease((t - 0.70f) / 0.30f), Ease((t - 0.45f) / 0.40f)),
		];

		if (segments[1].X < segments[0].X)
		{
			(segments[0], segments[1]) = (segments[1], segments[0]);
		}

		var trackStart = min.X;
		foreach (var segment in segments)
		{
			var from = float.Lerp(min.X, max.X, segment.X);
			var to = float.Lerp(min.X, max.X, segment.Y);
			if (to - from < 0.5f)
			{
				continue;
			}

			if (from - gap - trackStart > 0.5f)
			{
				drawList.AddRectFilled(new Vector2(trackStart, min.Y), new Vector2(from - gap, max.Y), M3.U32(s.SecondaryContainer), rounding);
			}

			drawList.AddRectFilled(new Vector2(from, min.Y), new Vector2(to, max.Y), M3.U32(s.Primary), rounding);
			trackStart = to + gap;
		}

		if (max.X - trackStart > 0.5f)
		{
			drawList.AddRectFilled(new Vector2(trackStart, min.Y), max, M3.U32(s.SecondaryContainer), rounding);
		}
	}

	public static void CircularProgress(float diameter, float? fraction = null, float thickness = 4f)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var size = Vector2.One * diameter;

		ImGui.Dummy(size);
		var center = ImGui.GetItemRectMin() + (size * 0.5f);
		var stroke = thickness * scale;
		var radius = MathF.Max(1f, (diameter - stroke) * 0.5f);
		var drawList = ImGui.GetWindowDrawList();

		if (fraction is { } value)
		{
			value = Math.Clamp(value, 0f, 1f);

			var gap = MathF.Min(0.1f, stroke * 1.5f / (MathF.Tau * radius));
			if (value <= 0.001f)
			{
				M3Draw.Arc(drawList, center, radius, 0f, 1f, s.SecondaryContainer, stroke);
				return;
			}

			if (value < 0.999f && 1f - value - (gap * 2f) > 0.001f)
			{
				M3Draw.Arc(drawList, center, radius, value + gap, 1f - gap, s.SecondaryContainer, stroke);
			}

			M3Draw.Arc(drawList, center, radius, 0f, value, s.Primary, stroke);
			if (value < 0.999f)
			{
				RoundCaps(drawList, center, radius, 0f, value, s.Primary, stroke);
			}

			return;
		}

		Spinner(drawList, center, radius, s.Primary, stroke);
	}

	public static void Spinner(ImDrawListPtr drawList, Vector2 center, float radius, Vector4 color, float stroke)
	{
		const float Cycle = 1.333f;
		var time = (float)ImGui.GetTime();
		var cycles = MathF.Floor(time / Cycle);
		var phase = (time - (cycles * Cycle)) / Cycle;
		var head = Ease(phase / 0.5f) * 0.75f;
		var tail = Ease((phase - 0.5f) / 0.5f) * 0.75f;
		var start = ((time * 0.25f) + (cycles * 0.75f) + tail) % 1f;
		var end = start + (head - tail) + 0.03f;

		M3Draw.Arc(drawList, center, radius, start, end, color, stroke);
		RoundCaps(drawList, center, radius, start, end, color, stroke);
	}

	private static float Ease(float t)
	{
		t = Math.Clamp(t, 0f, 1f);
		return t * t * (3f - (2f * t));
	}

	private static void RoundCaps(ImDrawListPtr drawList, Vector2 center, float radius, float from, float to, Vector4 color, float thickness)
	{
		RoundCap(drawList, center, radius, from, color, thickness);
		RoundCap(drawList, center, radius, to, color, thickness);
	}

	private static void RoundCap(ImDrawListPtr drawList, Vector2 center, float radius, float at, Vector4 color, float thickness)
	{
		var angle = (-MathF.PI * 0.5f) + (MathF.Tau * at);
		var point = center + (new Vector2(MathF.Cos(angle), MathF.Sin(angle)) * radius);
		drawList.AddCircleFilled(point, thickness * 0.5f, M3.U32(color), 12);
	}
}
