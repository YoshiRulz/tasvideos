﻿using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TASVideos.Core.Services;
using TASVideos.Data.Entity;

namespace TASVideos.RazorPages.Pages.Wiki
{
	[AllowAnonymous]
	[IgnoreAntiforgeryToken]
	public class PreviewModel : BasePageModel
	{
		private readonly IWikiPages _pages;

		public PreviewModel(IWikiPages pages)
		{
			_pages = pages;
		}

		public string Markup { get; set; } = "";

		[FromQuery]
		public int? Id { get; set; }

		public WikiPage PageData { get; set; } = new ();

		public async Task<IActionResult> OnPost()
		{
			Markup = await new StreamReader(Request.Body, Encoding.UTF8).ReadToEndAsync();
			if (Id.HasValue)
			{
				PageData = await _pages.Revision(Id.Value);
			}

			return Page();
		}
	}
}
