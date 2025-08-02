var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

// Add logging
builder.Services.AddLogging(loggingBuilder =>
{
    loggingBuilder.AddConsole();
    loggingBuilder.AddDebug();
});

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

// Service registrations
builder.Services.AddScoped<SqlOptimizer.Web.Services.ISqlOptimizerService, SqlOptimizer.Web.Services.SqlOptimizerService>();
builder.Services.AddScoped<SqlOptimizer.Web.Services.IResultBuilderService, SqlOptimizer.Web.Services.ResultBuilderService>();

// Register both old and new analyzers for compatibility
builder.Services.AddScoped<SqlOptimizer.Web.Services.SqlAnalyzer>();
builder.Services.AddScoped<SqlOptimizer.Web.Services.EnhancedSqlAnalyzer>();

// Use enhanced analyzer as the default implementation
builder.Services.AddScoped<SqlOptimizer.Web.Services.ISqlAnalyzer>(provider => 
    provider.GetRequiredService<SqlOptimizer.Web.Services.EnhancedSqlAnalyzer>());

var app = builder.Build();
app.UseCors();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.MapControllers();



app.Run();
