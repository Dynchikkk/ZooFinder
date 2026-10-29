using ZooFinder.Api.ErrorHandling;

namespace ZooFinder.Api.DependencyInjection;

internal static class ErrorHandlingServiceCollectionExtensions
{
    public static IServiceCollection AddZooFinderErrorHandling(this IServiceCollection services)
    {
        // Problem details
        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Instance ??= context.HttpContext.Request.Path;
                context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier;
            };
        });

        // Handler
        services.AddExceptionHandler<ApiExceptionHandler>();

        return services;
    }
}
