var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
        .AllowAnyHeader()
        .AllowAnyMethod();
    });
});

builder.Services.AddScoped<SqlOptimizer.Web.Services.ISqlOptimizerService, SqlOptimizer.Web.Services.SqlOptimizerService>();
builder.Services.AddScoped<SqlOptimizer.Web.Services.ISqlParser, SqlOptimizer.Web.Services.SqlParser>();
builder.Services.AddScoped<SqlOptimizer.Web.Services.ISqlAnalyzer, SqlOptimizer.Web.Services.SqlAnalyzer>();

var app = builder.Build();
app.UseCors();


if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}


app.MapControllers();



app.Run();
