﻿using Microsoft.AspNetCore.Mvc;
using TASVideos.Data.Entity;

namespace TASVideos.RazorPages.ViewComponents
{
	public class SystemPageHeader : ViewComponent
	{
		public IViewComponentResult Invoke(WikiPage pageData)
		{
			return View(pageData);
		}
	}
}
