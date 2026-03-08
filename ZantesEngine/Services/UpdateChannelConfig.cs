using System;

namespace ZantesEngine.Services
{
    internal static class UpdateChannelConfig
    {
        private const string PlaceholderOwner = "arapxd";
        private const string PlaceholderRepo = "ZantesTweak";

        public static string Owner =>
            Read("ZANTES_GITHUB_OWNER", PlaceholderOwner);

        public static string Repo =>
            Read("ZANTES_GITHUB_REPO", PlaceholderRepo);

        public static bool IsConfigured =>
            !string.Equals(Owner, PlaceholderOwner, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(Repo, PlaceholderRepo, StringComparison.OrdinalIgnoreCase);

        private static string Read(string envName, string fallback)
        {
            string value = Environment.GetEnvironmentVariable(envName) ?? string.Empty;
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
