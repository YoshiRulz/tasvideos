﻿using TASVideos.WikiEngine;

namespace TASVideos.WikiModules.TODO;

[WikiModule(ModuleNames.NoGameVersion)]
public class NoGameVersion(ApplicationDbContext db) : ViewComponent
{
	public async Task<IViewComponentResult> InvokeAsync()
	{
		var model = new NoGame.MissingModel
		{
			Publications = await db.Publications
				.Where(p => p.GameVersionId == -1)
				.OrderBy(p => p.Id)
				.Select(p => new NoGame.MissingModel.Entry(p.Id, p.Title))
				.ToListAsync(),
			Submissions = await db.Submissions
				.Where(s => s.GameVersionId == null || s.GameVersionId < 1)
				.ThatAreInActive()
				.OrderBy(p => p.Id)
				.Select(s => new NoGame.MissingModel.Entry(s.Id, s.Title))
				.ToListAsync()
		};
		return View(model);
	}
}
