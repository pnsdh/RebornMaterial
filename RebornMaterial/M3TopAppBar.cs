using Dalamud.Interface.Utility.Raii;

namespace RebornMaterial;

// A page's title and subtitle at the top of a window drawn without a title bar. It leaves room on the right for the
// window's action pill, and a trailing slot left of the pill for a control such as a clear search button.
// Pass ShownActions and BarTop on to the window's M3WindowFold, so the pill lines up with the bar on the next frame.
public static class M3TopAppBar
{
	public static Scope Begin(string title, string? subtitle, int actionCount, in M3WindowBrand brand, float trailingWidth = 0f, float? gap = null)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var width = MathF.Max(64f * scale, ImGui.GetContentRegionAvail().X);

		// Actions drop from the pill until the title has room.
		var shown = M3Widgets.WindowActionsThatFit(actionCount, brand, width - trailingWidth - (96f * scale));
		var barSize = M3Widgets.WindowActionsSize(shown, brand, 0f);
		var textWidth = MathF.Max(32f * scale, width - barSize.X - trailingWidth - (12f * scale));
		var padding = 4f * M3.PaddingScale;
		var lineGap = 2f * M3.PaddingScale;

		string clippedTitle;
		Vector2 titleSize;
		using (ImRaii.PushFont(M3.HeadlineSmall))
		{
			clippedTitle = M3Navigation.Truncate(title, textWidth);
			titleSize = ImGui.CalcTextSize(clippedTitle);
		}

		var clippedSubtitle = string.Empty;
		var subtitleSize = Vector2.Zero;
		if (!string.IsNullOrEmpty(subtitle))
		{
			using var font = ImRaii.PushFont(M3.LabelSmall);
			clippedSubtitle = M3Navigation.Truncate(subtitle, textWidth);
			subtitleSize = ImGui.CalcTextSize(clippedSubtitle);
		}

		var contentHeight = titleSize.Y + (subtitleSize.Y > 0f ? lineGap + subtitleSize.Y : 0f);

		// Tall enough for the action pill to clear the divider under the bar.
		var height = MathF.Max(barSize.Y + (4f * scale), (padding * 2f) + contentHeight);

		ImGui.Dummy(new Vector2(width, height));
		var min = ImGui.GetItemRectMin();
		var max = ImGui.GetItemRectMax();
		var drawList = ImGui.GetWindowDrawList();
		var textTop = min.Y + ((height - contentHeight) * 0.5f);

		using (ImRaii.PushFont(M3.HeadlineSmall))
		{
			drawList.AddText(new Vector2(min.X, textTop), M3.U32(s.OnSurface, 0.98f), clippedTitle);
		}

		if (subtitleSize.Y > 0f)
		{
			using var font = ImRaii.PushFont(M3.LabelSmall);
			drawList.AddText(new Vector2(min.X, textTop + titleSize.Y + lineGap), M3.U32(s.OnSurfaceVariant, 0.88f), clippedSubtitle);
		}

		return new Scope(min, max, shown, (height - barSize.Y) * 0.5f, max.X - barSize.X - trailingWidth, gap ?? M3.Space2);
	}

	public readonly struct Scope(Vector2 min, Vector2 max, int shownActions, float barTop, float trailingX, float gap) : IDisposable
	{
		public Vector2 Min => min;

		public Vector2 Max => max;

		// How many of the window's actions fit beside the title.
		public int ShownActions => shownActions;

		// How far down the bar the pill sits, for M3WindowFold.BarTop. It assumes the bar is the first thing in the window's content.
		public float BarTop => barTop;

		// Where to put a control of the given height in the trailing slot, centred on the bar.
		public Vector2 TrailingPosition(float itemHeight)
		{
			return new Vector2(trailingX, min.Y + ((max.Y - min.Y - itemHeight) * 0.5f));
		}

		public void Dispose()
		{
			ImGui.SetCursorScreenPos(new Vector2(min.X, max.Y));
			ImGui.GetWindowDrawList().AddLine(new Vector2(min.X, max.Y), max, M3.U32(M3.Scheme.OutlineVariant, 0.5f), 1f * M3.Scale);
			ImGui.Dummy(new Vector2(max.X - min.X, gap));
		}
	}
}
