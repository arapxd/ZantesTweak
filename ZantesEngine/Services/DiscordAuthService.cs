using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ZantesEngine.Services
{
    public sealed class DiscordUserProfile
    {
        public required string Id { get; init; }
        public required string DisplayName { get; init; }
        public string AvatarUrl { get; init; } = string.Empty;
        public string FallbackAvatarUrl { get; init; } = string.Empty;
    }

    public sealed class DiscordAuthResult
    {
        public bool Success { get; init; }
        public DiscordUserProfile? Profile { get; init; }
        public string ErrorKey { get; init; } = string.Empty;
        public string ErrorDetail { get; init; } = string.Empty;
    }

    internal sealed class DiscordTokenPayload
    {
        public string AccessToken { get; set; } = string.Empty;
        public string RefreshToken { get; set; } = string.Empty;
        public DateTimeOffset ExpiresAtUtc { get; set; }
    }

    public static class DiscordAuthService
    {
        private const string ClientIdDefault = "1478153809044701446";
        private const string RedirectUri = "http://127.0.0.1:43721/callback/";
        private const string ListenerPrefix = "http://127.0.0.1:43721/callback/";
        private const string AuthorizeUrl = "https://discord.com/oauth2/authorize";
        private const string TokenUrl = "https://discord.com/api/oauth2/token";
        private const string UserUrl = "https://discord.com/api/users/@me";
        private const string Scope = "identify";

        private static readonly HttpClient Http = new()
        {
            Timeout = TimeSpan.FromSeconds(20)
        };

        public static async Task<DiscordAuthResult> SignInAsync(CancellationToken token)
        {
            string clientId = ResolveClientId();
            if (string.IsNullOrWhiteSpace(clientId) || clientId.Contains("PASTE_DISCORD_CLIENT_ID", StringComparison.Ordinal))
            {
                return new DiscordAuthResult
                {
                    ErrorKey = "login.err.client_id"
                };
            }

            string verifier = CreateCodeVerifier();
            string challenge = CreateCodeChallenge(verifier);
            string state = CreateRandomBase64Url(24);

            if (!TryOpenBrowser(BuildAuthorizeUrl(clientId, challenge, state)))
            {
                return new DiscordAuthResult
                {
                    ErrorKey = "login.err.browser_open"
                };
            }

            var callback = await WaitForAuthorizationCodeAsync(state, token);
            if (!callback.Success)
            {
                return new DiscordAuthResult
                {
                    ErrorKey = callback.ErrorKey,
                    ErrorDetail = callback.ErrorDetail
                };
            }

            if (string.IsNullOrWhiteSpace(callback.Code))
            {
                return new DiscordAuthResult
                {
                    ErrorKey = "login.err.callback"
                };
            }

            var tokenResult = await ExchangeCodeForTokenAsync(clientId, callback.Code, verifier, token);
            if (!tokenResult.Success || tokenResult.Token == null)
            {
                return new DiscordAuthResult
                {
                    ErrorKey = "login.err.token",
                    ErrorDetail = tokenResult.ErrorDetail
                };
            }

            TokenStore.Save(tokenResult.Token);

            var profileResult = await FetchUserProfileAsync(tokenResult.Token.AccessToken, token);
            if (!profileResult.Success || profileResult.Profile == null)
            {
                return new DiscordAuthResult
                {
                    ErrorKey = "login.err.profile",
                    ErrorDetail = profileResult.ErrorDetail
                };
            }

            return new DiscordAuthResult
            {
                Success = true,
                Profile = profileResult.Profile
            };
        }

        public static async Task<DiscordAuthResult> TryRestoreSessionAsync(CancellationToken token)
        {
            var stored = TokenStore.Load();
            if (stored == null)
                return new DiscordAuthResult();

            string accessToken = stored.AccessToken;

            if (stored.ExpiresAtUtc <= DateTimeOffset.UtcNow.AddMinutes(1))
            {
                var refreshed = await RefreshTokenAsync(stored, token);
                if (!refreshed.Success || refreshed.Token == null)
                {
                    TokenStore.Clear();
                    return new DiscordAuthResult
                    {
                        ErrorKey = "login.err.restore"
                    };
                }

                stored = refreshed.Token;
                accessToken = stored.AccessToken;
                TokenStore.Save(stored);
            }

            var profileResult = await FetchUserProfileAsync(accessToken, token);
            if (!profileResult.Success || profileResult.Profile == null)
            {
                if (profileResult.StatusCode == HttpStatusCode.Unauthorized)
                {
                    var refreshed = await RefreshTokenAsync(stored, token);
                    if (!refreshed.Success || refreshed.Token == null)
                    {
                        TokenStore.Clear();
                        return new DiscordAuthResult();
                    }

                    TokenStore.Save(refreshed.Token);
                    var retryProfile = await FetchUserProfileAsync(refreshed.Token.AccessToken, token);
                    if (!retryProfile.Success || retryProfile.Profile == null)
                    {
                        TokenStore.Clear();
                        return new DiscordAuthResult();
                    }

                    return new DiscordAuthResult
                    {
                        Success = true,
                        Profile = retryProfile.Profile
                    };
                }

                return new DiscordAuthResult();
            }

            return new DiscordAuthResult
            {
                Success = true,
                Profile = profileResult.Profile
            };
        }

        public static void SignOut()
            => TokenStore.Clear();

        private static string ResolveClientId()
        {
            string fromEnv = Environment.GetEnvironmentVariable("ZANTES_DISCORD_CLIENT_ID") ?? string.Empty;
            if (!string.IsNullOrWhiteSpace(fromEnv))
                return fromEnv.Trim();

            return ClientIdDefault;
        }

        private static bool TryOpenBrowser(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static async Task<(bool Success, string Code, string ErrorKey, string ErrorDetail)> WaitForAuthorizationCodeAsync(
            string expectedState,
            CancellationToken token)
        {
            using var listener = new HttpListener();
            listener.Prefixes.Add(ListenerPrefix);
            try
            {
                listener.Start();
            }
            catch (Exception ex)
            {
                return (false, string.Empty, "login.err.callback", ex.Message);
            }

            try
            {
                Task<HttpListenerContext> waitContext = listener.GetContextAsync();
                Task delay = Task.Delay(TimeSpan.FromMinutes(3), token);

                Task done = await Task.WhenAny(waitContext, delay);
                if (done != waitContext)
                {
                    return (false, string.Empty, "login.err.timeout", string.Empty);
                }

                var context = await waitContext;
                var query = ParseQuery(context.Request.Url?.Query ?? string.Empty);

                if (query.TryGetValue("error", out var err))
                {
                    await WriteBrowserResponseAsync(context.Response, "Discord auth cancelled.");
                    return (false, string.Empty, "login.err.cancelled", err);
                }

                if (!query.TryGetValue("state", out var state) || !string.Equals(state, expectedState, StringComparison.Ordinal))
                {
                    await WriteBrowserResponseAsync(context.Response, "Invalid state. You can close this tab.");
                    return (false, string.Empty, "login.err.state", string.Empty);
                }

                if (!query.TryGetValue("code", out var code) || string.IsNullOrWhiteSpace(code))
                {
                    await WriteBrowserResponseAsync(context.Response, "Authorization code not found.");
                    return (false, string.Empty, "login.err.callback", string.Empty);
                }

                await WriteBrowserResponseAsync(context.Response, "Zantes Tweak authorization completed. You can close this tab.");
                return (true, code, string.Empty, string.Empty);
            }
            catch (OperationCanceledException)
            {
                return (false, string.Empty, "login.err.timeout", string.Empty);
            }
            catch (Exception ex)
            {
                return (false, string.Empty, "login.err.callback", ex.Message);
            }
        }

        private static async Task WriteBrowserResponseAsync(HttpListenerResponse response, string message)
        {
            string body = $"<html><body style='font-family:Segoe UI;background:#09070E;color:#F2F1FB;padding:20px;'><h2>Zantes Tweak</h2><p>{WebUtility.HtmlEncode(message)}</p></body></html>";
            byte[] data = Encoding.UTF8.GetBytes(body);
            response.ContentType = "text/html; charset=utf-8";
            response.ContentLength64 = data.Length;
            await response.OutputStream.WriteAsync(data);
            response.OutputStream.Close();
        }

        private static async Task<(bool Success, DiscordTokenPayload? Token, string ErrorDetail)> ExchangeCodeForTokenAsync(
            string clientId,
            string code,
            string verifier,
            CancellationToken token)
        {
            var form = new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = RedirectUri,
                ["code_verifier"] = verifier
            };

            using var content = new FormUrlEncodedContent(form);
            using var response = await Http.PostAsync(TokenUrl, content, token);
            string payload = await response.Content.ReadAsStringAsync(token);

            if (!response.IsSuccessStatusCode)
                return (false, null, payload);

            return (true, ParseTokenPayload(payload), string.Empty);
        }

        private static async Task<(bool Success, DiscordTokenPayload? Token)> RefreshTokenAsync(
            DiscordTokenPayload current,
            CancellationToken token)
        {
            if (string.IsNullOrWhiteSpace(current.RefreshToken))
                return (false, null);

            string clientId = ResolveClientId();
            var form = new Dictionary<string, string>
            {
                ["client_id"] = clientId,
                ["grant_type"] = "refresh_token",
                ["refresh_token"] = current.RefreshToken
            };

            using var content = new FormUrlEncodedContent(form);
            using var response = await Http.PostAsync(TokenUrl, content, token);
            string payload = await response.Content.ReadAsStringAsync(token);

            if (!response.IsSuccessStatusCode)
                return (false, null);

            var refreshed = ParseTokenPayload(payload);
            if (refreshed == null)
                return (false, null);

            return (true, refreshed);
        }

        private static async Task<(bool Success, DiscordUserProfile? Profile, HttpStatusCode? StatusCode, string ErrorDetail)> FetchUserProfileAsync(
            string accessToken,
            CancellationToken token)
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, UserUrl);
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);
            using var res = await Http.SendAsync(req, token);
            string payload = await res.Content.ReadAsStringAsync(token);

            if (!res.IsSuccessStatusCode)
                return (false, null, res.StatusCode, payload);

            using var doc = JsonDocument.Parse(payload);
            JsonElement root = doc.RootElement;

            string id = root.GetProperty("id").GetString() ?? string.Empty;
            string username = root.GetProperty("username").GetString() ?? "Discord";
            string globalName = root.TryGetProperty("global_name", out var globalNameEl)
                ? globalNameEl.GetString() ?? string.Empty
                : string.Empty;
            string avatarHash = root.TryGetProperty("avatar", out var avatarEl)
                ? avatarEl.GetString() ?? string.Empty
                : string.Empty;
            string discriminator = root.TryGetProperty("discriminator", out var discEl)
                ? discEl.GetString() ?? "0"
                : "0";

            string displayName = string.IsNullOrWhiteSpace(globalName) ? username : globalName;

            return (true, new DiscordUserProfile
            {
                Id = id,
                DisplayName = displayName,
                AvatarUrl = BuildAvatarUrl(id, avatarHash),
                FallbackAvatarUrl = BuildDefaultAvatarUrl(id, discriminator)
            }, res.StatusCode, string.Empty);
        }

        private static DiscordTokenPayload? ParseTokenPayload(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;

                string access = root.GetProperty("access_token").GetString() ?? string.Empty;
                string refresh = root.TryGetProperty("refresh_token", out var refreshEl)
                    ? refreshEl.GetString() ?? string.Empty
                    : string.Empty;
                int expiresIn = root.TryGetProperty("expires_in", out var expiresEl)
                    ? expiresEl.GetInt32()
                    : 3600;

                if (string.IsNullOrWhiteSpace(access))
                    return null;

                return new DiscordTokenPayload
                {
                    AccessToken = access,
                    RefreshToken = refresh,
                    ExpiresAtUtc = DateTimeOffset.UtcNow.AddSeconds(Math.Max(60, expiresIn - 30))
                };
            }
            catch
            {
                return null;
            }
        }

        private static string BuildAuthorizeUrl(string clientId, string challenge, string state)
        {
            var query = new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = clientId,
                ["scope"] = Scope,
                ["redirect_uri"] = RedirectUri,
                ["state"] = state,
                ["code_challenge"] = challenge,
                ["code_challenge_method"] = "S256",
                ["prompt"] = "consent"
            };

            string qs = string.Join("&", query.Select(p =>
                $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value)}"));

            return $"{AuthorizeUrl}?{qs}";
        }

        private static string CreateCodeVerifier()
            => CreateRandomBase64Url(64);

        private static string CreateCodeChallenge(string verifier)
        {
            byte[] bytes = Encoding.ASCII.GetBytes(verifier);
            byte[] hash = SHA256.HashData(bytes);
            return ToBase64Url(hash);
        }

        private static string CreateRandomBase64Url(int byteCount)
        {
            byte[] bytes = RandomNumberGenerator.GetBytes(byteCount);
            return ToBase64Url(bytes);
        }

        private static string ToBase64Url(byte[] bytes)
            => Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');

        private static Dictionary<string, string> ParseQuery(string query)
        {
            var map = new Dictionary<string, string>(StringComparer.Ordinal);
            string clean = query.StartsWith("?") ? query[1..] : query;

            foreach (string part in clean.Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                int eq = part.IndexOf('=');
                string key = eq >= 0 ? part[..eq] : part;
                string value = eq >= 0 ? part[(eq + 1)..] : string.Empty;

                key = Uri.UnescapeDataString(key.Replace("+", " "));
                value = Uri.UnescapeDataString(value.Replace("+", " "));
                map[key] = value;
            }

            return map;
        }

        private static string BuildAvatarUrl(string userId, string avatarHash)
        {
            if (!string.IsNullOrWhiteSpace(avatarHash))
                return $"https://cdn.discordapp.com/avatars/{userId}/{avatarHash}.png?size=128";

            return string.Empty;
        }

        private static string BuildDefaultAvatarUrl(string userId, string discriminator)
        {
            int index = 0;
            if (discriminator == "0")
            {
                if (ulong.TryParse(userId, out var uid))
                    index = (int)((uid >> 22) % 6);
            }
            else if (int.TryParse(discriminator, out var discNum))
            {
                index = discNum % 5;
            }

            return $"https://cdn.discordapp.com/embed/avatars/{index}.png";
        }

        private static class TokenStore
        {
            private static readonly string StorePath =
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZantesEngine", "discord_auth.dat");

            public static void Save(DiscordTokenPayload payload)
            {
                try
                {
                    Directory.CreateDirectory(Path.GetDirectoryName(StorePath)!);
                    string json = JsonSerializer.Serialize(payload);
                    byte[] encrypted = ProtectedData.Protect(Encoding.UTF8.GetBytes(json), null, DataProtectionScope.CurrentUser);
                    File.WriteAllBytes(StorePath, encrypted);
                }
                catch
                {
                    // ignored: no persistence if storage fails
                }
            }

            public static DiscordTokenPayload? Load()
            {
                try
                {
                    if (!File.Exists(StorePath))
                        return null;

                    byte[] encrypted = File.ReadAllBytes(StorePath);
                    byte[] raw = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
                    string json = Encoding.UTF8.GetString(raw);
                    return JsonSerializer.Deserialize<DiscordTokenPayload>(json);
                }
                catch
                {
                    return null;
                }
            }

            public static void Clear()
            {
                try
                {
                    if (File.Exists(StorePath))
                        File.Delete(StorePath);
                }
                catch
                {
                    // ignored
                }
            }
        }
    }
}
