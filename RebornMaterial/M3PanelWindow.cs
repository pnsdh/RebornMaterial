using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace RebornMaterial;

public abstract class M3PanelWindow(string name, string title, Vector2 initialSize, WindowSizeConstraints? constraints = null)
	: M3FoldWindow(name, title, initialSize, constraints)
{
	private string _loggedError = string.Empty;

	protected virtual string Title => BrandLabel;

	protected virtual (string Text, Vector4? Color) Status => (string.Empty, null);

	protected virtual bool BodyScrolls => true;

	protected abstract void DrawContents();

	protected override void DrawOpen()
	{
		var (status, statusColor) = Status;
		DrawHeader(Title, status, statusColor);

		var origin = ImGui.GetCursorScreenPos();
		var available = ImGui.GetContentRegionAvail();
		using (var body = ImRaii.Child("##panel_body", -Vector2.One, false,
			BodyScrolls ? ImGuiWindowFlags.None : ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse))
		{
			if (body)
			{
				try
				{
					DrawContents();
				}
				catch (Exception ex)
				{
					var error = $"{Title}: {ex.Message}";
					if (error != _loggedError)
					{
						_loggedError = error;
						M3Services.Log?.Error(ex, $"[RebornMaterial] Exception while drawing {Title}");
					}

					M3Widgets.Banner("##panel_error", $"Something went wrong while drawing this window: {ex.Message}", M3Severity.Error, FontAwesomeIcon.Bug);
				}
			}
		}

		DrawSnackbar(origin, origin + available);
	}

	private void DrawHeader(string title, string status, Vector4? statusColor)
	{
		var s = M3.Scheme;
		var scale = M3.Scale;
		var width = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
		var logoSize = 32f * scale;
		var gap = M3.Space2;
		var lineGap = 2f * M3.PaddingScale;

		var shown = M3Widgets.WindowActionsThatFit(WindowActions.Length, Brand, width - logoSize - gap - (80f * scale), MinimumActions);
		var pill = M3Widgets.WindowActionsSize(shown, Brand, 0f);

		float titleHeight;
		using (ImRaii.PushFont(M3.TitleMedium))
		{
			titleHeight = ImGui.CalcTextSize(title).Y;
		}

		var statusHeight = status.Length > 0 ? ImGui.GetTextLineHeight() : 0f;
		var textHeight = titleHeight + (statusHeight > 0f ? lineGap + statusHeight : 0f);
		var height = MathF.Max(pill.Y, MathF.Max(logoSize, textHeight));

		var origin = ImGui.GetCursorScreenPos();
		var drawList = ImGui.GetWindowDrawList();
		var logoMin = new Vector2(origin.X, origin.Y + ((height - logoSize) * 0.5f));
		M3Brand.DrawLogo(drawList, logoMin, logoSize);

		var textX = logoMin.X + logoSize + gap;
		var textWidth = MathF.Max(0f, origin.X + width - pill.X - gap - textX);
		var textY = origin.Y + ((height - textHeight) * 0.5f);
		using (ImRaii.PushFont(M3.TitleMedium))
		{
			drawList.AddText(new Vector2(textX, textY), M3.U32(s.OnSurface), M3Navigation.Truncate(title, textWidth));
		}

		if (statusHeight > 0f)
		{
			var statusY = textY + titleHeight + lineGap;
			var clipped = M3Navigation.Truncate(status, textWidth);
			drawList.AddText(new Vector2(textX, statusY), M3.U32(statusColor ?? s.OnSurfaceVariant), clipped);
			if (clipped != status && M3Draw.MouseOver(new Vector2(textX, statusY), new Vector2(textX + textWidth, statusY + statusHeight)))
			{
				M3Tooltip.Show(status);
			}
		}

		SetActionBar(shown, (height - pill.Y) * 0.5f);
		ImGui.Dummy(new Vector2(width, height));

		var lineY = ImGui.GetCursorScreenPos().Y + M3.Space1;
		drawList.AddLine(new Vector2(origin.X, lineY), new Vector2(origin.X + width, lineY), M3.U32(s.OutlineVariant, 0.5f), 1f * scale);
		ImGui.Dummy(new Vector2(width, M3.Space2 + (1f * scale)));
	}
}
