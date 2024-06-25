﻿namespace TASVideos.Core.Tests.Services;

[TestClass]
public class PointsCalculatorTests
{
	private const double AverageRatingsPerMovie = 13.866;

	[TestMethod]
	public void PlayerPoints_NoPublications()
	{
		var actual = PointsCalculator.PlayerPoints([], 1);
		Assert.AreEqual(0, actual);
	}

	[TestMethod]
	public void PlayerPoints_PublicationMinimum_IfNotObsolete()
	{
		var publications = new[]
		{
			new PointsCalculator.Publication(false, 0, 1, 0, 0)
		};

		var expected = publications.Length * PlayerPointConstants.MinimumPlayerPointsForPublication;
		var actual = PointsCalculator.PlayerPoints(publications, AverageRatingsPerMovie);
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void PlayerPoints_BasicTest()
	{
		var publications = new[]
		{
			new PointsCalculator.Publication(false, 6, 1, 0.75, 4.45166667)
		};

		const double roundedExpected = 18.3;
		var actual = PointsCalculator.PlayerPoints(publications, AverageRatingsPerMovie);
		var roundedActual = Math.Round(actual, 1); // Close enough
		Assert.AreEqual(roundedExpected, roundedActual);
	}

	[TestMethod]
	public void PlayerPoints_NegativeRating()
	{
		var publications = new[]
		{
			new PointsCalculator.Publication(false, 6, 1, 0.75, -100)
		};

		const int expected = PlayerPointConstants.MinimumPlayerPointsForPublication;
		var actual = PointsCalculator.PlayerPoints(publications, AverageRatingsPerMovie);
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	public void PlayerPoints_MultipleAuthors_ReducesPoints()
	{
		var publications = new[]
		{
			new PointsCalculator.Publication(false, 5, 2, 1, 5)
		};

		var actual = PointsCalculator.PlayerPoints(publications, AverageRatingsPerMovie);

		Assert.AreEqual(19.016, actual, 0.001);
	}

	[TestMethod]
	public void PlayerPointsForMovie_WithWeight_UsesWeight()
	{
		const double weightMultiplier = 2.0;
		var publicationWithoutWeight = new PointsCalculator.Publication(false, 1, 1, 1, 5.0);
		var withoutWeight = PointsCalculator.PlayerPointsForMovie(publicationWithoutWeight, 1);

		var publication = new PointsCalculator.Publication(false, 1, 1, weightMultiplier, 5.0);
		var actual = PointsCalculator.PlayerPointsForMovie(publication, 1);
		Assert.AreEqual(withoutWeight * weightMultiplier, actual, 0.001);
	}

	[TestMethod]
	[DataRow(-1, "")]
	[DataRow(0, "")]
	[DataRow(0.0001, PlayerRanks.FormerPlayer)]
	[DataRow(0.9, PlayerRanks.FormerPlayer)]
	[DataRow(1, PlayerRanks.Player)]
	[DataRow(249, PlayerRanks.Player)]
	[DataRow(250, PlayerRanks.ActivePlayer)]
	[DataRow(499, PlayerRanks.ActivePlayer)]
	[DataRow(500, PlayerRanks.ExperiencedPlayer)]
	[DataRow(999, PlayerRanks.ExperiencedPlayer)]
	[DataRow(1000, PlayerRanks.SkilledPlayer)]
	[DataRow(1999, PlayerRanks.SkilledPlayer)]
	[DataRow(2000, PlayerRanks.ExpertPlayer)]
	[DataRow(int.MaxValue, PlayerRanks.ExpertPlayer)]
	public void PlayerRank(double points, string expected)
	{
		var actual = PointsCalculator.PlayerRank((decimal)points);
		Assert.AreEqual(expected, actual);
	}

	[TestMethod]
	[DataRow(0, 0, 0)]
	[DataRow(0, 1, 0)]
	[DataRow(1, 0, 2.6)]
	[DataRow(1, 5, 1.6)]
	[DataRow(1, 10, 1)]
	[DataRow(1, 20, 1)]
	[DataRow(1, 100, 1)]
	[DataRow(2, 10, 1.6)]
	[DataRow(4, 10, 2.1)]
	[DataRow(8, 10, 2.35)]
	[DataRow(10, 10, 2.4)]
	[DataRow(16, 10, 2.475)]
	[DataRow(20, 10, 2.5)]
	[DataRow(100, 10, 2.58)]
	public void RatingExponent(int total, double averageRatingsPerMovie, double expected)
	{
		var actual = PointsCalculator.RatingExponent(total, averageRatingsPerMovie);
		Assert.AreEqual(expected, actual);
	}
}
