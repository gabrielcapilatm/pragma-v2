namespace FinancialApi.Api.Extensions;

using FinancialApi.Api.Middlewares;
using Scalar.AspNetCore;
using Serilog;

public static class WebApplicationExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        app.UseCors("Frontend");
        app.UseMiddleware<CorrelationIdMiddleware>();
        app.UseMiddleware<ErrorHandlingMiddleware>();
        app.UseSerilogRequestLogging();

        if (app.Environment.IsDevelopment())
        {
            app.MapOpenApi();
            app.MapScalarApiReference(options =>
            {
                var authority = app.Configuration["Keycloak:Authority"];
                var realm = app.Configuration["Keycloak:Realm"];
                var clientId = app.Configuration["Keycloak:ClientId"] ?? "latam-api";
                var tokenUrl = $"{authority}/realms/{realm}/protocol/openid-connect/token";

                options
                    .WithTitle("Financial API")
                    .AddPreferredSecuritySchemes("Bearer")
                    .AddPasswordFlow("Bearer", flow =>
                    {
                        flow.WithClientId(clientId)
                            .WithTokenUrl(tokenUrl)
                            .WithCredentialsLocation(CredentialsLocation.Body);
                    });
            });
        }

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        return app;
    }
}
