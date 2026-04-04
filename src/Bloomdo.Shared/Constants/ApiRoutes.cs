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
        public const string VerifyPhoto = $"{BaseRoute}/verify-photo";
    }

    public static class Achievements
    {
        private const string BaseRoute = $"{Base}/achievements";

        public const string List = BaseRoute;
    }

    public static class Chat
    {
        private const string BaseRoute = $"{Base}/chat";

        public const string Conversations = $"{BaseRoute}/conversations";
        public const string ConversationById = $"{BaseRoute}/conversations/{{id}}";
        public const string Messages = $"{BaseRoute}/conversations/{{id}}/messages";
    }

    public static class Subscription
    {
        private const string BaseRoute = $"{Base}/subscription";

        public const string Status = BaseRoute;
        public const string Checkout = $"{BaseRoute}/checkout";
        public const string Cancel = $"{BaseRoute}/cancel";
        public const string Webhook = $"{BaseRoute}/webhook";
        public const string CheckoutSuccess = $"{BaseRoute}/checkout/success";
        public const string CheckoutCancel = $"{BaseRoute}/checkout/cancel";
    }

    public static class Social
    {
        private const string BaseRoute = $"{Base}/social";

        // Search
        public const string Search = $"{BaseRoute}/search";

        // Followers / Following
        public const string Followers = $"{BaseRoute}/followers";
        public const string Following = $"{BaseRoute}/following";
        public const string Follow = $"{BaseRoute}/follow/{{userId}}";
        public const string Unfollow = $"{BaseRoute}/unfollow/{{userId}}";
        public const string MutualFollowers = $"{BaseRoute}/mutual";

        // Follow requests (for private profiles)
        public const string FollowRequests = $"{BaseRoute}/follow-requests";
        public const string RespondFollowRequest = $"{BaseRoute}/follow-requests/{{id}}";

        // Notifications
        public const string Notifications = $"{BaseRoute}/notifications";
        public const string ReadNotification = $"{BaseRoute}/notifications/{{id}}/read";

        // Shared groups
        public const string SharedGroups = $"{BaseRoute}/groups";
        public const string SharedGroupById = $"{BaseRoute}/groups/{{id}}";
        public const string SharedGroupUpdate = $"{BaseRoute}/groups/{{id}}";
        public const string SharedGroupInvite = $"{BaseRoute}/groups/{{id}}/invite";
        public const string SharedGroupInviteRespond = $"{BaseRoute}/groups/{{id}}/invite/respond";
        public const string SharedGroupMemberRemove = $"{BaseRoute}/groups/{{id}}/members/{{memberId}}";
    }
}
