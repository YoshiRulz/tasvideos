using System.Reflection;

using static TASVideos.Pages.Profile.SettingsModel;

namespace TASVideos.RazorPages.Tests.Pages.Profile;

[TestClass]
public sealed class SettingsModelTests
{
	private const string EmbedPrefix = "TASVideos.RazorPages.Tests.Pages.Profile.TestFiles.";

	private static readonly Assembly Asm = typeof(SettingsModelTests).Assembly;

	private static Stream ReadEmbeddedFile(string embedPath)
		=> Asm.GetManifestResourceStream(embedPath)!;

	[DataRow($"{EmbedPrefix}124x.gif", true)]
	[DataRow($"{EmbedPrefix}125x.gif", true)]
	[DataRow($"{EmbedPrefix}126x.gif", false)]
	[DataRow($"{EmbedPrefix}124x.jpg", true)]
	[DataRow($"{EmbedPrefix}125x.jpg", true)]
	[DataRow($"{EmbedPrefix}126x.jpg", false)]
	[DataRow($"{EmbedPrefix}124x.png", true)]
	[DataRow($"{EmbedPrefix}125x.png", true)]
	[DataRow($"{EmbedPrefix}126x.png", false)]
#if false // not implemented (will always return `true`)
	[DataRow($"{EmbedPrefix}124x.webp", true)]
	[DataRow($"{EmbedPrefix}125x.webp", true)]
	[DataRow($"{EmbedPrefix}126x.webp", false)]
#endif
	[TestMethod]
	public async Task IsAcceptableImageWithinMaxSize_IdentifiesSizeCorrectly(string embedPath, bool expected)
		=> Assert.AreEqual(
			expected,
			await IsAcceptableImageWithinMaxSize(ReadEmbeddedFile(embedPath)));

	[TestMethod]
	public async Task IsAcceptableImageWithinMaxSize_ThrowsIfMalformed()
	{
		await Assert.ThrowsExceptionAsync<ArgumentException>(
			async () => await IsAcceptableImageWithinMaxSize(ReadEmbeddedFile($"{EmbedPrefix}dummy.bin")),
			"dummy.bin");
		await Assert.ThrowsExceptionAsync<EndOfStreamException>(
			async () => await IsAcceptableImageWithinMaxSize(ReadEmbeddedFile($"{EmbedPrefix}empty.bin")),
			"empty.bin");
		await Assert.ThrowsExceptionAsync<EndOfStreamException>(
			async () => await IsAcceptableImageWithinMaxSize(ReadEmbeddedFile($"{EmbedPrefix}truncated.jpg")),
			"truncated.jpg");
	}
}
