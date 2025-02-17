using TelegramDigest.API.Core;
using TelegramDigest.Backend;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IApplicationFacade, BackendFacade>();

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
}

app.UseHttpsRedirection();
app.MapControllers();

await app.Services.UseTelegramDigest();

await app.RunAsync();
