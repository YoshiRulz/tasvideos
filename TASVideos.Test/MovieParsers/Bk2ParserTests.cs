﻿using System.Linq;

using Microsoft.VisualStudio.TestTools.UnitTesting;

using TASVideos.MovieParsers;
using TASVideos.MovieParsers.Parsers;
using TASVideos.MovieParsers.Result;

// ReSharper disable InconsistentNaming
namespace TASVideos.Test.MovieParsers
{
	[TestClass]
	[TestCategory("BK2Parsers")]
	public class Bk2ParserTests : BaseParserTests
	{
		private Bk2 _bk2Parser = null!;
		public override string ResourcesPath { get; } = "TASVideos.Test.MovieParsers.Bk2SampleFiles.";

		[TestInitialize]
		public void Initialize()
		{
			_bk2Parser = new Bk2();
		}

		[TestMethod]
		[DataRow("MissingHeader.bk2", DisplayName = "Missing Header creates error")]
		[DataRow("MissingInputLog.bk2", DisplayName = "Missing InputLog creates error")]
		public void Errors(string filename)
		{
			var result = _bk2Parser.Parse(Embedded(filename));
			Assert.AreEqual(false, result.Success);
			AssertNoWarnings(result);
			Assert.IsNotNull(result.Errors);
			Assert.AreEqual(1, result.Errors.Count());
		}

		[TestMethod]
		public void Frames_CorrectResult()
		{
			var result = _bk2Parser.Parse(Embedded("2Frames.bk2"));
			Assert.AreEqual(true, result.Success);
			Assert.AreEqual(2, result.Frames);
			AssertNoWarningsOrErrors(result);
		}

		[TestMethod]
		public void Frames_NoInputFrames_Returns0()
		{
			var result = _bk2Parser.Parse(Embedded("0Frames.bk2"));
			Assert.AreEqual(true, result.Success);
			Assert.AreEqual(0, result.Frames);
			AssertNoWarningsOrErrors(result);
		}

		[TestMethod]
		public void ValidRerecordCount()
		{
			var result = _bk2Parser.Parse(Embedded("RerecordCount1.bk2"));
			Assert.IsTrue(result.Success);
			Assert.AreEqual(1, result.RerecordCount);
			AssertNoWarningsOrErrors(result);
		}

		[TestMethod]
		public void InvalidRerecordCount_Warning()
		{
			var result = _bk2Parser.Parse(Embedded("RerecordCountMissing.bk2"));
			Assert.IsTrue(result.Success);
			Assert.IsNotNull(result.Warnings);
			Assert.AreEqual(1, result.Warnings.Count());
			Assert.AreEqual(0, result.RerecordCount, "Rerecord count is assumed to be 0");
			AssertNoErrors(result);
		}

		[TestMethod]
		[DataRow("Pal1.bk2", RegionType.Pal)]
		[DataRow("0Frames.bk2", RegionType.Ntsc, DisplayName = "Missing flag defaults to Ntsc")]
		public void PalFlag_True(string fileName, RegionType expected)
		{
			var result = _bk2Parser.Parse(Embedded(fileName));
			Assert.AreEqual(expected, result.Region);
			AssertNoWarningsOrErrors(result);
		}

		[TestMethod]
		[DataRow("System-A2600.bk2", SystemCodes.Atari2600)]
		[DataRow("System-A7800.bk2", SystemCodes.Atari7800)]
		[DataRow("System-AppleII.bk2", SystemCodes.AppleII)]
		[DataRow("System-C64.bk2", SystemCodes.C64)]
		[DataRow("System-Intellivision.bk2", SystemCodes.Intellivision)]
		[DataRow("System-Lynx.bk2", SystemCodes.Lynx)]
		[DataRow("System-Nes.bk2", SystemCodes.Nes)]
		[DataRow("System-Fds.bk2",  SystemCodes.Fds)]
		[DataRow("System-Gb.bk2", SystemCodes.GameBoy)]
		[DataRow("System-Dgb.bk2", SystemCodes.GameBoy)]
		[DataRow("System-Sgb.bk2", SystemCodes.Sgb)]
		[DataRow("System-Gba.bk2", SystemCodes.Gba)]
		[DataRow("System-Gbc.bk2", SystemCodes.Gbc)]
		[DataRow("System-Genesis.bk2", SystemCodes.Genesis)]
		[DataRow("System-Ngb.bk2", SystemCodes.Ngb)]
		[DataRow("System-SegaCd.bk2", SystemCodes.SegaCd)]
		[DataRow("System-32x.bk2", SystemCodes.X32)]
		[DataRow("System-Sms.bk2", SystemCodes.Sms)]
		[DataRow("System-Gg.bk2", SystemCodes.Gg)]
		[DataRow("System-Sg.bk2", SystemCodes.Sg)]
		[DataRow("System-Pce.bk2", SystemCodes.Pce)]
		[DataRow("System-PceCd.bk2", SystemCodes.PceCd)]
		[DataRow("System-Pcfx.bk2", SystemCodes.Pcfx)]
		[DataRow("System-Sgx.bk2", SystemCodes.Sgx)]
		[DataRow("System-Snes.bk2", SystemCodes.Snes)]
		[DataRow("System-Saturn.bk2", SystemCodes.Saturn)]
		[DataRow("System-Uze.bk2", SystemCodes.UzeBox)]
		[DataRow("System-Vb.bk2", SystemCodes.VirtualBoy)]
		[DataRow("System-Wswan.bk2", SystemCodes.WSwan)]
		[DataRow("System-Zxs.bk2", SystemCodes.ZxSpectrum)]
		public void Systems(string filename, string expectedSystem)
		{
			var result = _bk2Parser.Parse(Embedded(filename));
			Assert.IsTrue(result.Success);
			Assert.AreEqual(expectedSystem, result.SystemCode);
			AssertNoWarningsOrErrors(result);
		}

		[TestMethod]
		[DataRow("System-Nes.bk2", MovieStartType.PowerOn)]
		[DataRow("sram.bk2", MovieStartType.Sram)]
		[DataRow("savestate.bk2", MovieStartType.Savestate)]
		public void StartType(string filename, MovieStartType expected)
		{
			var result = _bk2Parser.Parse(Embedded(filename));
			Assert.IsTrue(result.Success);
			Assert.AreEqual(expected, result.StartType);
		}

		[TestMethod]
		public void InnerFileExtensions_AreNotChecked()
		{
			var result = _bk2Parser.Parse(Embedded("NoFileExts.bk2"));
			Assert.IsTrue(result.Success);
			Assert.AreEqual("nes", result.SystemCode);
			Assert.AreEqual(1, result.Frames);
			AssertNoWarningsOrErrors(result);
		}

		[TestMethod]
		public void SubNes_ReportsCorrectFrameCount()
		{
			var result = _bk2Parser.Parse(Embedded("SubNes.bk2"));
			Assert.IsTrue(result.Success);
			Assert.AreEqual("nes", result.SystemCode);
			Assert.AreEqual(12, result.Frames);
			AssertNoWarningsOrErrors(result);
		}

		[TestMethod]
		public void SubNes_MissingVBlank_Error()
		{
			var result = _bk2Parser.Parse(Embedded("SubNesMissingVBlank.bk2"));

			Assert.IsFalse(result.Success);
			Assert.IsNotNull(result.Errors);
			Assert.IsTrue(result.Errors.Any());
		}
	}
}
