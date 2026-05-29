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
        var actionAttributes = context.MethodInfo.GetCustomAttributes(inherit: true);
        var controllerAttributes = context.MethodInfo.DeclaringType?.GetCustomAttributes(inherit: true) ?? [];
        var endpointMetadata = context.ApiDescription.ActionDescriptor.EndpointMetadata;

        var allowsAnonymous =
            actionAttributes.OfType<IAllowAnonymous>().Any()
            || controllerAttributes.OfType<IAllowAnonymous>().Any()
            || endpointMetadata.OfType<IAllowAnonymous>().Any();

        if (allowsAnonymous)
        {
            operation.Security = [];
            return;
        }

        var requiresAuthorize =
            actionAttributes.OfType<IAuthorizeData>().Any()
            || controllerAttributes.OfType<IAuthorizeData>().Any()
            || endpointMetadata.OfType<IAuthorizeData>().Any();

        if (!requiresAuthorize)
        {
            operation.Security = [];
            return;
        }
    }
}
