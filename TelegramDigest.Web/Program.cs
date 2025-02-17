using Microsoft.Extensions.Options;
using TelegramDigest.Backend;
using TelegramDigest.Web.DeploymentOptions;
using TelegramDigest.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Deployment options
builder.Configuration.AddEnvironmentVariables();
builder.AddDeploymentOptions();

// Add services to the container.
builder.Services.AddRazorPages();
builder.Services.AddControllers();
builder.Services.AddTelegramDigest(builder.Configuration);
builder.Services.AddScoped<BackendClient>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}
else
{
    app.UseDeveloperExceptionPage();
}

var deploymentOptions = app.Services.GetRequiredService<IOptions<DeploymentOptions>>().Value;

app.UsePathBase(deploymentOptions.BasePath);
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();
app.MapControllers();

await app.Services.UseTelegramDigest();

app.Run();
