using Core;
using EntityFramework;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Backend;

[ApiController]
[Route("github")]
public class GitHubController : ControllerBase
{
    private GitHub _github { get; init; }
    private IServiceScopeFactory _serviceScopeFactory { get; init; }

    public GitHubController(GitHub github, IServiceScopeFactory serviceScopeFactory)
    {
        this._github = github;
        this._serviceScopeFactory = serviceScopeFactory;
    }

    [HttpGet("history")]
    public async Task<IResult> History()
    {
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BackDbContext>();
        List<Entities.Request> entityRequests = await dbContext.Requests
            .Include(r => r.VerboseUser)
                .ThenInclude(r => r.UserFollowers)
            .Include(r => r.VerboseUser)
                .ThenInclude(r => r.UserFollowing)
            .ToListAsync();

        var modelRequests = entityRequests.Select(r => r.ToModel());

        return Results.Ok(modelRequests);
    }

    [HttpGet("find/{username}")]
    public async Task<IResult> Find(string username)
    {
        var time = DateTime.UtcNow;
        using var scope = _serviceScopeFactory.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<BackDbContext>();

        var user = await _github.GetUser(username);
        var userEntity = Entities.ToEntity(user);
        var addUserTask = dbContext.AddAsync(userEntity);

        var requestEntity = new Entities.Request()
        {
            Time = time,
            VerboseUser = userEntity,
        };

        var addRequestTask = dbContext.AddAsync(requestEntity);

        await addUserTask;
        await addRequestTask;

        await dbContext.SaveChangesAsync();

        return Results.Ok(user);
    }
}
