using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace TradingJournal.Modules.Trades.Extensions;

/// <summary>
/// Scalar API Extensions
/// </summary>
public static class OpenApiExtensions
{
    /// <summary>
    /// Config Jwt Bearer for Scalar API
    /// </summary>
    /// <param name="options"></param>
    /// <returns></returns>
    public static OpenApiOptions UseJwtBearerAuthentication(this OpenApiOptions options)
    {
        OpenApiSecurityScheme schema = new()
        {
            Type = SecuritySchemeType.Http,
            Name = JwtBearerDefaults.AuthenticationScheme,
            Scheme = JwtBearerDefaults.AuthenticationScheme
        };

        options.AddDocumentTransformer((document, context, ct) =>
        {
            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes?.Add(JwtBearerDefaults.AuthenticationScheme, schema);
            return Task.CompletedTask;
        });

        options.AddOperationTransformer((operation, context, ct) =>
        {
            if (context.Description.ActionDescriptor.EndpointMetadata.OfType<IAuthorizeData>().Any())
            {
                var schemeReference = new OpenApiSecuritySchemeReference(JwtBearerDefaults.AuthenticationScheme);
                operation.Security = [new OpenApiSecurityRequirement() {
                    [schemeReference] = []
                }];
            }

            return Task.CompletedTask;
        });
        return options;
    }
}