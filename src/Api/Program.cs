using FinancialApi.Api.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.AddServices();

var app = builder.Build();

app.UseApiPipeline();

app.Run();

public partial class Program { }
