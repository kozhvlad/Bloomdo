namespace Bloomdo.Shared.Constants;

public static class ApiRoutes
{
    public const string Base = "api";

    public static class Auth
    {
        private const string BaseRoute = $"{Base}/auth";

        public const string Register = $"{BaseRoute}/register";
        public const string Login = $"{BaseRoute}/login";
        public const string Refresh = $"{BaseRoute}/refresh";
        public const string Revoke = $"{BaseRoute}/revoke";
        public const string Me = $"{BaseRoute}/me";
    }

    public static class Profile
    {
        private const string BaseRoute = $"{Base}/profile";

        public const string Update = BaseRoute;
        public const string Stats = $"{BaseRoute}/stats";
    }

    public static class Stats
    {
        private const string BaseRoute = $"{Base}/stats";

        public const string Sync = $"{BaseRoute}/sync";
        public const string Daily = $"{BaseRoute}/daily";
        public const string Calendar = $"{BaseRoute}/calendar";
        public const string Weekly = $"{BaseRoute}/weekly";
    }

    public static class Blocks
    {
        private const string BaseRoute = $"{Base}/blocks";

        public const string List = BaseRoute;
        public const string Create = BaseRoute;
        public const string ById = $"{BaseRoute}/{{id}}";
    }

    public static class Activities
    {
        private const string BaseRoute = $"{Base}/activities";

        public const string Daily = $"{BaseRoute}/daily";
        public const string Groups = $"{BaseRoute}/groups";
        public const string GroupById = $"{BaseRoute}/groups/{{id}}";
        public const string Items = $"{BaseRoute}/items";
        public const string ItemById = $"{BaseRoute}/items/{{id}}";
        public const string Toggle = $"{BaseRoute}/toggle";
    }

    public static class Achievements
    {
        private const string BaseRoute = $"{Base}/achievements";

        public const string List = BaseRoute;
    }
}
