using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

public readonly record struct M3ChartSeries(string Name, IReadOnlyList<float> Values);

// Series take the palette's colours in order. There are eight, and they never repeat: fold extra series into an "Other" series.
public static class M3Chart
{
	public const int MaxSeries = 8;

	// Checked for colour-blind separation and contrast against this library's surfaces, from the window background up to standard containers.
	public static readonly Vector4[] Palette =
	[
		M3ColorMath.FromRgb(0x3987E5),
		M3ColorMath.FromRgb(0xD95926),
		M3ColorMath.FromRgb(0x199E70),
		M3ColorMath.FromRgb(0xC98500),
		M3ColorMath.FromRgb(0xD55181),
		M3ColorMath.FromRgb(0x008300),
		M3ColorMath.FromRgb(0x9085E9),
		M3ColorMath.FromRgb(0xE66767),
	];

	public static Vector4 SeriesColor(int index)
	{
		return Palette[index];
	}

	// Stacked puts each category's series on top of each other; otherwise they stand side by side. Values below zero draw as zero.
	public static void Bars(string id, IReadOnlyList<string> categories, IReadOnlyList<M3ChartSeries> series, Vector2 size, bool stacked = true,
		Func<float, string>? format = null, string? yTitle = null, string? xTitle = null)
	{
		CheckSeries(series);
		format ??= FormatValue;

		var top = 0f;
		for (var c = 0; c < categories.Count; c++)
		{
			var sum = 0f;
			foreach (var item in series)
			{
				var value = ValueAt(item, c);
				sum = stacked ? sum + value : MathF.Max(sum, value);
			}

			top = MathF.Max(top, sum);
		}

		var layout = BeginChart(id, categories, series, size, 0f, top, format, yTitle, xTitle, false);
		if (layout.Plot.Width <= 0f || categories.Count == 0)
		{
			EndChart(layout);
			return;
		}

		var s = M3.Scheme;
		var scale = M3.Scale;
		var drawList = ImGui.GetWindowDrawList();
		var slot = layout.Plot.Width / categories.Count;
		var gap = 2f * scale;
		var rounding = 4f * scale;
		var hovered = layout.Hovered ? Math.Clamp((int)((ImGui.GetMousePos().X - layout.Plot.Left) / slot), 0, categories.Count - 1) : -1;

		for (var c = 0; c < categories.Count; c++)
		{
			var center = layout.Plot.Left + (slot * (c + 0.5f));
			if (c == hovered)
			{
				drawList.AddRectFilled(new Vector2(center - (slot * 0.5f), layout.Plot.Top), new Vector2(center + (slot * 0.5f), layout.Plot.Bottom),
					M3.U32(s.OnSurface, M3.StateHover), M3.ShapeExtraSmall);
			}

			if (stacked)
			{
				var barWidth = MathF.Min(24f * scale, slot * 0.7f);
				var lastShown = -1;
				for (var i = 0; i < series.Count; i++)
				{
					if (ValueAt(series[i], c) > 0f)
					{
						lastShown = i;
					}
				}

				var bottom = layout.Plot.Bottom;
				for (var i = 0; i < series.Count; i++)
				{
					var height = ValueAt(series[i], c) / layout.Top * layout.Plot.Height;
					if (height <= 0f)
					{
						continue;
					}

					var start = bottom - (i > 0 && bottom < layout.Plot.Bottom ? gap : 0f);
					var end = bottom - height;
					if (start - end > 0.5f)
					{
						DrawBar(drawList, center - (barWidth * 0.5f), barWidth, end, start, Palette[i], i == lastShown ? rounding : 0f);
					}

					bottom = end;
				}
			}
			else
			{
				var groupWidth = MathF.Min(slot * 0.8f, (series.Count * 24f * scale) + ((series.Count - 1) * gap));
				var barWidth = (groupWidth - ((series.Count - 1) * gap)) / series.Count;
				var left = center - (groupWidth * 0.5f);
				for (var i = 0; i < series.Count; i++)
				{
					var height = ValueAt(series[i], c) / layout.Top * layout.Plot.Height;
					if (height > 0.5f)
					{
						DrawBar(drawList, left, barWidth, layout.Plot.Bottom - height, layout.Plot.Bottom, Palette[i], rounding);
					}

					left += barWidth + gap;
				}
			}
		}

		if (hovered >= 0)
		{
			Readout(categories[hovered], series, hovered, format, false);
		}

		EndChart(layout);
	}

