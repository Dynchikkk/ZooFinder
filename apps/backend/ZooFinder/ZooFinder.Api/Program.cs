using ZooFinder.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseDefaultServiceProvider(options =>
{
    options.ValidateScopes = true;
    options.ValidateOnBuild = true;
});

builder.Services.AddControllers();
builder.Services.AddOpenApi();
builder.Services.AddZooFinderErrorHandling();
builder.Services.AddZooFinderDependencies(builder.Configuration);

var app = builder.Build();
app.ValidateZooFinderConfiguration();

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();
app.Run();
