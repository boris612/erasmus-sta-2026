using Events.EFModel.Models;
using Events.MVC.Models;
using Events.MVC.Util.Middleware;
using Microsoft.EntityFrameworkCore;
using Sieve.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews(options =>
    options.Filters.Add<ProblemDetailsForSqlException>());
builder.Services.AddDbContext<EventsContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("EventDB")));
builder.Services.AddScoped<ISieveProcessor, SieveProcessor>();
builder.Services.Configure<PagingSettings>(builder.Configuration.GetSection(PagingSettings.SectionName));

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.MapDefaultControllerRoute();

app.Run();

public partial class Program;
