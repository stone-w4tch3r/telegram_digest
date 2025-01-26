using TelegramDigest.Application;
using TelegramDigest.Application.Public;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// builder.Services.AddSwaggerGen();

// Add Application services
builder.Services.AddTelegramDigest(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    // app.UseSwagger();
    // app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await app.Services.InitializeTelegramDigest();
await app.RunAsync();
