namespace InviteStudio.Application.Helpers;

public static class GlobalConstants
{
    public static class EventTypes
    {
        public const string Wedding = "Wedding";
        public const string Birthday = "Birthday";
        public const string Anniversary = "Anniversary";
        public const string BabyShower = "Baby Shower";
        public const string Graduation = "Graduation";
        public const string Corporate = "Corporate";

        public static IReadOnlyList<string> All { get; } =
            new[] { Wedding, Birthday, Anniversary, BabyShower, Graduation, Corporate };
    }
}