﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TASVideos.Data;
using TASVideos.Data.Constants;
using TASVideos.Data.Entity;
using TASVideos.WikiEngine;

namespace TASVideos.Services
{
	public interface IWikiService : IEnumerable<WikiPage>
	{
		/// <summary>
		/// Returns whether or not any revision of the given page exists
		/// </summary>
		bool Exists(string pageName, bool includeDeleted = false);

		/// <summary>
		/// Returns details about a Wiki page with the given <see cref="pageName" />
		/// If a <see cref="revisionId" /> is provided then that revision of the page will be returned
		/// Else the latest revision is returned
		/// </summary>
		/// <returns>A model representing the Wiki page if it exists else null</returns>
		WikiPage Page(string pageName, int? revisionId = null);

		/// <summary>
		/// Creates a new revision of a wiki page
		/// </summary>
		Task Add(WikiPage revision);

		/// <summary>
		/// Renames the given wiki page to the destination name
		/// All revisions are renamed to the new page
		/// and <seealso cref="WikiPageReferral" /> entries are updated
		/// </summary>
		Task Move(string originalName, string destinationName);

		/// <summary>
		/// Returns details about a Wiki page with the given id
		/// </summary>
		/// <returns>A model representing the Wiki page if it exists else null</returns>
		WikiPage Revision(int dbId);

		/// <summary>
		/// Performs a soft delete on all revisions of the given page name,
		/// In addition <see cref="WikiPageReferral"/> entries are updated
		/// to remove entries where the given page name is a referrer
		/// </summary>
		/// <returns>The number of revisions that were deleted</returns>
		Task<int> Delete(string pageName);

		/// <summary>
		/// Performs a soft delete on a single revision of a <see cref="WikiPage"/>
		/// If the revision is latest revisions, then <see cref="WikiPageReferral"/>
		/// will be removed where the given page name is a referrer
		/// </summary>
		Task Delete(string pageName, int revision);

		
		/// <summary>
		/// Restores all revisions of the given page
		/// </summary>
		Task Undelete(string pageName);

		/// <summary>
		/// Clears the wiki cache
		/// </summary>
		void ClearCache();
	}

	public class WikiService : IWikiService
	{
		private readonly ApplicationDbContext _db;
		private readonly ICacheService _cache;

		private List<WikiPage> WikiCache
		{
			get
			{
				var cacheKey = CacheKeys.WikiCache;
				if (_cache.TryGetValue(cacheKey, out List<WikiPage> pages))
				{
					return pages;
				}

				pages = new List<WikiPage>();
				_cache.Set(cacheKey, pages, Durations.OneWeekInSeconds);
				LoadWikiCache().Wait();
				return pages;
			}
		}

		public WikiService(
			ApplicationDbContext db,
			ICacheService cache)
		{
			_db = db;
			_cache = cache;

			if (!WikiCache.Any())
			{
				LoadWikiCache().Wait();
			}
		}

