namespace EntityFramework;

using Core;

public static class Entities
{
    public class VerboseUser
    {
        public long UID { get; set; }
        public long UserID { get; set; }
        public string UserLogin { get; set; } = "null";
        public List<FollowerUser> UserFollowers { get; set; } = [];
        public List<FollowingUser> UserFollowing { get; set; } = [];
    }

    public class SummaryUser
    {
        public long UID { get; set; }
        public long UserID { get; set; }
        public string UserLogin { get; set; } = "null";
    }

    public class FollowerUser : SummaryUser;

    public class FollowingUser : SummaryUser;

    public class Request
    {
        public long UID { get; set; }
        public long VerboseUserUID { get; set; }
        public VerboseUser VerboseUser { get; set; }
        public DateTime Time { get; set; }
    }

    public static SummaryUser ToEntity(this Core.SummaryUser dto)
        => new()
        {
            UserLogin = dto.Login,
            UserID = dto.Id
        };

    public static FollowerUser ToFollower(this Core.SummaryUser dto)
        => new()
        {
            UserID = dto.Id,
            UserLogin = dto.Login
        };

    public static FollowingUser ToFollowing(this Core.SummaryUser dto)
          => new()
          {
              UserID = dto.Id,
              UserLogin = dto.Login
          };

    public static Core.SummaryUser ToModel(this SummaryUser entity)
        => new(
            entity.UserLogin,
            entity.UserID
            );

    public static VerboseUser ToEntity(this Core.VerboseUser dto)
        => new()
        {
            UserID = dto.Id,
            UserLogin = dto.Login,
            UserFollowers = dto.Followers.Select(ToFollower).ToList(),
            UserFollowing = dto.Following.Select(ToFollowing).ToList()
        };

    public static Core.VerboseUser ToModel(this VerboseUser entity)
        => new(
            entity.UserLogin,
            entity.UserID,
            entity.UserFollowers.Select(ToModel).ToList(),
            entity.UserFollowing.Select(ToModel).ToList()
            );

    public static Request ToEntity(this Core.Request dto)
    => new()
    {
        Time = dto.Time,
        VerboseUser = dto.VerboseUser.ToEntity(),
    };

    public static Core.Request ToModel(this Request entity)
        => new(
            entity.VerboseUser.ToModel(),
            entity.Time
        );
}
