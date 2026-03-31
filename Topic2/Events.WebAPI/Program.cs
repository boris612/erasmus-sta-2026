using System.Reflection;
using AutoMapper;
using Events.WebAPI.CertificateCreator;
using Events.WebAPI.Contract.Services.Certificates;
using Events.WebAPI.Contract.Services.EventRegistrationsExcel;
using Events.WebAPI.ExcelExporter;
using Events.WebAPI;
using Events.WebAPI.Contract.Validation.Sport;
using Events.WebAPI.Contract.Validation;
using Events.WebAPI.Handlers.EF.Mappings;
using Events.WebAPI.Handlers.EF.Models;
using Events.WebAPI.Handlers.EF.QueryHandlers;
using Events.WebAPI.Util.Extensions;
using Events.WebAPI.Util.Startup;
using Events.WebAPI.Util.Swagger;
using Events.WebAPI.Util.Validation;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi;
using Sieve.Models;
using Sieve.Services;

var builder = WebApplication.CreateBuilder(args);
builder.Services
       .AddControllers(options => options.AddJsonPatchSupport())
       .AddJsonOptions(configure => configure.JsonSerializerOptions.PropertyNamingPolicy = null);

builder.Services.AddDbContext<EventsContext>(options =>
       options.UseNpgsql(builder.Configuration.GetConnectionString("EventDB")));

builder.Services.AddOptions<CertificateCreatorPathsOptions>()
  .Bind(builder.Configuration.GetSection("Paths"))
  .ValidateDataAnnotations()
  .Validate(
    settings => !string.IsNullOrWhiteSpace(settings.Certificates),
    "CertificateCreatorPathsOptions:Certificates must be configured.")
  .ValidateOnStart();
builder.Services.AddOptions<ExcelExporterPathsOptions>()
  .Bind(builder.Configuration.GetSection("Paths"))
  .ValidateDataAnnotations()
  .Validate(
    settings => !string.IsNullOrWhiteSpace(settings.Certificates),
    "ExcelExporterPathsOptions:Certificates must be configured.")
  .ValidateOnStart();

builder.Services.Configure<SieveOptions>(builder.Configuration.GetSection("Sieve"));
builder.Services.AddScoped<ISieveProcessor, SieveProcessor>();
builder.Services.AddScoped<IRegistrationCertificateService, RegistrationCertificateService>();
builder.Services.AddScoped<IEventRegistrationsExcelService, EventRegistrationsExcelService>();
builder.Services.AddScoped<IValidationMessageProvider, ValidationMessageProvider>();

builder.Services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));
builder.Services.AddValidatorsFromAssemblyContaining(typeof(AddSportValidator));
builder.Services.AddMediatR(cfg => {
  cfg.RegisterServicesFromAssembly(typeof(SportsQueryHandler).Assembly);
});

builder.Services.SetupMassTransit(builder.Configuration);
builder.Services.SetupAuthenticationAndAuthorization(builder.Configuration);

#region AutoMapper settings
Action<IServiceProvider, IMapperConfigurationExpression> mapperConfigAction = (serviceProvider, cfg) =>
{
  cfg.ConstructServicesUsing(serviceProvider.GetService);
};
builder.Services.AddAutoMapper(mapperConfigAction, typeof(EFMappingProfile)); //assemblies containing mapping profiles            
#endregion

builder.Services.AddSwaggerGen(c =>
{
  c.SwaggerDoc(Constants.ApiVersion, new OpenApiInfo { Title = "Events API", Version = Constants.ApiVersion });
  var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
  var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
  if (File.Exists(xmlPath))
    c.IncludeXmlComments(xmlPath);
  c.AddBearerTokenScheme();
});

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
  c.RoutePrefix = "docs";
  c.DocumentTitle = "Events WebApi";
  c.SwaggerEndpoint($"../swagger/{Constants.ApiVersion}/swagger.json", "Events WebAPI");
});

app.UseRouting();

app.UseCors(builder =>
{
  builder
   .AllowAnyOrigin()
   .AllowAnyMethod()
   .AllowAnyHeader()
   .WithExposedHeaders("Token-Expired", "Content-Disposition");
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.Run();
