using Core;

namespace Backend;

public static class DbModels
{
    public class VerboseUser
    {
        public long UID { get; set; }
        public long UserID { get; set; }
        public string UserLogin { get; set; }
        public List<FollowerUser> UserFollowers { get; set; }
        public List<FollowingUser> UserFollowing { get; set; }
    }

    public class SummaryUser
    {
        public long UID { get; set; }
        public long UserID { get; set; }
        public string UserLogin { get; set; }
    }

    public class FollowerUser : SummaryUser;

    public class FollowingUser : SummaryUser;

    public class Request
    {
        public long UID { get; set; }
        public DateTime Time { get; set; }
        public long VerboseUserUID { get; set; }

        public Request(long VerboseUserUID)
        {
            this.Time = DateTime.UtcNow;
            this.VerboseUserUID = VerboseUserUID;
        }
    }

    public static SummaryUser ToModel(GitHub.SummaryUser dto)
        => new()
        {
            UserID = dto.Id,
            UserLogin = dto.Login
        };

    public static FollowerUser ToFollower(GitHub.SummaryUser dto)
        => new()
        {
            UserID = dto.Id,
            UserLogin = dto.Login
        };

    public static FollowingUser ToFollowing(GitHub.SummaryUser dto)
          => new()
          {
              UserID = dto.Id,
              UserLogin = dto.Login
          };


    public static VerboseUser ToModel(GitHub.VerboseUser dto)
        => new()
        {
            UserID = dto.Id,
            UserLogin = dto.Login,
            UserFollowers = dto.Followers.Select(ToFollower).ToList(),
            UserFollowing = dto.Following.Select(ToFollowing).ToList()
        };
}
