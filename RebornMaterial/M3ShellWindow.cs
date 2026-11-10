using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;

namespace RebornMaterial;

public abstract class M3ShellWindow(string name, string idPrefix, string title, string brandLabel, Vector2 initialSize,
	WindowSizeConstraints? constraints = null) : M3FoldWindow(name, brandLabel, initialSize, constraints)
{
	private readonly List<M3Page> _pages = [];
	private readonly List<M3Page> _visible = [];
	private readonly List<M3NavItem> _navItems = [];
	private readonly string _title = title;
	private readonly string _idPrefix = idPrefix;
	private readonly string _navId = $"##{idPrefix}_nav";
	private readonly string _navItemsId = $"{idPrefix}_nav";
	private readonly string _navListId = $"##{idPrefix}_nav_list";
	private readonly string _brandId = $"##{idPrefix}_brand";
	private readonly string _searchId = $"##{idPrefix}_search";
	private readonly string _searchToggleId = $"##{idPrefix}_search_toggle";
	private readonly string _clearSearchId = $"##{idPrefix}_clear_search";
	private readonly string _bodyId = $"##{idPrefix}_body";
	private readonly string _searchContentId = $"##{idPrefix}_page_search";
	private M3Page? _page;
	private M3Page? _contentIdPage;
	private string _contentId = string.Empty;
	private string _loggedPageError = string.Empty;
	private string _version = string.Empty;
	private string _versionSource = string.Empty;

	private string _searchText = string.Empty;
	private bool _railSearch;
	private bool _focusSearch;

	private static float Scale => M3.Scale;

	public string CurrentPageId => CurrentPage?.Id ?? string.Empty;

	protected M3Page? CurrentPage
	{
		get
		{
			if (_page != null && _page.IsVisible)
			{
				return _page;
			}

			_page = null;
			foreach (var page in _pages)
			{
				if (page.IsVisible)
				{
					_page = page;
					break;
				}
			}

			return _page;
		}
	}

	protected IReadOnlyList<M3Page> VisiblePages => RefreshVisiblePages();

	protected int CurrentPageIndex
	{
		get
		{
			var current = CurrentPage;
			if (current == null)
			{
				return -1;
			}

			var index = 0;
			foreach (var page in _pages)
			{
				if (page == current)
				{
					return index;
				}

				if (page.IsVisible)
				{
					index++;
				}
			}

			return -1;
		}
	}

	protected virtual bool GamepadPageStepping => true;

	protected void AddPage(M3Page page)
	{
		_pages.Add(page);
	}

	public bool ShowPage(string id)
	{
		foreach (var page in _pages)
		{
			if (page.Id != id || !page.IsVisible)
			{
				continue;
			}

			if (_page != page)
			{
				_page = page;
				OnPageChanged(page);
			}

			ClearSearch();
			return true;
		}

		return false;
	}

	protected virtual void OnPageChanged(M3Page page)
	{
	}

	protected bool Searching => _searchText.Length > 0;

	protected string SearchText => _searchText;

	private bool RailSearchOpen => _railSearch || Searching;

	protected virtual bool SupportsSearch => false;

	protected virtual string SearchHint => "Search";

	protected virtual void OnSearchChanged(string text)
	{
	}

	protected virtual string SearchSubtitle(string text)
	{
		return string.Empty;
	}

	protected virtual void DrawSearchResults(string text)
	{
	}

	protected void ClearSearch()
	{
		if (_searchText.Length > 0)
		{
			_searchText = string.Empty;
			OnSearchChanged(_searchText);
		}

		_railSearch = false;
	}

	protected virtual void BeforeDrawShell()
	{
	}

	protected virtual float TopAppBarTrailingWidth => 0f;

	protected virtual void DrawTopAppBarTrailing(in M3TopAppBar.Scope bar)
	{
	}

	protected virtual void DrawPageHeader()
	{
	}

	protected virtual float NavigationFooterHeight(bool expanded)
	{
		return 0f;
	}

	protected virtual void DrawNavigationFooter(float width, bool expanded)
	{
	}

	protected override void DrawOpen()
	{
		BeforeDrawShell();
		RefreshVisiblePages();
		StepPageFromGamepad();

		var available = MathF.Max(1f, ImGui.GetContentRegionAvail().X);
		var spacing = ImGui.GetStyle().ItemSpacing.X;
		var expanded = available >= 600f * Scale;
		var sidebarWidth = expanded ? MathF.Min(DrawerWidth(), available * 0.4f) : MathF.Min(84f * Scale, available * 0.3f);

		var origin = ImGui.GetCursorScreenPos();
		var paneHeight = MathF.Max(1f, ImGui.GetContentRegionAvail().Y);
		var dividerX = origin.X + sidebarWidth + (spacing * 0.5f);
		ImGui.GetWindowDrawList().AddLine(new Vector2(dividerX, origin.Y), new Vector2(dividerX, origin.Y + paneHeight),
			M3.U32(M3.Scheme.OutlineVariant, 0.45f), 1f * Scale);

		DrawNavigation(sidebarWidth, expanded);
		ImGui.SameLine(0f, spacing);
		DrawBody(expanded);

		DrawSnackbar(new Vector2(origin.X + sidebarWidth + spacing, origin.Y), new Vector2(origin.X + available, origin.Y + paneHeight));
	}

	private List<M3Page> RefreshVisiblePages()
	{
		_visible.Clear();
		foreach (var page in _pages)
		{
			if (page.IsVisible)
			{
				_visible.Add(page);
			}
		}

		return _visible;
	}

	private void StepPageFromGamepad()
	{
		if (!GamepadPageStepping || Searching || _visible.Count < 2)
		{
			return;
		}

		var step = M3Gamepad.PageStep();
		if (step == 0)
		{
			return;
		}

		var current = CurrentPage;
		var next = M3Navigation.Step(current == null ? -1 : _visible.IndexOf(current), _visible.Count, step);
		ShowPage(_visible[next].Id);
	}

	private string ContentId(M3Page page)
	{
		if (_contentIdPage != page)
		{
			_contentIdPage = page;
			_contentId = $"##{_idPrefix}_page_{page.Id}";
		}

		return _contentId;
	}

	private string Version
	{
		get
		{
			var source = M3Brand.Version;
			if (source != _versionSource)
			{
				_versionSource = source;
				_version = source.Length == 0 || char.IsLetter(source[0]) ? source : $"v{source}";
			}

			return _version;
		}
	}

	private float DrawerWidth()
	{
		var widest = ImGui.CalcTextSize(_title).X * 0.6f;
		for (var i = 0; i < _visible.Count; i++)
		{
			var page = _visible[i];
			widest = MathF.Max(widest, ImGui.CalcTextSize(page.Label).X + (page.Badge is { Length: > 0 } badge ? ImGui.CalcTextSize(badge).X + (20f * Scale) : 0f));
		}

		return MathF.Max(190f * Scale, widest + (24f * Scale) + M3Style.Spacing(16f, 12f) + M3Style.Spacing(14f, 10f) + (20f * Scale));
	}

	private void DrawNavigation(float width, bool expanded)
	{
		using var child = ImRaii.Child(_navId, new Vector2(width, -1f), false,
			ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
		if (!child)
		{
			return;
		}

		var inner = ImGui.GetContentRegionAvail().X;
		DrawBrand(inner, expanded);
		ImGui.Dummy(new Vector2(0f, M3.Space1));

		if (SupportsSearch)
		{
			if (expanded)
			{
				DrawSearchField(inner);
			}
			else
			{
				var button = M3Widgets.IconButtonSize;
				ImGui.SetCursorPosX(ImGui.GetCursorPosX() + MathF.Max(0f, (inner - button) * 0.5f));
				var open = RailSearchOpen;
				if (M3Widgets.IconToggle(_searchToggleId, FontAwesomeIcon.Search, ref open, open ? "Close search" : SearchHint))
				{
					if (open)
					{
						_railSearch = _focusSearch = true;
					}
					else
					{
						ClearSearch();
					}
				}
			}

			ImGui.Dummy(new Vector2(0f, M3.Space1));
		}

		var footer = NavigationFooterHeight(expanded);
		using (var list = ImRaii.Child(_navListId, new Vector2(-1f, MathF.Max(1f, ImGui.GetContentRegionAvail().Y - footer)), false))
		{
			if (list)
			{
				DrawNavigationItems(expanded);
			}
		}

		if (footer > 0f)
		{
			DrawNavigationFooter(ImGui.GetContentRegionAvail().X, expanded);
		}
	}

	private void DrawNavigationItems(bool expanded)
	{
		var current = CurrentPage;
		var tools = M3.Scheme.Tertiary;
		var searching = Searching;
		_navItems.Clear();
		for (var i = 0; i < _visible.Count; i++)
		{
			var page = _visible[i];
			_navItems.Add(new M3NavItem(page.Id, page.Label, page.Icon, !searching && page == current, page.Tooltip ?? page.Description,
				page.Tool ? tools : null, page.Badge, Section: page.Section));
		}

		if (M3Navigation.Draw(_navItemsId, _navItems, expanded) is { } clicked)
		{
			ShowPage(clicked);
		}
	}

	private void DrawBrand(float width, bool expanded)
	{
		var s = M3.Scheme;
		var size = 40f * Scale;
		var origin = ImGui.GetCursorScreenPos();
		var height = size + (2f * M3.Space1);
		var home = _visible.Count > 0 ? _visible[0] : null;

		var pressed = ImGui.InvisibleButton(_brandId, new Vector2(width, height));
		var hovered = ImGui.IsItemHovered();
		var drawList = ImGui.GetWindowDrawList();
		if (hovered)
		{
			drawList.AddRectFilled(origin, origin + new Vector2(width, height), M3.U32(s.OnSurface, M3.StateHover), M3.ShapeMedium);
			ImGui.SetMouseCursor(ImGuiMouseCursor.Hand);
			M3Tooltip.Show(home?.Label ?? _title);
		}

		M3Draw.FocusRing(origin, origin + new Vector2(width, height), M3.ShapeMedium);
		var tileMin = new Vector2(expanded ? origin.X + M3.Space2 : origin.X + ((width - size) * 0.5f), origin.Y + M3.Space1);
		M3Brand.DrawLogo(drawList, tileMin, size);

		if (expanded)
		{
			var textX = tileMin.X + size + M3.Space2;
			var textWidth = MathF.Max(1f, origin.X + width - textX - M3.Space1);
			float titleHeight, versionHeight;
			string title, version;
			using (ImRaii.PushFont(M3.TitleMedium))
			{
				title = M3Navigation.Truncate(_title, textWidth);
				titleHeight = ImGui.CalcTextSize(title).Y;
			}

			using (ImRaii.PushFont(M3.LabelSmall))
			{
				version = M3Navigation.Truncate(Version, textWidth);
				versionHeight = version.Length > 0 ? ImGui.CalcTextSize(version).Y : 0f;
			}

			var top = tileMin.Y + ((size - titleHeight - versionHeight - (2f * Scale)) * 0.5f);
			using (ImRaii.PushFont(M3.TitleMedium))
			{
				drawList.AddText(new Vector2(textX, top), M3.U32(s.OnSurface), title);
			}

			if (version.Length > 0)
			{
				using var font = ImRaii.PushFont(M3.LabelSmall);
				drawList.AddText(new Vector2(textX, top + titleHeight + (2f * Scale)), M3.U32(s.OnSurfaceVariant, 0.9f), version);
			}
		}

		if (pressed && home != null)
		{
			ShowPage(home.Id);
		}
	}

	private void DrawSearchField(float width)
	{
		if (M3Widgets.SearchField(_searchId, SearchHint, ref _searchText, width, busy: Searching))
		{
			OnSearchChanged(_searchText);
		}
	}

	private void DrawBody(bool expanded)
	{
		using var child = ImRaii.Child(_bodyId, -Vector2.One, false, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
		if (!child)
		{
			return;
		}

		var page = CurrentPage;
		DrawTopAppBar(page);
		if (SupportsSearch && !expanded && RailSearchOpen)
		{
			_railSearch = true;
			if (_focusSearch)
			{
				_focusSearch = false;
				ImGui.SetKeyboardFocusHere();
			}

			DrawSearchField(ImGui.GetContentRegionAvail().X);
			ImGui.Dummy(new Vector2(0f, M3.Space1));
		}

		DrawPageHeader();

		var searching = Searching;
		if (!searching && page == null)
		{
			return;
		}

		var scrolls = searching || page!.Scrolls;
		using var content = ImRaii.Child(searching ? _searchContentId : ContentId(page!), -Vector2.One, false,
			scrolls ? ImGuiWindowFlags.None : ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
		if (!content)
		{
			return;
		}

		try
		{
			if (searching)
			{
				DrawSearchResults(_searchText);
			}
			else
			{
				page!.Draw();
			}
		}
		catch (Exception ex)
		{
			var pageName = searching ? "search results" : page!.Id;
			var error = $"{pageName}: {ex.Message}";
			if (error != _loggedPageError)
			{
				_loggedPageError = error;
				M3Services.Log?.Error(ex, $"[RebornMaterial] Exception while drawing page {pageName}");
			}

			M3Widgets.Banner($"##{_idPrefix}_page_error", $"Something went wrong while drawing this page: {ex.Message}", M3Severity.Error, FontAwesomeIcon.Bug);
		}
	}

	private void DrawTopAppBar(M3Page? page)
	{
		var pageTitle = Searching ? "Search results" : page?.Label ?? _title;
		var subtitle = Searching ? SearchSubtitle(_searchText) : page?.Description;

		var clearSize = M3Widgets.IconButtonSize;
		var trailing = Searching ? clearSize + (4f * Scale) : TopAppBarTrailingWidth;
		using var bar = M3TopAppBar.Begin(pageTitle, subtitle, WindowActions.Length, Brand, trailing, minimumActions: MinimumActions);
		SetActionBar(bar.ShownActions, bar.BarTop);

		if (Searching)
		{
			ImGui.SetCursorScreenPos(bar.TrailingPosition(clearSize));
			if (M3Widgets.IconButton(_clearSearchId, FontAwesomeIcon.Times, "Clear search"))
			{
				ClearSearch();
			}
		}
		else if (trailing > 0f)
		{
			DrawTopAppBarTrailing(bar);
		}
	}
}
