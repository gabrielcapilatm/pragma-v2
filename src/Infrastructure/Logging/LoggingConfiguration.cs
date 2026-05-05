namespace FinancialApi.Infrastructure.Logging;

using Microsoft.AspNetCore.Builder;
using Serilog;
using Serilog.Formatting.Compact;

public static class LoggingConfiguration
{
    public static void ConfigureSerilog(WebApplicationBuilder builder)
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(builder.Configuration)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("Application", "FinancialApi")
            .WriteTo.Console(new CompactJsonFormatter())
            .CreateLogger();

        builder.Host.UseSerilog();
    }
}
