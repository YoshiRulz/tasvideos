using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc.TagHelpers;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.AspNetCore.Razor.TagHelpers;

namespace TASVideos.TagHelpers;

public sealed class SubmissionMetadataEditTagHelper(IHtmlGenerator generator) : TagHelper
{
	public bool IsEditing { get; init; } = false;

	public ModelExpression? MovieFile { get; init; } = null;

	public required ModelExpression Authors { get; init; }

	public required ModelExpression ExternalAuthors { get; init; }

	public required ModelExpression GameVersion { get; init; }

	public required ModelExpression GameName { get; init; }

	public required ModelExpression Emulator { get; init; }

	public required ModelExpression GoalName { get; init; }

	public required ModelExpression RomName { get; init; }

	public required ModelExpression EncodeEmbedLink { get; init; }

	public override void Process(TagHelperContext context, TagHelperOutput output)
	{
		static IHtmlContent CreateFieldset(params IHtmlContent[] children)
		{
			TagBuilder fieldset = new("fieldset");
			foreach (var child in children)
			{
				fieldset.InnerHtml.AppendHtml(child);
			}

			return fieldset;
		}

		TagBuilder movieFileLongDescElem = new("div");
		movieFileLongDescElem.InnerHtml.Append($"Your movie packed in a ZIP file (max size: {SiteGlobalConstants.MaximumMovieSizeHumanReadable})");

		output.TagName = null;
		output.Content.SetHtmlContent(new RowTagHelper().InvokeProcessWithChildContent(
			new ColumnTagHelper() { Lg = 6 },
			CreateFieldset(
				new LabelTagHelper(generator) { For = MovieFile }.InvokeProcessWithChildContent(),
				new InputTagHelper(generator) { For = MovieFile }.InvokeProcessWithChildContent(),
				movieFileLongDescElem,
				new ValidationMessageTagHelper(generator) { For = MovieFile }.InvokeProcessWithChildContent())
			/*
			if (Model.IsEditing)
			{
				<column lg="6" permission="ReplaceSubmissionMovieFile">
					@{ MovieFileField() }
				</column>
			}
			else
			{
				<column lg="6">
					@{ MovieFileField() }
				</column>
			}
			<column lg="6">
				<fieldset>
					<label asp-for="Authors">Author(s)</label>
					<string-list asp-for="Authors"/>
					<span asp-validation-for="Authors"></span>
				</fieldset>
				<fieldset>
					<label asp-for="ExternalAuthors"></label>
					<input asp-for="ExternalAuthors"/>
					<div>Only authors not registered for TASVideos should be listed here. If multiple authors, separate the names with a comma.</div>
					<span asp-validation-for="ExternalAuthors"></span>
				</fieldset>
			</column>
			*/
		));
		output.Content.SetHtmlContent(new RowTagHelper().InvokeProcessWithChildContent(
			/*
			<column lg="6">
				<fieldset>
					<label asp-for="GameVersion"></label>
					<input asp-for="GameVersion" placeholder="USA v1.0" />
					<span asp-validation-for="GameVersion"></span>
				</fieldset>
				<fieldset>
					<label asp-for="GameName"></label>
					<input asp-for="GameName" placeholder="Example: Mega Man 2" />
					<span asp-validation-for="GameName"></span>
				</fieldset>
				<fieldset>
					<label asp-for="Emulator">Emulator and version</label>
					<input asp-for="Emulator" spellcheck="false" placeholder="Example: BizHawk 2.8.0" />
					@if (Model.IsEditing)
					{
						<div>Needs to be a specific version that sync was verified on. Does not necessarily need to be the version used by the author.</div>
					}
					<span asp-validation-for="Emulator"></span>
				</fieldset>
			</column>
			<column lg="6">
				<fieldset>
					<label asp-for="GoalName"></label>
					<input asp-for="GoalName" placeholder="Example: 100% or princess only; any% can usually be omitted" />
					<span asp-validation-for="GoalName"></span>
				</fieldset>
				<fieldset>
					<label asp-for="RomName">ROM filename</label>
					<input asp-for="RomName" placeholder="Example: Mega Man II (U) [!].nes" />
					<span asp-validation-for="RomName"></span>
				</fieldset>
				<fieldset>
					<label asp-for="EncodeEmbedLink"></label>
					<input asp-for="EncodeEmbedLink" placeholder="https://www.youtube.com/embed/0mregEW6kVU" />
					<div>Embedded link to a video of your movie. Must be YouTube or niconico.</div>
					<span asp-validation-for="EncodeEmbedLink"></span>
				</fieldset>
			</column>
			*/
		));
	}
}
