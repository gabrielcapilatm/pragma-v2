namespace FinancialApi.Api.Extensions;

using FinancialApi.Api.OpenApi;
using FinancialApi.Application;
using FinancialApi.Infrastructure;
using FinancialApi.Infrastructure.Logging;

public static class WebApplicationBuilderExtensions
{
    public static WebApplicationBuilder AddServices(this WebApplicationBuilder builder)
    {
        LoggingConfiguration.ConfigureSerilog(builder);

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("Frontend", policy =>
                policy.WithOrigins("http://localhost:3000")
                      .AllowAnyHeader()
                      .AllowAnyMethod());
        });

        builder.Services.AddControllers();
        builder.Services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer<BearerSecuritySchemeTransformer>();
            options.AddOperationTransformer<SecurityRequirementOperationTransformer>();
        });

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        return builder;
    }
}
