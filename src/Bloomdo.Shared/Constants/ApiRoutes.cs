namespace Bloomdo.Shared.Constants;

public class ApiRoutes
{
    public const string Base = "api";

    public static class Tasks
    {
        public const string GetAll = $"{Base}/tasks";
        public const string Create = $"{Base}/tasks";
        public const string GetById = $"{Base}/tasks/{{id}}";
    }
}