	public static void Lines(string id, IReadOnlyList<string> xLabels, IReadOnlyList<M3ChartSeries> series, Vector2 size,
		Func<float, string>? format = null, string? yTitle = null, string? xTitle = null)
	{
		CheckSeries(series);
		format ??= FormatValue;

		var low = 0f;
		var high = 0f;
		foreach (var item in series)
		{
			foreach (var value in item.Values)
			{
				if (!float.IsNaN(value))
				{
					low = MathF.Min(low, value);
					high = MathF.Max(high, value);
				}
			}
		}

		var layout = BeginChart(id, xLabels, series, size, low, high, format, yTitle, xTitle, true);
		var count = xLabels.Count;
		if (layout.Plot.Width <= 0f || count == 0)
		{
			EndChart(layout);
			return;
		}

		var s = M3.Scheme;
		var scale = M3.Scale;
		var drawList = ImGui.GetWindowDrawList();

		float X(int index)
		{
			return count == 1 ? layout.Plot.Left + (layout.Plot.Width * 0.5f) : layout.Plot.Left + (layout.Plot.Width * index / (count - 1));
		}

		float Y(float value)
		{
			return layout.Plot.Bottom - ((value - layout.Bottom) / (layout.Top - layout.Bottom) * layout.Plot.Height);
		}

		for (var i = 0; i < series.Count; i++)
		{
			var color = M3.U32(Palette[i]);
			var drawing = 0;
			var last = Vector2.Zero;
			for (var p = 0; p < count; p++)
			{
				var value = p < series[i].Values.Count ? series[i].Values[p] : float.NaN;
				if (float.IsNaN(value))
				{
					drawing = StrokePath(drawList, drawing, last, color, scale);
					continue;
				}

				last = new Vector2(X(p), Y(value));
				drawList.PathLineTo(last);
				drawing++;
			}

			StrokePath(drawList, drawing, last, color, scale);
		}

		if (!layout.Hovered)
		{
			EndChart(layout);
			return;
		}

		var nearest = count == 1 ? 0 : Math.Clamp((int)MathF.Round((ImGui.GetMousePos().X - layout.Plot.Left) / layout.Plot.Width * (count - 1)), 0, count - 1);
		var x = X(nearest);
		drawList.AddLine(new Vector2(x, layout.Plot.Top), new Vector2(x, layout.Plot.Bottom), M3.U32(s.OnSurfaceVariant, 0.5f), 1f * scale);
		for (var i = 0; i < series.Count; i++)
		{
			var value = nearest < series[i].Values.Count ? series[i].Values[nearest] : float.NaN;
			if (!float.IsNaN(value))
			{
				var point = new Vector2(x, Y(value));
				drawList.AddCircleFilled(point, 6f * scale, M3.U32(s.Surface), 16);
				drawList.AddCircleFilled(point, 4f * scale, M3.U32(Palette[i]), 16);
			}
		}

		Readout(xLabels[nearest], series, nearest, format, true);
		EndChart(layout);
	}

