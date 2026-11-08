namespace RebornMaterial;

public sealed class M3SettingsEntry(string search, Action draw, Func<bool>? visible = null)
{
	private readonly Func<bool>? _visible = visible;

	public string Search { get; } = search;

	public Action Draw { get; } = draw;

	public bool IsVisible => _visible?.Invoke() ?? true;

	public bool Matches(string text)
	{
		return Search.Contains(text, StringComparison.OrdinalIgnoreCase);
	}
}

public sealed class M3SettingsGroup(string id, string title, FontAwesomeIcon icon, string? subtitle, params M3SettingsEntry[] entries)
{
	public string Id { get; } = id;

	public string Title { get; } = title;

	public FontAwesomeIcon Icon { get; } = icon;

	public string? Subtitle { get; } = subtitle;

	public M3SettingsEntry[] Entries { get; } = entries;
}

public sealed class M3SettingsPage(string id, string label, FontAwesomeIcon icon, string description, params M3SettingsGroup[] groups)
{
	public string Id { get; } = id;

	public string Label { get; } = label;

	public FontAwesomeIcon Icon { get; } = icon;

	public string Description { get; } = description;

	public M3SettingsGroup[] Groups { get; } = groups;

	public void Draw()
	{
		var accent = 0;
		foreach (var group in Groups)
		{
			using var card = M3Card.Begin($"settings_{Id}_{group.Id}", group.Title, group.Icon, M3.AccentFor(accent++), M3CardStyle.Filled, group.Subtitle);
			foreach (var entry in group.Entries)
			{
				if (entry.IsVisible)
				{
					entry.Draw();
				}
			}
		}
	}
}

public sealed class M3SettingsSearchResult(M3SettingsPage page, M3SettingsGroup group, bool wholeGroup)
{
	public M3SettingsPage Page { get; } = page;

	public M3SettingsGroup Group { get; } = group;

	public bool WholeGroup { get; } = wholeGroup;

	public List<M3SettingsEntry> Entries { get; } = [];
}

public static class M3SettingsSearch
{
	private const int MaxResults = 40;

	public static void Find(IEnumerable<M3SettingsPage> pages, string text, List<M3SettingsSearchResult> results)
	{
		results.Clear();
		text = text.Trim();
		if (text.Length == 0)
		{
			return;
		}

		foreach (var page in pages)
		{
			var pageMatches = page.Label.Contains(text, StringComparison.OrdinalIgnoreCase);
			foreach (var group in page.Groups)
			{
				var whole = pageMatches || group.Title.Contains(text, StringComparison.OrdinalIgnoreCase);
				var result = new M3SettingsSearchResult(page, group, whole);
				foreach (var entry in group.Entries)
				{
					if (whole || entry.Matches(text))
					{
						result.Entries.Add(entry);
					}
				}

				if (result.Entries.Count > 0)
				{
					results.Add(result);
				}
			}
		}
	}

	public static M3SettingsPage? Draw(IReadOnlyList<M3SettingsSearchResult> results, string text)
	{
		if (results.Count == 0)
		{
			M3Widgets.EmptyState("##m3_settings_search_empty", "No settings found", $"Nothing matches \"{text.Trim()}\". Try a shorter or different word.",
				FontAwesomeIcon.Search);
			return null;
		}

		M3SettingsPage? open = null;
		var shown = 0;
		foreach (var result in results)
		{
			if (++shown > MaxResults)
			{
				M3Text.Draw($"Showing the first {MaxResults} of {results.Count} groups. Refine the search to see the rest.", M3TextStyle.Muted, true);
				break;
			}

			using var card = M3Card.Begin($"search_{result.Page.Id}_{result.Group.Id}", $"{result.Page.Label} › {result.Group.Title}", result.Group.Icon,
				M3.AccentFor(shown - 1), M3CardStyle.Outlined);
			foreach (var entry in result.Entries)
			{
				if (entry.IsVisible)
				{
					entry.Draw();
				}
			}

			if (M3SettingRow.NavigationRow($"##search_open_{result.Page.Id}_{result.Group.Id}", $"Open {result.Page.Label}", null,
				FontAwesomeIcon.ExternalLinkAlt, FontAwesomeIcon.ChevronRight, M3.Scheme.Tertiary))
			{
				open = result.Page;
			}
		}

		return open;
	}
}
