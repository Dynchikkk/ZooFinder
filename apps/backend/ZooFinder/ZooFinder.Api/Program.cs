using ZooFinder.Api.Configuration;
using ZooFinder.Api.DependencyInjection;
using ZooFinder.Api.Logging;
using ZooFinder.Api.Realtime.Discussions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddZooFinderApi(builder.Configuration);

var app = builder.Build();
StartupConfigurationValidator.Validate(app.Services);

app.UseMiddleware<RequestLoggingMiddleware>();
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<DiscussionHub>(DiscussionHub.Route);
app.Run();
