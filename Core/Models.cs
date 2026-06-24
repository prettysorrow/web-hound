namespace Core;

public record VerboseUser(
    string Login,
    long Id,
    List<SummaryUser> Followers,
    List<SummaryUser> Following
);

public record SummaryUser(
    string Login,
    long Id
);

public record Request(
    VerboseUser VerboseUser,
    DateTime Time
);
