using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace TASVideos.Data;

public static class ServiceCollectionExtensions
{
	public static IServiceCollection AddTasvideosData(this IServiceCollection services, bool isDevelopment, string connectionString)
	{
		AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
		return services.AddDbContextPool<ApplicationDbContext>(
			options =>
			{
				options.UseNpgsql(connectionString, b => b.MigrationsAssembly("TASVideos.Data").UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery))
					.UseSnakeCaseNamingConvention();

				if (isDevelopment)
				{
					options.EnableDetailedErrors();
					options.EnableSensitiveDataLogging(); // NEVER do this in production
					options.ConfigureWarnings(warningsAction =>
					{
						warningsAction.Log([
							CoreEventId.FirstWithoutOrderByAndFilterWarning, // .FirstOrDefault() without .OrderBy()
							CoreEventId.RowLimitingOperationWithoutOrderByWarning, // .Take() without .OrderBy()
						]);
					});
				}
			});
	}
}
