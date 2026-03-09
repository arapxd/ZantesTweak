using System;

namespace ZantesEngine.Services
{
    internal static class UpdateChannelConfig
    {
        private const string DefaultOwner = "arapxd";
        private const string DefaultRepo = "ZantesTweak";

        public static string Owner =>
            Read("ZANTES_GITHUB_OWNER", DefaultOwner);

        public static string Repo =>
            Read("ZANTES_GITHUB_REPO", DefaultRepo);

        public static bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Owner) &&
            !string.IsNullOrWhiteSpace(Repo);

        private static string Read(string envName, string fallback)
        {
            string value = Environment.GetEnvironmentVariable(envName) ?? string.Empty;
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }
    }
}
