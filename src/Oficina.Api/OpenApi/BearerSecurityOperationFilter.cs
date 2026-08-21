using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Oficina.Api.OpenApi;

public sealed class BearerSecurityOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var actionAttributes = context.MethodInfo.GetCustomAttributes(true);
        var controllerAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(true) ?? [];

        if (actionAttributes.OfType<IAllowAnonymous>().Any()
            || controllerAttributes.OfType<IAllowAnonymous>().Any())
        {
            return;
        }

        if (!actionAttributes.OfType<IAuthorizeData>().Any()
            && !controllerAttributes.OfType<IAuthorizeData>().Any())
        {
            return;
        }

        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            }] = []
        });
    }
}
