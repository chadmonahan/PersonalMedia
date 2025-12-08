using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PersonalMedia.Data;
using PersonalMedia.Services;

var builder = FunctionsApplication.CreateBuilder(args);

builder.ConfigureFunctionsWebApplication();

builder.Services
    .AddApplicationInsightsTelemetryWorkerService()
    .ConfigureFunctionsApplicationInsights();

builder.Services.AddDbContext<PersonalMediaDbContext>(options =>
    options.UseSqlServer(builder.Configuration["ConnectionStrings:DefaultConnection"]));

builder.Services.AddHttpClient<IImageGenerationService, ImageGenerationService>();
builder.Services.AddHttpClient<IRunPodImageGenerationService, RunPodImageGenerationService>();
builder.Services.AddScoped<IAzureStorageService, AzureStorageService>();

builder.Build().Run();
