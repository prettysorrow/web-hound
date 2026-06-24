using Core;
using Services;
using EntityFramework;
using Microsoft.EntityFrameworkCore;
using Backend;

var builder = WebApplication.CreateBuilder(args);
var config = builder.Configuration;

var github_pat = config["github_pat"] ?? throw new IsNotSpecifiedException("GitHub PAT");
var url = config["backend_url"] ?? throw new IsNotSpecifiedException("Backend URL");
var postgre = config["db_connection"] ?? throw new IsNotSpecifiedException("Database Connection");

builder.Services.AddHttpClient();

builder.Services.AddSingleton<IRequestsProvider>(serviceProvider =>
{
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    return new RequestsProvider(httpClientFactory);
});

builder.Services.AddSingleton<GitHub>(serviceProvider =>
{
    var requestsProvider = serviceProvider.GetRequiredService<IRequestsProvider>();
    return new GitHub(github_pat, requestsProvider);
}
);

builder.Services.AddControllers();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod());
});

builder.Services.AddDbContext<BackDbContext>(options => options.UseNpgsql(postgre));

var app = builder.Build();
app.MapControllers();
app.UseCors();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BackDbContext>();
    await db.Database.MigrateAsync();
}

app.Run(url);
