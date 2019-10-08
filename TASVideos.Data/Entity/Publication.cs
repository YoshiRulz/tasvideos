﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

using TASVideos.Data.Entity.Awards;
using TASVideos.Data.Entity.Game;

namespace TASVideos.Data.Entity
{
	/// <summary>
	/// Represents filter criteria for filtering publications
	/// </summary>
	public interface IPublicationTokens
	{
		IEnumerable<string> SystemCodes { get; }
		IEnumerable<string> Tiers { get; }
		IEnumerable<int> Years { get; }
		IEnumerable<string> Tags { get; }
		IEnumerable<string> Genres { get; }
		IEnumerable<string> Flags { get; }
		IEnumerable<int> Authors { get; }
		IEnumerable<int> MovieIds { get; }
		bool ShowObsoleted { get; set; }
	}

	public class Publication : BaseEntity, ITimeable
	{
		public int Id { get; set; }

		public virtual ICollection<PublicationFile> Files { get; set; } = new List<PublicationFile>();
		public virtual ICollection<PublicationTag> PublicationTags { get; set; } = new List<PublicationTag>();
		public virtual ICollection<PublicationFlag> PublicationFlags { get; set; } = new List<PublicationFlag>();
		public virtual ICollection<PublicationAward> PublicationAwards { get; set; } = new List<PublicationAward>();

		[ForeignKey(nameof(PublicationRating.PublicationId))]
		public virtual ICollection<PublicationRating> PublicationRatings { get; set; } = new List<PublicationRating>();

		public int? ObsoletedById { get; set; }
		public virtual Publication ObsoletedBy { get; set; }

		public int GameId { get; set; }
		public virtual Game.Game Game { get; set; }

		public int SystemId { get; set; }
		public virtual GameSystem System { get; set; }

		public int SystemFrameRateId { get; set; }
		public virtual GameSystemFrameRate SystemFrameRate { get; set; }

		public int RomId { get; set; }
		public virtual GameRom Rom { get; set; }

		public int TierId { get; set; }
		public virtual Tier Tier { get; set; }

		public int SubmissionId { get; set; }
		public virtual Submission Submission { get; set; }
		public virtual ICollection<PublicationAuthor> Authors { get; set; } = new List<PublicationAuthor>();

		public int? WikiContentId { get; set; } // making this non-nullable is a catch-22 when creating a publication, the wiki needs a publication id and the publication needs a wiki id
		public virtual WikiPage WikiContent { get; set; }

		// TODO: we eventually should want to move these to the file server instead
		[Required]
		public byte[] MovieFile { get; set; }

		[Required]
		public string MovieFileName { get; set; }

		[StringLength(50)]
		public string Branch { get; set; }

		[StringLength(50)]
		public string EmulatorVersion { get; set; }

		public string OnlineWatchingUrl { get; set; }
		public string MirrorSiteUrl { get; set; }

		public int Frames { get; set; }
		public int RerecordCount { get; set; }

		/// <summary>
		/// Gets or sets Any author's that are not a user. If they are a user, they should linked, and not listed here
		/// </summary>
		[StringLength(200)]
		public string AdditionalAuthors { get; set; }

		// De-normalized name for easy recreation
		public string Title { get; set; }

		double ITimeable.FrameRate => SystemFrameRate?.FrameRate ?? throw new InvalidOperationException($"{nameof(SystemFrameRate)} must not be lazy loaded!"); 

		public void GenerateTitle()
		{
			var authorList = Authors.Select(sa => sa.Author.UserName);

			if (!string.IsNullOrWhiteSpace(AdditionalAuthors))
			{
				authorList = authorList.Concat(AdditionalAuthors.Split(new [] { "," }, StringSplitOptions.RemoveEmptyEntries));
			}

			if (System == null)
			{
				throw new InvalidOperationException($"{nameof(System)} must not be lazy loaded!");
			}

			if (Game == null)
			{
				throw new InvalidOperationException($"{nameof(Game)} must not be lazy loaded!");
			}

			Title =
				$"{System.Code} {Game.DisplayName}"
				+ (!string.IsNullOrWhiteSpace(Branch) ? $" \"{Branch}\" " : "")
				+ $" by {string.Join(" & ", authorList)}"
				+ $" in {this.Time():g}";
		}
	}

	public static class PublicationExtensions
	{
		public static IQueryable<Publication> ThatAreCurrent(this IQueryable<Publication> publications)
		{
			return publications.Where(p => p.ObsoletedById == null);
		}

		public static IEnumerable<Publication> ThatAreCurrent(this IEnumerable<Publication> publications)
		{
			return publications.Where(p => p.ObsoletedById == null);
		}

		public static IQueryable<Publication> ThatAreObsolete(this IQueryable<Publication> publications)
		{
			return publications.Where(p => p.ObsoletedById != null);
		}

		public static IQueryable<Publication> FilterByTokens(this IQueryable<Publication> publications, IPublicationTokens tokens)
		{
			if (tokens.MovieIds.Any())
			{
				return publications.Where(p => tokens.MovieIds.Contains(p.Id));
			}

			var query = publications;
			if (tokens.SystemCodes.Any())
			{
				query = query.Where(p => tokens.SystemCodes.Contains(p.System.Code));
			}

			if (tokens.Tiers.Any())
			{
				query = query.Where(p => tokens.Tiers.Contains(p.Tier.Name));
			}

			if (!tokens.ShowObsoleted)
			{
				query = query.ThatAreCurrent();
			}

			if (tokens.Years.Any())
			{
				query = query.Where(p => tokens.Years.Contains(p.CreateTimeStamp.Year));
			}

			if (tokens.Tags.Any())
			{
				query = query.Where(p => p.PublicationTags.Any(t => tokens.Tags.Contains(t.Tag.Code)));
			}

			if (tokens.Genres.Any())
			{
				query = query.Where(p => p.Game.GameGenres.Any(gg => tokens.Genres.Contains(gg.Genre.DisplayName)));
			}

			if (tokens.Flags.Any())
			{
				query = query.Where(p => p.PublicationFlags.Any(f => tokens.Flags.Contains(f.Flag.Token)));
			}

			if (tokens.Authors.Any())
			{
				query = query.Where(p => p.Authors.Select(a => a.UserId).Any(a => tokens.Authors.Contains(a)));
			}

			return query;
		}
	}
}
