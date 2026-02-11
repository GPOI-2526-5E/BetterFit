using Microsoft.AspNetCore.Authorization;
using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Betterfit.Infrastructure.Swagger;

/// <summary>
/// Adds Bearer auth requirement to operations that require authorization.
/// </summary>
public sealed class BearerAuthOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        var allowsAnonymous = endpointMetadata.OfType<AllowAnonymousAttribute>().Any();
        if (allowsAnonymous)
        {
            return;
        }

        var requiresAuthorize = endpointMetadata.OfType<AuthorizeAttribute>().Any();
        if (!requiresAuthorize)
        {
            return;
        }

        operation.Security ??= [];
        operation.Security.Add(new OpenApiSecurityRequirement
        {
            [new OpenApiSecuritySchemeReference("Bearer", null!, null)] = []
        });
    }
}
