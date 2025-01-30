using TelegramDigest.Application;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.Services.AddTelegramDigest(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseDeveloperExceptionPage();
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseStaticFiles();
    app.MapDefaultControllerRoute();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

await app.Services.InitializeTelegramDigest();
await app.RunAsync();
