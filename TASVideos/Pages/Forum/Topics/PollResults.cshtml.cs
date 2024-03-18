﻿using Microsoft.AspNetCore.Mvc;
using TASVideos.Data;
using TASVideos.Data.Entity;
using TASVideos.Pages.Forum.Topics.Models;

namespace TASVideos.Pages.Forum.Topics;

[RequirePermission(PermissionTo.SeePollResults)]
public class PollResultsModel(ApplicationDbContext db) : BasePageModel
{
	[FromRoute]
	public int Id { get; set; }

	public PollResultModel Poll { get; set; } = new();

	public async Task<IActionResult> OnGet()
	{
		var poll = await db.ForumPolls
			.Where(p => p.Id == Id)
			.Select(p => new PollResultModel
			{
				TopicTitle = p.Topic!.Title,
				TopicId = p.TopicId,
				Question = p.Question,
				Votes = p.PollOptions
					.SelectMany(po => po.Votes)
					.Select(v => new PollResultModel.VoteResult
					{
						UserId = v.UserId,
						UserName = v.User!.UserName,
						Ordinal = v.PollOption!.Ordinal,
						OptionText = v.PollOption.Text,
						CreateTimestamp = v.CreateTimestamp,
						IpAddress = v.IpAddress
					})
					.ToList()
			})
			.SingleOrDefaultAsync();

		if (poll is null)
		{
			return NotFound();
		}

		Poll = poll;
		return Page();
	}

	public async Task<IActionResult> OnPostResetVote(int userId)
	{
		if (!User.Has(PermissionTo.ResetPollResults))
		{
			return AccessDenied();
		}

		var poll = await db.ForumPolls
			.Include(p => p.PollOptions)
			.ThenInclude(o => o.Votes)
			.Where(p => p.Id == Id)
			.SingleOrDefaultAsync();

		if (poll is null)
		{
			return NotFound();
		}

		var votes = poll.PollOptions
			.SelectMany(o => o.Votes)
			.Where(v => v.UserId == userId)
			.ToList();

		db.ForumPollOptionVotes.RemoveRange(votes);

		await ConcurrentSave(db, "Poll reset", "Unable to reset poll");
		return RedirectToPage("PollResults", new { Id });
	}
}
