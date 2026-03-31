using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Events.WebAPI.Util.Swagger;

public class AuthorizeOperationFilter : IOperationFilter
{
  public void Apply(OpenApiOperation operation, OperationFilterContext context)
  {
    var hasAuthorize = context.MethodInfo.DeclaringType?
                              .GetCustomAttributes(true)
                              .OfType<AuthorizeAttribute>().Any() == true
                        ||
                       context.MethodInfo.GetCustomAttributes(true)
                              .OfType<AuthorizeAttribute>().Any();

    if (!hasAuthorize)
      return;

    operation.Security ??= new List<OpenApiSecurityRequirement>();

    operation.Security.Add(new OpenApiSecurityRequirement
    {
      [
            new OpenApiSecuritySchemeReference(
                JwtBearerDefaults.AuthenticationScheme,
                context.Document
            )
        ] = new List<string>()
    });
  }
}