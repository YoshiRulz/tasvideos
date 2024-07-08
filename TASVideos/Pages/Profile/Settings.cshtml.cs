using TASVideos.Core.Services.Email;

namespace TASVideos.Pages.Profile;

[Authorize]
public class SettingsModel(
	UserManager userManager,
	IEmailService emailService,
	ApplicationDbContext db,
	IHttpClientFactory httpClientFactory) : BasePageModel
{
	public static readonly List<SelectListItem> AvailablePronouns = Enum
		.GetValues<PreferredPronounTypes>()
		.ToDropDown();

	public static readonly List<SelectListItem> AvailableUserPreferenceTypes = Enum
		.GetValues<UserPreference>()
		.ToDropDown();

	public static readonly List<SelectListItem> AvailableDateFormats = Enum
		.GetValues<UserDateFormat>()
		.ToDropDown();

	public static readonly List<SelectListItem> AvailableTimeFormats = Enum
		.GetValues<UserTimeFormat>()
		.ToDropDown();

	public static readonly List<SelectListItem> AvailableDecimalFormats = Enum
		.GetValues<UserDecimalFormat>()
		.ToDropDown();

	public string Username { get; set; } = "";
	public string CurrentEmail { get; set; } = "";
	public bool IsEmailConfirmed { get; set; }

	[BindProperty]
	public string? TimeZone { get; set; } = TimeZoneInfo.Utc.Id;

	[BindProperty]
	public bool PublicRatings { get; set; }

	[BindProperty]
	[StringLength(100)]
	public string? Location { get; set; }

	[BindProperty]
	[StringLength(1000)]
	public string? Signature { get; set; }

	[BindProperty]
	[Url]
	public string? AvatarUrl { get; set; }

	[BindProperty]
	[Url]
	public string? MoodAvatar { get; set; }

	[BindProperty]
	public PreferredPronounTypes PreferredPronouns { get; set; }

	[BindProperty]
	public bool EmailOnPrivateMessage { get; set; }

	[BindProperty]
	public UserPreference AutoWatchTopic { get; set; }

	[BindProperty]
	public UserDateFormat DateFormat { get; set; }

	[BindProperty]
	public UserTimeFormat TimeFormat { get; set; }

	[BindProperty]
	public UserDecimalFormat DecimalFormat { get; set; }

	public async Task OnGet()
	{
		var user = await userManager.GetRequiredUser(User);
		Username = user.UserName;
		CurrentEmail = user.Email;
		IsEmailConfirmed = user.EmailConfirmed;
		TimeZone = user.TimeZoneId;
		PublicRatings = user.PublicRatings;
		Location = user.From;
		Signature = user.Signature;
		AvatarUrl = user.Avatar;
		MoodAvatar = user.MoodAvatarUrlBase;
		PreferredPronouns = user.PreferredPronouns;
		EmailOnPrivateMessage = user.EmailOnPrivateMessage;
		AutoWatchTopic = user.AutoWatchTopic ?? UserPreference.Auto;
		DateFormat = user.DateFormat;
		TimeFormat = user.TimeFormat;
		DecimalFormat = user.DecimalFormat;
	}

	private const byte AvatarSideLengthLimit = 125;

	/// <summary>Does magic byte checks for PNG, JPEG (JFIF), GIF, and WebP, and attempts to extract the width and height. Does no further parsing.</summary>
	/// <returns><see langword="true"/> iff within <see cref="AvatarSideLengthLimit"/>x<see cref="AvatarSideLengthLimit"/></returns>
	/// <exception cref="ArgumentException">if magic bytes don't match any known, or if reached EOF unexpectedly</exception>
	public static async Task<bool> IsAcceptableImageWithinMaxSize(Stream stream)
	{
		const string ErrMsgInvalid = "not an acceptable image";
		var buf = new byte[16]; // can't use `Span` in `async` methods apparently
		await stream.ReadExactlyAsync(buf);
		switch (buf)
		{
			case [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D, 0x49, 0x48, 0x44, 0x52]:
				// PNG
				await stream.ReadExactlyAsync(buf, offset: 0, count: 8);
				return buf is [
					0, 0, 0, <= AvatarSideLengthLimit, // width
					0, 0, 0, <= AvatarSideLengthLimit, // height
					..];
			case [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46, 0x49, 0x46, 0x00, ..]:
				// JPEG (JFIF)
#pragma warning disable SA1503
				// TODO lazily enumerate the stream
				var buf1 = new byte[Math.Clamp(stream.Length - stream.Position, 0, 0x800)];
				await stream.ReadExactlyAsync(buf1);
				var iter = buf1.AsEnumerable().GetEnumerator();
				while (iter.MoveNext())
				{
					if (iter.Current is not 0xFF) continue;
					if (!iter.MoveNext()) break;
					if (iter.Current is not (0xC0 or 0xC1 or 0xC2 or 0xC9 or 0xCA)) continue; // there are other start-of-frame markers, but these are the 5 supported by libjpeg
					if (!(iter.MoveNext() && iter.MoveNext() && iter.MoveNext() && iter.MoveNext())) break;
					if (iter.Current is not 0) return false; // height >= 256
					if (!iter.MoveNext()) break;
					if (iter.Current > AvatarSideLengthLimit) return false; // height too large
					if (!iter.MoveNext()) break;
					if (iter.Current is not 0) return false; // width >= 256
					if (!iter.MoveNext()) break;
					if (iter.Current > AvatarSideLengthLimit) return false; // width too large
					return true;
				}
#pragma warning restore SA1503

				// reached EOF
				throw new EndOfStreamException(ErrMsgInvalid);
			case [0x47, 0x49, 0x46, 0x38, 0x37 or 0x39, 0x61, ..]:
				// GIF
				return buf is [
					_, _, _, _, _, _, // magic bytes
					<= AvatarSideLengthLimit, 0, // width
					<= AvatarSideLengthLimit, 0, // height
					..];
			case [0x52, 0x49, 0x46, 0x46, _, _, _, _, 0x57, 0x45, 0x42, 0x50, ..]:
				// WebP
				return true; // TODO
		}

		throw new ArgumentException(paramName: nameof(stream), message: ErrMsgInvalid);
	}

	public async Task<IActionResult> OnPost()
	{
		HttpClient? httpClient = null;
		async Task<bool> ResolvesToAcceptableImageWithinMaxSize(string url)
		{
			httpClient ??= httpClientFactory.CreateClient();
			var res = await httpClient.SendAsync(new(HttpMethod.Get, url)); // TODO set Accept header?
			res.EnsureSuccessStatusCode();
			using var stream = await res.Content.ReadAsStreamAsync();
			return await IsAcceptableImageWithinMaxSize(stream);
		}

		if (!ModelState.IsValid)
		{
			return Page();
		}

		var site = userManager.AvatarSiteIsBanned(AvatarUrl);
		if (!string.IsNullOrEmpty(site))
		{
			ModelState.AddModelError($"{nameof(AvatarUrl)}", $"Using {site} to host avatars is not allowed.");
		}
		else if (AvatarUrl is not null)
		{
			var isTooLarge = false;
			try
			{
				isTooLarge = !await ResolvesToAcceptableImageWithinMaxSize(AvatarUrl);
			}
			catch (Exception)
			{
				ModelState.AddModelError(nameof(AvatarUrl), "This URL doesn't seem to resolve to one of the accepted image types.");
			}

			if (isTooLarge)
			{
				ModelState.AddModelError(nameof(AvatarUrl), $"This URL resolves to an image which is larger than {AvatarSideLengthLimit}x{AvatarSideLengthLimit}.");
			}
		}

		site = userManager.AvatarSiteIsBanned(MoodAvatar);
		if (!string.IsNullOrEmpty(site))
		{
			ModelState.AddModelError($"{nameof(AvatarUrl)}", $"Using {site} to host avatars is not allowed.");
		}
		else
		{
			// off-topic, the above AddModelError call has a typo in the first param
			// TODO could loop through every mood and check those URIs too...
		}

		if (!ModelState.IsValid)
		{
			return Page();
		}

		var user = await userManager.GetRequiredUser(User);

		bool hasUserCustomLocaleChanged = user.DateFormat != DateFormat || user.TimeFormat != TimeFormat || user.DecimalFormat != DecimalFormat;

		user.TimeZoneId = TimeZone ?? TimeZoneInfo.Utc.Id;
		user.PublicRatings = PublicRatings;
		user.From = Location;
		user.Avatar = AvatarUrl;
		user.MoodAvatarUrlBase = User.Has(PermissionTo.UseMoodAvatars) ? MoodAvatar : null;
		user.PreferredPronouns = PreferredPronouns;
		user.EmailOnPrivateMessage = EmailOnPrivateMessage;
		user.AutoWatchTopic = AutoWatchTopic;
		user.DateFormat = DateFormat;
		user.TimeFormat = TimeFormat;
		user.DecimalFormat = DecimalFormat;
		if (User.Has(PermissionTo.EditSignature))
		{
			user.Signature = Signature;
		}

		await db.SaveChangesAsync();

		if (hasUserCustomLocaleChanged)
		{
			userManager.ClearCustomLocaleCache(User.GetUserId());
		}

		SuccessStatusMessage("Your profile has been updated");
		return BasePageRedirect("Settings");
	}

	public async Task<IActionResult> OnPostSendVerificationEmail()
	{
		if (!ModelState.IsValid)
		{
			return Page();
		}

		var user = await userManager.GetRequiredUser(User);

		var code = await userManager.GenerateEmailConfirmationTokenAsync(user);
		var callbackUrl = Url.EmailConfirmationLink(user.Id.ToString(), code);
		await emailService.EmailConfirmation(user.Email, callbackUrl);

		SuccessStatusMessage("Verification email sent. Please check your email.");
		return BasePageRedirect("Settings");
	}
}
