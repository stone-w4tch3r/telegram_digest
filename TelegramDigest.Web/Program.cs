using TelegramDigest.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Configure HTTP client for MainService
builder.Services.AddHttpClient<MainServiceClient>(client =>
{
    client.BaseAddress = new(
        builder.Configuration["MainService:BaseUrl"]
            ?? throw new InvalidOperationException("MainService:BaseUrl is not configured")
    );

    // Optional: Add default headers, timeout, etc.
    client.Timeout = TimeSpan.FromSeconds(30);
});

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

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.MapRazorPages();

app.Run();
