﻿#define SHOULD_INCLUDE_STAGING_CSP

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using TASVideos.Core.Settings;
using TASVideos.Middleware;

namespace TASVideos.Extensions;

public static class ApplicationBuilderExtensions
{
	public static IApplicationBuilder UseRobots(this IApplicationBuilder app)
	{
		return app.UseWhen(
			context => context.Request.IsRobotsTxt(),
			appBuilder =>
			{
				appBuilder.UseMiddleware<RobotHandlingMiddleware>();
			});
	}

	public static WebApplication UseExceptionHandlers(this WebApplication app, IHostEnvironment env)
	{
		app.UseExceptionHandler("/Error");
		app.UseStatusCodePagesWithReExecute("/Error");
		return app;
	}

	public static IApplicationBuilder UseGzipCompression(this IApplicationBuilder app, AppSettings settings)
	{
		if (settings.EnableGzipCompression)
		{
			app.UseResponseCompression();
		}

		return app;
	}

	public static IApplicationBuilder UseStaticFilesWithExtensionMapping(this IApplicationBuilder app, IWebHostEnvironment env)
	{
		var provider = new FileExtensionContentTypeProvider();
		provider.Mappings[".avif"] = "image/avif";
		return app.UseStaticFiles(new StaticFileOptions
		{
			ContentTypeProvider = provider,
			ServeUnknownFileTypes = true,
			DefaultContentType = "text/plain"
		});
	}

	public static IApplicationBuilder UseMvcWithOptions(this IApplicationBuilder app, IHostEnvironment env, AppSettings settings)
	{
		var userAgentReportUrl = $"{settings.BaseUrl}/Diagnostics/UserAgentInterventionReports";
		string[] trustedJsHosts = [
			"https://cdn.jsdelivr.net",
			"https://cdnjs.cloudflare.com",
			"https://code.jquery.com",
			"https://www.google.com/recaptcha/",
			"https://www.gstatic.com/recaptcha/",
			"https://www.youtube.com",
		];
		string[] cspDirectives = [
			"base-uri 'none'", // neutralises the `<base/>` footgun
			"default-src 'self'", // fallback for other `*-src` directives
			"font-src 'self' https://cdnjs.cloudflare.com/ajax/libs/font-awesome/", // CSS `font: url();` and `@font-face { src: url(); }` will be blocked unless they're from one of these domains (this also blocks nonstandard fonts installed on the system maybe)
			"form-action 'self'", // domains allowed for `<form action/>` (POST target page)
			"frame-src 'self' https://www.youtube.com/embed/", // allow these domains in <iframe/>
			"img-src *", // allow hotlinking images from any domain in UGC (not great)
			/*
			"report-to for-csp", // browsers using CSP Level 3+ will look up this key in the `Reporting-Endpoints` header and use that URI
			$"report-uri {userAgentReportUrl}?kind=csp&csp-lvl=lteq2", // browsers from before CSP Level 3 will use this
			*/
			"require-trusted-types-for 'script'", // experimental, but Google seems to be pushing it: should block `HTMLScriptElement.innerHTML = "user.pwn();";`, and similarly block adding in-line scripts as attrs
			$"script-src 'self' {string.Join(' ', trustedJsHosts)}", // `<script/>`s will be blocked unless they're from one of these domains
			"style-src 'unsafe-inline' 'self' https://cdnjs.cloudflare.com/ajax/libs/font-awesome/", // allow `<style/>`, and `<link rel="stylesheet"/>` if it's from our domain or trusted CDN
			"upgrade-insecure-requests", // browser should automagically replace links to any `http://tasvideos.org/...` URL (in UGC, for example) with HTTPS
		];
		var contentSecurityPolicyValue = string.Join("; ", cspDirectives);
#if SHOULD_INCLUDE_STAGING_CSP
		var contentSecurityPolicyStagingValue = string.Join("; ", [
			"report-to for-csp-staging", // browsers using CSP Level 3+ will look up this key in the `Reporting-Endpoints` header and use that URI
			$"report-uri {userAgentReportUrl}?kind=csp-staging&csp-lvl=lteq2", // browsers from before CSP Level 3 will use this
			"object-src 'none'", // new directive for testing (should probably be changed to this anyway, currently it falls back to `default-src`)
			..cspDirectives, // at end because, in the case of a duplicate (like `report-to`), the first is used
		]);
#else
		var contentSecurityPolicyStagingValue = contentSecurityPolicyValue;
#endif
		var permissionsPolicyValue = string.Join(", ", [
			"camera=()", // defaults to `self`
			"display-capture=()", // defaults to `self`
			"fullscreen=()", // defaults to `self`
			"geolocation=()", // defaults to `self`
			"microphone=()", // defaults to `self`
			"publickey-credentials-get=()", // defaults to `self`
			"screen-wake-lock=()", // defaults to `self`
			"web-share=()", // defaults to `self`

			// ...and that's all the non-experimental options listed on MDN as of 2024-04
		]);
		var reportingEndpointsValue = string.Join(", ", [
			$"for-csp=\"{userAgentReportUrl}?kind=csp&csp-lvl=gteq3\"",
			$"for-csp-staging=\"{userAgentReportUrl}?kind=csp-staging&csp-lvl=gteq3\"",
		]);
		app.Use(async (context, next) =>
		{
#if SHOULD_INCLUDE_STAGING_CSP
			context.Response.Headers.ContentSecurityPolicyReportOnly = contentSecurityPolicyStagingValue;
#endif
			context.Response.Headers["Cross-Origin-Embedder-Policy"] = "unsafe-none"; // this is as unsecure as before, but can't use `credentialless`, due to breaking YouTube Embeds, see https://github.com/TASVideos/tasvideos/issues/1852
			context.Response.Headers["Cross-Origin-Opener-Policy"] = "same-origin";
			context.Response.Headers["Cross-Origin-Resource-Policy"] = "cross-origin"; // TODO this is as unsecure as before; should be `same-site` or `same-origin` when serving auth-gated responses
			context.Response.Headers["Permissions-Policy"] = permissionsPolicyValue;
			context.Response.Headers["Reporting-Endpoints"] = reportingEndpointsValue; // used with `report-to` CSP directive
			context.Response.Headers.XXSSProtection = "1; mode=block";
			context.Response.Headers.XFrameOptions = "DENY";
			context.Response.Headers.XContentTypeOptions = "nosniff";
			context.Response.Headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
			context.Response.Headers.XPoweredBy = "";
			context.Response.Headers.ContentSecurityPolicy = contentSecurityPolicyValue;
			await next();
		});

		app.UseCookiePolicy(new CookiePolicyOptions
		{
			Secure = CookieSecurePolicy.Always
		});

		app
			.UseRouting()
			.UseAuthorization();

		if (!env.IsProduction() && !env.IsStaging())
		{
			app.UseHsts();
		}

		return app.UseEndpoints(endpoints =>
		{
			endpoints.MapRazorPages();
		});
	}
}
