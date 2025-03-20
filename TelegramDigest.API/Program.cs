using TelegramDigest.API.Core;
using TelegramDigest.Backend;
using TelegramDigest.Backend.DeploymentOptions;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddScoped<IApplicationFacade, BackendFacade>();

builder.Services.AddControllers();
builder.Services.AddSwaggerGen();
builder.AddBackend();

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

await app.Services.UseBackend();

await app.RunAsync();