		public IEnumerator<WikiPage> GetEnumerator()
		{
			return WikiCache.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public bool Exists(string pageName, bool includeDeleted = false)
		{
			var query = includeDeleted
				? WikiCache
				: WikiCache.ThatAreNotDeleted();

			return query
				.Any(wp => wp.PageName == pageName);
		}

		public WikiPage Page(string pageName, int? revisionId = null)
		{
			return WikiCache
				.ForPage(pageName)
				.ThatAreNotDeleted()
				.FirstOrDefault(w => (revisionId != null ? w.Revision == revisionId : w.ChildId == null));
		}

		public WikiPage Revision(int dbId)
		{
			return WikiCache
				.ThatAreNotDeleted()
				.FirstOrDefault(w => w.Id == dbId);
		}

		public async Task Add(WikiPage revision)
		{
			_db.WikiPages.Add(revision);

			var currentRevision = await _db.WikiPages
				.ForPage(revision.PageName)
				.ThatAreCurrentRevisions()
				.SingleOrDefaultAsync();

			if (currentRevision != null)
			{
				currentRevision.Child = revision;
				revision.Revision = currentRevision.Revision + 1;
			}

			await GenerateReferrals(revision.PageName, revision.Markup);
		
			var cachedCurrentRevision = WikiCache
				.ForPage(revision.PageName)
				.ThatAreCurrentRevisions()
				.FirstOrDefault();
			if (cachedCurrentRevision != null)
			{
				cachedCurrentRevision.Child = revision;
				cachedCurrentRevision.ChildId = revision.Id;
			}

			WikiCache.Add(revision);
		}

		public async Task Move(string originalName, string destinationName)
		{
			if (string.IsNullOrWhiteSpace(destinationName))
			{
				throw new ArgumentException($"{destinationName} must have a value.");
			}

			// TODO: support moving a page to a deleted page
			// Revision ids would have to be adjusted but it could be done
			if (Exists(destinationName, includeDeleted: true))
			{
				throw new InvalidOperationException($"Cannot move {originalName} to {destinationName} because {destinationName} already exists.");
			}

			var existingRevisions = await _db.WikiPages
				.ForPage(originalName)
				.ToListAsync();

			foreach (var revision in existingRevisions)
			{
				revision.PageName = destinationName;

				var cachedRevision = WikiCache.FirstOrDefault(w => w.Id == revision.Id);
				if (cachedRevision != null)
				{
					cachedRevision.PageName = destinationName;
				}
			}

			await _db.SaveChangesAsync();

			// Update all Referrals
			// Referrals can be safely updated since the new page still has the original content 
			// and any links on them are still correctly referring to other pages
			var existingReferrals = await _db.WikiReferrals
				.Where(wr => wr.Referral == originalName)
				.ToListAsync();

			foreach (var referral in existingReferrals)
			{
				referral.Referral = destinationName;
			}

			await _db.SaveChangesAsync();

			// Note that we can not update Referrers since the wiki pages will still
			// Physically refer to the original page. Those links are broken and it is
			// Important to keep them listed as broken so they can show up in the Broken Links module
			// for editors to see and fix. Anyone doing a move operation should know to check broken links
			// afterwards
		}

		public async Task<int> Delete(string pageName)
		{
			var revisions = await _db.WikiPages
				.ForPage(pageName)
				.ThatAreNotDeleted()
				.ToListAsync();

			foreach (var revision in revisions)
			{
				revision.IsDeleted = true;
			}

			var cachedRevisions = WikiCache
				.ForPage(pageName)
				.ThatAreNotDeleted()
				.ToList();

			foreach (var cachedRevision in cachedRevisions)
			{
				cachedRevision.IsDeleted = true;
			}

			// Remove referrers
			var referrers = await _db.WikiReferrals
				.ThatReferTo(pageName)
				.ToListAsync();

			_db.RemoveRange(referrers);

			await _db.SaveChangesAsync();
			return revisions.Count;
		}

		public async Task Delete(string pageName, int revision)
		{
			var wikiPage = await _db.WikiPages
				.ThatAreNotDeleted()
				.Revision(pageName, revision)
				.SingleOrDefaultAsync();

			if (wikiPage != null)
			{
				wikiPage.IsDeleted = true;

				var cachedRevision = WikiCache
					.ThatAreNotDeleted()
					.Revision(pageName, revision)
					.SingleOrDefault();

				if (cachedRevision != null)
				{
					cachedRevision.IsDeleted = true;
				}

				// Update referrers if latest revision
				if (wikiPage.Child == null)
				{
					var referrers = await _db.WikiReferrals
						.ThatReferTo(pageName)
						.ToListAsync();

					_db.RemoveRange(referrers);
				}

				await _db.SaveChangesAsync();
			}
		}

		public async Task Undelete(string pageName)
		{
			var revisions = await _db.WikiPages
				.ThatAreDeleted()
				.ForPage(pageName)
				.ToListAsync();

			foreach (var revision in revisions)
			{
				revision.IsDeleted = false;

				var cachedRevision = WikiCache
					.FirstOrDefault(w => w.Id == revision.Id);

				if (cachedRevision != null)
				{
					cachedRevision.IsDeleted = false;
				}
			}

			await _db.SaveChangesAsync();
		}


		public async Task PreLoadCache()
		{
			var wikiPages = await _db.WikiPages
				.ThatAreCurrentRevisions()
				.ToListAsync();

			WikiCache.AddRange(wikiPages);
		}

		public void ClearCache()
		{
			_cache.Remove(CacheKeys.WikiCache);
		}

		// Loads all current wiki pages into the WikiCache
		private async Task LoadWikiCache()
		{
			var wikiPages = await _db.WikiPages
				.ThatAreCurrentRevisions()
				.ToListAsync();

			WikiCache.AddRange(wikiPages);
		}

		private async Task GenerateReferrals(string pageName, string markup)
		{
			var existingReferrals = await _db.WikiReferrals
				.ThatReferTo(pageName)
				.ToListAsync();

			_db.WikiReferrals.RemoveRange(existingReferrals);

			var referrers = Util.GetAllWikiLinks(markup)
				.Select(wl => new WikiPageReferral
				{
					Referrer = pageName,
					Referral = wl.Link,
					Excerpt =  wl.Excerpt
				});

			_db.WikiReferrals.AddRange(referrers);
			await _db.SaveChangesAsync();
		}
	}
}
