﻿/*
// These tests error on a query that the in memory database can not project, but a real database provider can
// Try these out on a newer in memory nuget package and see if the bug is fixed
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TASVideos.Data;
using TASVideos.Data.Entity;
using TASVideos.Services;

namespace TASVideos.Test.Services
{
	[TestClass]
	public class PointsServiceTests
	{
		private IPointsService _pointsService = null!;
		private ApplicationDbContext _db = null!;
		private static User Player => new () { UserName = "Player" };

		[TestInitialize]
		public void Initialize()
		{
			_db = TestDbContext.Create();
			_pointsService = new PointsService(_db, new NoCacheService());
		}

		[TestMethod]
		public async Task PlayerPoints_NoUser_Returns0()
		{
			var actual = await _pointsService.PlayerPoints(int.MinValue);
			Assert.AreEqual(0, actual);
		}

		[TestMethod]
		public async Task PlayerPoints_UserWithNoMovies_Returns0()
		{
			_db.Users.Add(Player);
			await _db.SaveChangesAsync();
			var user = _db.Users.Single();
			var actual = await _pointsService.PlayerPoints(user.Id);
			Assert.AreEqual(0, actual);
		}

		[TestMethod]
		public async Task PlayerPoints_NoRatings_MinimumPointsReturned()
		{
			int numMovies = 2;

			_db.Users.Add(Player);
			var tier = new Tier { Weight = 1, Name = "Test" };
			_db.Tiers.Add(tier);
			for (int i = 0; i < numMovies; i++)
			{
				_db.Publications.Add(new Publication { Tier = tier });
			}

			await _db.SaveChangesAsync();
			var user = _db.Users.Single();
			var pa = _db.Publications
				.ToList()
				.Select(p => new PublicationAuthor
				{
					PublicationId = p.Id,
					UserId = user.Id
				})
				.ToList();

			_db.PublicationAuthors.AddRange(pa);
			await _db.SaveChangesAsync();

			var actual = await _pointsService.PlayerPoints(user.Id);
			int expected = numMovies * PlayerPointConstants.MinimumPlayerPointsForPublication;
			Assert.AreEqual(expected, actual);
		}
	}
}
*/
