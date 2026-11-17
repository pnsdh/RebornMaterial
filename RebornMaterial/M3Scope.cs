namespace RebornMaterial;

internal static class M3Scope
{
	public static M3.FontScope Font(ImFontPtr font, bool condition = true)
	{
		if (!condition)
		{
			return default;
		}

		ImGui.PushFont(font);
		return new M3.FontScope(true);
	}

	public static ColorScope Color(ImGuiCol target, Vector4 value, bool condition = true)
	{
		return default(ColorScope).Push(target, value, condition);
	}

	public static StyleScope Style(ImGuiStyleVar target, float value, bool condition = true)
	{
		return default(StyleScope).Push(target, value, condition);
	}

	public static StyleScope Style(ImGuiStyleVar target, Vector2 value, bool condition = true)
	{
		return default(StyleScope).Push(target, value, condition);
	}

	public static IdScope Id(string id)
	{
		ImGui.PushID(id);
		return new IdScope(true);
	}

	public static DisabledScope Disabled(bool disabled)
	{
		if (!disabled)
		{
			return default;
		}

		ImGui.BeginDisabled();
		return new DisabledScope(true);
	}

	public static TextWrapScope TextWrap(float position)
	{
		ImGui.PushTextWrapPos(position);
		return new TextWrapScope(true);
	}

	public readonly struct ColorScope(int count) : IDisposable
	{
		public ColorScope Push(ImGuiCol target, Vector4 value, bool condition = true)
		{
			if (!condition)
			{
				return this;
			}

			ImGui.PushStyleColor(target, value);
			return new ColorScope(count + 1);
		}

		public void Dispose()
		{
			if (count > 0)
			{
				ImGui.PopStyleColor(count);
			}
		}
	}

	public readonly struct StyleScope(int count) : IDisposable
	{
		public StyleScope Push(ImGuiStyleVar target, float value, bool condition = true)
		{
			if (!condition)
			{
				return this;
			}

			ImGui.PushStyleVar(target, value);
			return new StyleScope(count + 1);
		}

		public StyleScope Push(ImGuiStyleVar target, Vector2 value, bool condition = true)
		{
			if (!condition)
			{
				return this;
			}

			ImGui.PushStyleVar(target, value);
			return new StyleScope(count + 1);
		}

		public void Dispose()
		{
			if (count > 0)
			{
				ImGui.PopStyleVar(count);
			}
		}
	}

	public readonly struct IdScope(bool pushed) : IDisposable
	{
		public void Dispose()
		{
			if (pushed)
			{
				ImGui.PopID();
			}
		}
	}

	public readonly struct DisabledScope(bool active) : IDisposable
	{
		public void Dispose()
		{
			if (active)
			{
				ImGui.EndDisabled();
			}
		}
	}

	public readonly struct TextWrapScope(bool pushed) : IDisposable
	{
		public void Dispose()
		{
			if (pushed)
			{
				ImGui.PopTextWrapPos();
			}
		}
	}
}
