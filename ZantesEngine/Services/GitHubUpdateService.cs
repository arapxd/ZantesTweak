using System;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ZantesEngine.Services
{
    public enum UpdateCheckState
    {
        NotConfigured,
        UpToDate,
        UpdateAvailable,
        Failed
    }

    public sealed class GitHubReleaseInfo
    {
        public required string TagName { get; init; }
        public required string Name { get; init; }
        public required Version Version { get; init; }
        public required string HtmlUrl { get; init; }
        public string Body { get; init; } = string.Empty;
        public DateTimeOffset PublishedAtUtc { get; init; }
        public bool IsPrerelease { get; init; }
    }

    public sealed class UpdateCheckResult
    {
        public required UpdateCheckState State { get; init; }
        public required Version CurrentVersion { get; init; }
        public GitHubReleaseInfo? Release { get; init; }
        public string ErrorDetail { get; init; } = string.Empty;
    }

    public static class GitHubUpdateService
    {
        private static readonly HttpClient Http = BuildHttpClient();

        public static Version CurrentVersion => GetCurrentVersion();

        public static string CurrentVersionDisplay => CurrentVersion.ToString(3);

        public static async Task<UpdateCheckResult> CheckLatestReleaseAsync(CancellationToken token)
        {
            Version current = CurrentVersion;
            if (!UpdateChannelConfig.IsConfigured)
            {
                return new UpdateCheckResult
                {
                    State = UpdateCheckState.NotConfigured,
                    CurrentVersion = current
                };
            }

            try
            {
                string url = $"https://api.github.com/repos/{UpdateChannelConfig.Owner}/{UpdateChannelConfig.Repo}/releases/latest";
                using var response = await Http.GetAsync(url, token);
                string payload = await response.Content.ReadAsStringAsync(token);
                if (!response.IsSuccessStatusCode)
                {
                    return new UpdateCheckResult
                    {
                        State = UpdateCheckState.Failed,
                        CurrentVersion = current,
                        ErrorDetail = $"GitHub API returned {(int)response.StatusCode}."
                    };
                }

                using JsonDocument doc = JsonDocument.Parse(payload);
                JsonElement root = doc.RootElement;
                string tagName = root.TryGetProperty("tag_name", out var tagEl) ? tagEl.GetString() ?? string.Empty : string.Empty;
                if (!TryParseVersion(tagName, out Version? latestVersion))
                {
                    return new UpdateCheckResult
                    {
                        State = UpdateCheckState.Failed,
                        CurrentVersion = current,
                        ErrorDetail = "Release tag could not be parsed as a version."
                    };
                }

                var release = new GitHubReleaseInfo
                {
                    TagName = tagName,
                    Name = root.TryGetProperty("name", out var nameEl) ? nameEl.GetString() ?? tagName : tagName,
                    Version = latestVersion!,
                    HtmlUrl = root.TryGetProperty("html_url", out var htmlEl) ? htmlEl.GetString() ?? string.Empty : string.Empty,
                    Body = root.TryGetProperty("body", out var bodyEl) ? bodyEl.GetString() ?? string.Empty : string.Empty,
                    PublishedAtUtc = root.TryGetProperty("published_at", out var pubEl) && DateTimeOffset.TryParse(pubEl.GetString(), out var published)
                        ? published
                        : DateTimeOffset.MinValue,
                    IsPrerelease = root.TryGetProperty("prerelease", out var preEl) && preEl.GetBoolean()
                };

                return new UpdateCheckResult
                {
                    State = latestVersion > current ? UpdateCheckState.UpdateAvailable : UpdateCheckState.UpToDate,
                    CurrentVersion = current,
                    Release = release
                };
            }
            catch (Exception ex)
            {
                return new UpdateCheckResult
                {
                    State = UpdateCheckState.Failed,
                    CurrentVersion = current,
                    ErrorDetail = ex.Message
                };
            }
        }

        public static bool TryOpenReleasePage(string? url = null)
        {
            string target = !string.IsNullOrWhiteSpace(url)
                ? url
                : $"https://github.com/{UpdateChannelConfig.Owner}/{UpdateChannelConfig.Repo}/releases";

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = target,
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Version GetCurrentVersion()
        {
            var assembly = Assembly.GetEntryAssembly() ?? typeof(GitHubUpdateService).Assembly;
            string? informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (TryParseVersion(informational, out Version? infoVersion))
                return infoVersion!;

            return assembly.GetName().Version ?? new Version(1, 0, 0);
        }

        private static bool TryParseVersion(string? raw, out Version? version)
        {
            version = null;
            if (string.IsNullOrWhiteSpace(raw))
                return false;

            string cleaned = raw.Trim();
            if (cleaned.StartsWith("v", StringComparison.OrdinalIgnoreCase))
                cleaned = cleaned.Substring(1);

            int plusIndex = cleaned.IndexOf('+');
            if (plusIndex >= 0)
                cleaned = cleaned.Substring(0, plusIndex);

            int dashIndex = cleaned.IndexOf('-');
            if (dashIndex >= 0)
                cleaned = cleaned.Substring(0, dashIndex);

            if (!Version.TryParse(cleaned, out Version? parsed))
                return false;

            version = parsed;
            return true;
        }

        private static HttpClient BuildHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(12)
            };
            client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", "ZantesTweak-Updater/1.0");
            client.DefaultRequestHeaders.TryAddWithoutValidation("Accept", "application/vnd.github+json");
            return client;
        }
    }
}
