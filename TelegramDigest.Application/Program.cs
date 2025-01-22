using Microsoft.EntityFrameworkCore;
using TelegramDigest.Application.Database;
using TelegramDigest.Application.Public;
using TelegramDigest.Application.Services;

var builder = WebApplication.CreateBuilder(args);

ConfigureServices(builder.Services, builder.Configuration);

var app = builder.Build();

ConfigureMiddleware(app);

await InitializeDatabaseAsync(app);
await app.RunAsync();

static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
{
    services.AddDbContext<ApplicationDbContext>(options =>
        options.UseSqlite(configuration.GetConnectionString("DefaultConnection"))
    );

    // OpenAI
    // services.AddOpenAIService(settings =>
    //     configuration.GetSection("OpenAI").Bind(settings));

    // Application services
    services.AddScoped<ChannelReader>();
    services.AddScoped<ChannelsService>();
    services.AddScoped<ChannelsRepository>();
    services.AddScoped<DigestsService>();
    services.AddScoped<DigestRepository>();
    services.AddScoped<SummaryGenerator>();
    services.AddScoped<EmailSender>();
    services.AddSingleton<SettingsManager>(sp =>
    {
        var logger = sp.GetRequiredService<ILogger<SettingsManager>>();
        var settingsPath = configuration.GetValue<string>("SettingsPath") ?? "settings.json";
        return new SettingsManager(settingsPath, logger);
    });
    services.AddScoped<MainService>();
    services.AddScoped<PublicFacade>();

    // Background Service
    services.AddHostedService<Scheduler>();

    // API & UI
    services.AddControllers();
    services.AddRazorPages();
    services.AddEndpointsApiExplorer();
    services.AddSwaggerGen();

    // Logging
    services.AddLogging(logging =>
    {
        logging.ClearProviders();
        logging.AddConsole();
        logging.AddDebug();
    });
}

static void ConfigureMiddleware(WebApplication app)
{
    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        app.UseDeveloperExceptionPage();
    }
    else
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseStaticFiles();
    app.UseRouting();
    app.UseAuthorization();

    app.MapRazorPages();
    app.MapControllers();
}

static async Task InitializeDatabaseAsync(WebApplication app)
{
    using var scope = app.Services.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();
}