	public static void Table(string id, IReadOnlyList<string> categories, IReadOnlyList<M3ChartSeries> series, Func<float, string>? format = null, string? categoryTitle = null)
	{
		format ??= FormatValue;
		using var table = ImRaii.Table(id, series.Count + 1, ImGuiTableFlags.RowBg | ImGuiTableFlags.BordersInnerH | ImGuiTableFlags.SizingStretchProp);
		if (!table)
		{
			return;
		}

		ImGui.TableSetupColumn(categoryTitle ?? string.Empty);
		foreach (var item in series)
		{
			ImGui.TableSetupColumn(item.Name);
		}

		ImGui.TableHeadersRow();
		for (var c = 0; c < categories.Count; c++)
		{
			ImGui.TableNextRow();
			ImGui.TableNextColumn();
			ImGui.TextUnformatted(categories[c]);
			foreach (var item in series)
			{
				ImGui.TableNextColumn();
				var value = c < item.Values.Count ? item.Values[c] : float.NaN;
				var text = float.IsNaN(value) ? "-" : format(value);
				ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0f, ImGui.GetContentRegionAvail().X - ImGui.CalcTextSize(text).X));
				ImGui.TextUnformatted(text);
			}
		}
	}

	public static string FormatValue(float value)
	{
		var size = MathF.Abs(value);
		return size >= 1_000_000f ? $"{value / 1_000_000f:0.#}M"
			: size >= 10_000f ? $"{value / 1_000f:0.#}K"
			: value.ToString("#,0.##");
	}

	private readonly record struct PlotRect(float Left, float Top, float Right, float Bottom)
	{
		public float Width => Right - Left;
		public float Height => Bottom - Top;
	}

	private readonly record struct ChartLayout(Vector2 Min, Vector2 Size, PlotRect Plot, float Bottom, float Top, bool Hovered);

	private static void CheckSeries(IReadOnlyList<M3ChartSeries> series)
	{
		if (series.Count > MaxSeries)
		{
			throw new ArgumentException($"A chart takes at most {MaxSeries} series. Fold the rest into an \"Other\" series.", nameof(series));
		}
	}

	private static float ValueAt(in M3ChartSeries series, int index)
	{
		var value = index < series.Values.Count ? series.Values[index] : 0f;
		return float.IsNaN(value) ? 0f : MathF.Max(0f, value);
	}

	private static float NiceStep(float range, int ticks)
	{
		var rough = range / MathF.Max(1, ticks);
		var power = MathF.Pow(10f, MathF.Floor(MathF.Log10(rough)));
		var fraction = rough / power;
		var nice = fraction <= 1f ? 1f : fraction <= 2f ? 2f : fraction <= 2.5f ? 2.5f : fraction <= 5f ? 5f : 10f;
		return nice * power;
	}

	private static ChartLayout BeginChart(string id, IReadOnlyList<string> labels, IReadOnlyList<M3ChartSeries> series, Vector2 size, float low, float high,
		Func<float, string> format, string? yTitle, string? xTitle, bool lines)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var width = size.X > 0f ? size.X : MathF.Max(64f * scale, ImGui.GetContentRegionAvail().X);
		var height = MathF.Max(80f * scale, size.Y);
		var min = ImGui.GetCursorScreenPos();
		var drawList = ImGui.GetWindowDrawList();

		using var font = ImRaii.PushFont(M3.LabelSmall);
		var lineHeight = ImGui.GetTextLineHeight();
		var y = min.Y;

		if (series.Count >= 2)
		{
			y = Legend(drawList, series, new Vector2(min.X, y), width, lines) + (8f * scale);
		}

		if (!string.IsNullOrEmpty(yTitle))
		{
			drawList.AddText(new Vector2(min.X, y), M3.U32(s.OnSurfaceVariant), yTitle);
			y += lineHeight + (4f * scale);
		}

		var bottomRoom = lineHeight + (6f * scale) + (string.IsNullOrEmpty(xTitle) ? 0f : lineHeight + (4f * scale));
		var plotTop = y + (lineHeight * 0.5f);
		var plotBottom = MathF.Max(plotTop + (24f * scale), min.Y + height - bottomRoom);

		var ticks = Math.Clamp((int)((plotBottom - plotTop) / (36f * scale)), 2, 6);
		if (high <= low)
		{
			high = low + 1f;
		}

		var step = NiceStep(high - low, ticks);
		var bottom = MathF.Floor(low / step) * step;
		var top = MathF.Ceiling(high / step) * step;
		if (top <= bottom)
		{
			top = bottom + step;
		}

		var labelWidth = 0f;
		for (var tick = bottom; tick <= top + (step * 0.5f); tick += step)
		{
			labelWidth = MathF.Max(labelWidth, ImGui.CalcTextSize(format(tick)).X);
		}

		var plot = new PlotRect(min.X + labelWidth + (8f * scale), plotTop, min.X + width - (4f * scale), plotBottom);

		for (var tick = bottom; tick <= top + (step * 0.5f); tick += step)
		{
			var tickY = plot.Bottom - ((tick - bottom) / (top - bottom) * plot.Height);
			var isZero = MathF.Abs(tick) < step * 0.001f;
			drawList.AddLine(new Vector2(plot.Left, tickY), new Vector2(plot.Right, tickY),
				M3.U32(isZero ? s.Outline : s.OutlineVariant, isZero ? 0.8f : 0.5f), 1f * scale);
			var text = format(tick);
			var textSize = ImGui.CalcTextSize(text);
			drawList.AddText(new Vector2(plot.Left - (8f * scale) - textSize.X, tickY - (textSize.Y * 0.5f)), M3.U32(s.OnSurfaceVariant), text);
		}

		XLabels(drawList, labels, plot, lines, s, scale);

		if (!string.IsNullOrEmpty(xTitle))
		{
			var titleSize = ImGui.CalcTextSize(xTitle);
			drawList.AddText(new Vector2(plot.Left + ((plot.Width - titleSize.X) * 0.5f), plot.Bottom + lineHeight + (10f * scale)),
				M3.U32(s.OnSurfaceVariant), xTitle);
		}

		ImGui.SetCursorScreenPos(new Vector2(plot.Left, plot.Top));
		_ = ImGui.InvisibleButton(id, new Vector2(MathF.Max(1f, plot.Width), MathF.Max(1f, plot.Height)));
		var hovered = ImGui.IsItemHovered();

		return new ChartLayout(min, new Vector2(width, height), plot, bottom, top, hovered);
	}

	private static void EndChart(in ChartLayout layout)
	{
		ImGui.SetCursorScreenPos(layout.Min);
		ImGui.Dummy(layout.Size);
	}

	private static void XLabels(ImDrawListPtr drawList, IReadOnlyList<string> labels, PlotRect plot, bool lines, M3Scheme s, float scale)
	{
		var count = labels.Count;
		if (count == 0)
		{
			return;
		}

		var widest = 0f;
		foreach (var label in labels)
		{
			widest = MathF.Max(widest, ImGui.CalcTextSize(label).X);
		}

		var spacing = lines && count > 1 ? plot.Width / (count - 1) : plot.Width / count;
		var every = Math.Max(1, (int)MathF.Ceiling((widest + (12f * scale)) / MathF.Max(1f, spacing)));

		for (var i = 0; i < count; i += every)
		{
			var x = lines
				? count == 1 ? plot.Left + (plot.Width * 0.5f) : plot.Left + (spacing * i)
				: plot.Left + (spacing * (i + 0.5f));
			var size = ImGui.CalcTextSize(labels[i]);
			var left = Math.Clamp(x - (size.X * 0.5f), plot.Left - (size.X * 0.5f), plot.Right - size.X);
			drawList.AddText(new Vector2(left, plot.Bottom + (4f * scale)), M3.U32(s.OnSurfaceVariant), labels[i]);
		}
	}

	private static float Legend(ImDrawListPtr drawList, IReadOnlyList<M3ChartSeries> series, Vector2 origin, float width, bool lines)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var lineHeight = ImGui.GetTextLineHeight();
		var key = 12f * scale;
		var x = origin.X;
		var y = origin.Y;

		for (var i = 0; i < series.Count; i++)
		{
			var name = series[i].Name;
			var itemWidth = key + (6f * scale) + ImGui.CalcTextSize(name).X;
			if (x > origin.X && x + itemWidth > origin.X + width)
			{
				x = origin.X;
				y += lineHeight + (4f * scale);
			}

			var middle = y + (lineHeight * 0.5f);
			if (lines)
			{
				drawList.AddLine(new Vector2(x, middle), new Vector2(x + key, middle), M3.U32(Palette[i]), 2f * scale);
			}
			else
			{
				drawList.AddRectFilled(new Vector2(x, middle - (key * 0.5f)), new Vector2(x + key, middle + (key * 0.5f)), M3.U32(Palette[i]), 2f * scale);
			}

			drawList.AddText(new Vector2(x + key + (6f * scale), y), M3.U32(s.OnSurface, 0.9f), name);
			x += itemWidth + (16f * scale);
		}

		return y + lineHeight;
	}

	private static void Readout(string heading, IReadOnlyList<M3ChartSeries> series, int index, Func<float, string> format, bool lines)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		using var tooltip = ImRaii.Tooltip();
		M3Text.Draw(heading, M3TextStyle.Label);

		var lineHeight = ImGui.GetTextLineHeight();
		var key = 12f * scale;
		for (var i = 0; i < series.Count; i++)
		{
			var value = index < series[i].Values.Count ? series[i].Values[index] : float.NaN;
			var origin = ImGui.GetCursorScreenPos();
			ImGui.Dummy(new Vector2(key, lineHeight));
			var middle = origin.Y + (lineHeight * 0.5f);
			ImGui.GetWindowDrawList().AddLine(new Vector2(origin.X, middle), new Vector2(origin.X + key, middle), M3.U32(Palette[i]), (lines ? 2f : 4f) * scale);

			ImGui.SameLine(0f, 6f * scale);
			ImGui.TextUnformatted(float.IsNaN(value) ? "-" : format(value));
			if (series.Count > 1 || !string.IsNullOrEmpty(series[i].Name))
			{
				ImGui.SameLine(0f, 6f * scale);
				M3Text.Draw(series[i].Name, M3TextStyle.Muted);
			}
		}
	}

	private static int StrokePath(ImDrawListPtr drawList, int points, Vector2 last, uint color, float scale)
	{
		if (points >= 2)
		{
			drawList.PathStroke(color, ImDrawFlags.None, 2f * scale);
		}
		else
		{
			drawList.PathClear();
			if (points == 1)
			{
				drawList.AddCircleFilled(last, 2.5f * scale, color, 12);
			}
		}

		return 0;
	}

	private static void DrawBar(ImDrawListPtr drawList, float left, float width, float top, float bottom, Vector4 color, float rounding)
	{
		var radius = MathF.Min(rounding, MathF.Min(width, bottom - top) * 0.5f);
		drawList.AddRectFilled(new Vector2(left, top), new Vector2(left + width, bottom), M3.U32(color), radius,
			radius > 0f ? ImDrawFlags.RoundCornersTop : ImDrawFlags.None);
	}
}
