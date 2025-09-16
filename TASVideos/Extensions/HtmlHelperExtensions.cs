using Microsoft.AspNetCore.Html;

using TASVideos.WikiEngine;

namespace TASVideos.Extensions;

public static class HtmlHelperExtensions
{
	public static string ToYesNo(this bool val)
	{
		return val ? "Yes" : "No";
	}

	public static async Task<IHtmlContent> RenderWiki(
		this IHtmlHelper html,
		string pageName,
		ParsedAuthorityControlContainer? authControlContainer = null) // can't have `out` param w/ `Task`
	{
		if (authControlContainer is not null)
		{
			html.ViewData[nameof(ParsedAuthorityControlContainer)] = authControlContainer;
		}

		return await html.PartialAsync("_RenderWikiPage", pageName);
	}
}
