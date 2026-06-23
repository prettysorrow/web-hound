using Core;
using Services;
using EntityFramework;
using Microsoft.EntityFrameworkCore;

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

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod());
});

builder.Services.AddDbContext<BackDbContext>(options => options.UseNpgsql(postgre));

var app = builder.Build();
app.UseCors();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BackDbContext>();
    await db.Database.MigrateAsync();
}

app.MapGet("/github/api/find/{username}", async (string username, GitHub github, BackDbContext db) =>
{
    var verbose = await github.GetUser(username);
    var verboseModel = DbModels.ToModel(verbose);

    Console.WriteLine($"[debug] /github/find/{username} followers: [ {string.Join(", ", verboseModel.UserFollowers.Select(e => e.UserLogin).ToList())} ], following: [ {string.Join(", ", verboseModel.UserFollowing.Select(e => e.UserLogin).ToList())} ]");

    await db.VerboseUsers.AddAsync(verboseModel);
    await db.SaveChangesAsync();

    var requestModel = new DbModels.Request(VerboseUserUID: verboseModel.UID);
    Console.WriteLine($"[debug] /github/find/{username} requestModel: {requestModel}");
    await db.Requests.AddAsync(requestModel);
    await db.SaveChangesAsync();

    return Results.Ok(verbose);
});

app.MapGet("/github/history/requests", async (BackDbContext db) =>
{
    var requests = await db.Requests.ToListAsync();
    return Results.Ok(requests);
});

app.MapGet("/github/history/find/{username}", async (string username, BackDbContext db)
    =>
      {
          var verbose = db.VerboseUsers
            .Include(v => v.UserFollowing)
            .Include(v => v.UserFollowers)
            .OrderBy(v => v.UID)
            .Last(v => v.UserLogin == username);

          return verbose switch
          {
              null => Results.NotFound($"[tip] try find user via \"/github/api/find/{username}\" first"),
              _ => Results.Ok(verbose)
          };
      }
);

app.MapGet("/github/history/verbose", async (BackDbContext db) =>
{
    var verboses = await db.VerboseUsers
        .Include(v => v.UserFollowing)
        .Include(v => v.UserFollowers)
        .ToListAsync();

    var filtered = verboses
        .GroupBy(v => v.UserLogin)
        .Select(vs => vs.Last())
        .ToList();

    return Results.Ok(filtered);
});

app.Run(url);
