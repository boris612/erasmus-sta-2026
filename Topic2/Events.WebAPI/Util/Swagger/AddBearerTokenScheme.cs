using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;


namespace Events.WebAPI.Util.Swagger;

public static class AddBearerTokenSchemeExtension
{
  public static void AddBearerTokenScheme(this SwaggerGenOptions opt)
  {
    var jwtSecurityScheme = new OpenApiSecurityScheme
    {
      Description = "Paste token here",
      Name = "Authorization",
      In = ParameterLocation.Header,
      Type = SecuritySchemeType.Http,
      Scheme = JwtBearerDefaults.AuthenticationScheme,
      BearerFormat = "JWT",
    };

    //Dodaj Authorize button u Swagger UI
    opt.AddSecurityDefinition(jwtSecurityScheme.Scheme, jwtSecurityScheme);

    //opt.AddSecurityRequirement(document => new() { [new OpenApiSecuritySchemeReference(jwtSecurityScheme.Scheme, document)] = [] });

    // nemoj ga primijeniti na sve operacije (redak iznad), nego samo na one koje imaju Authorize atribut
    opt.OperationFilter<AuthorizeOperationFilter>();
  }
}