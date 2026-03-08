using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;

namespace ZantesEngine.Services
{
    public sealed class DiscordRichPresenceActivity
    {
        public string Details { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string LargeImageKey { get; set; } = string.Empty;
        public string LargeImageText { get; set; } = string.Empty;
        public string SmallImageKey { get; set; } = string.Empty;
        public string SmallImageText { get; set; } = string.Empty;
        public long StartTimestampUnix { get; set; }
    }

    public sealed class DiscordRichPresenceService : IDisposable
    {
        private readonly string _clientId;
        private readonly object _sync = new();
        private NamedPipeClientStream? _pipe;
        private bool _disposed;

        public DiscordRichPresenceService(string clientId)
        {
            _clientId = clientId;
        }

        public bool SetActivity(DiscordRichPresenceActivity activity)
        {
            if (activity == null)
                return false;

            lock (_sync)
            {
                if (!EnsureConnected())
                    return false;

                var activityPayload = new Dictionary<string, object?>();
                if (!string.IsNullOrWhiteSpace(activity.Details))
                    activityPayload["details"] = activity.Details;
                if (!string.IsNullOrWhiteSpace(activity.State))
                    activityPayload["state"] = activity.State;

                if (activity.StartTimestampUnix > 0)
                {
                    activityPayload["timestamps"] = new Dictionary<string, long>
                    {
                        ["start"] = activity.StartTimestampUnix
                    };
                }

                var assets = new Dictionary<string, string>();
                if (!string.IsNullOrWhiteSpace(activity.LargeImageKey))
                    assets["large_image"] = activity.LargeImageKey;
                if (!string.IsNullOrWhiteSpace(activity.LargeImageText))
                    assets["large_text"] = activity.LargeImageText;
                if (!string.IsNullOrWhiteSpace(activity.SmallImageKey))
                    assets["small_image"] = activity.SmallImageKey;
                if (!string.IsNullOrWhiteSpace(activity.SmallImageText))
                    assets["small_text"] = activity.SmallImageText;

                if (assets.Count > 0)
                    activityPayload["assets"] = assets;

                var payload = new Dictionary<string, object?>
                {
                    ["cmd"] = "SET_ACTIVITY",
                    ["args"] = new Dictionary<string, object?>
                    {
                        ["pid"] = Environment.ProcessId,
                        ["activity"] = activityPayload
                    },
                    ["nonce"] = Guid.NewGuid().ToString("N")
                };

                return SendFrame(1, payload);
            }
        }

        public void ClearActivity()
        {
            lock (_sync)
            {
                if (_pipe == null || !_pipe.IsConnected)
                    return;

                var payload = new Dictionary<string, object?>
                {
                    ["cmd"] = "SET_ACTIVITY",
                    ["args"] = new Dictionary<string, object?>
                    {
                        ["pid"] = Environment.ProcessId
                    },
                    ["nonce"] = Guid.NewGuid().ToString("N")
                };

                SendFrame(1, payload);
            }
        }

        public void Disconnect()
        {
            lock (_sync)
            {
                DisposePipe();
            }
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            lock (_sync)
            {
                try
                {
                    if (_pipe != null && _pipe.IsConnected)
                        ClearActivity();
                }
                catch
                {
                    // ignored
                }

                DisposePipe();
            }

            _disposed = true;
        }

        private bool EnsureConnected()
        {
            if (_disposed)
                return false;

            if (_pipe != null && _pipe.IsConnected)
                return true;

            DisposePipe();

            for (int i = 0; i <= 9; i++)
            {
                var candidate = new NamedPipeClientStream(".", $"discord-ipc-{i}", PipeDirection.InOut, PipeOptions.Asynchronous);
                try
                {
                    candidate.Connect(150);
                    _pipe = candidate;
                    return SendHandshake();
                }
                catch
                {
                    candidate.Dispose();
                }
            }

            return false;
        }

        private bool SendHandshake()
        {
            var payload = new Dictionary<string, object?>
            {
                ["v"] = 1,
                ["client_id"] = _clientId
            };

            return SendFrame(0, payload);
        }

        private bool SendFrame(int opcode, object payload)
        {
            if (_pipe == null || !_pipe.IsConnected)
                return false;

            try
            {
                byte[] body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(payload));
                Span<byte> header = stackalloc byte[8];
                BinaryPrimitives.WriteInt32LittleEndian(header[..4], opcode);
                BinaryPrimitives.WriteInt32LittleEndian(header[4..8], body.Length);

                _pipe.Write(header);
                _pipe.Write(body, 0, body.Length);
                _pipe.Flush();
                return true;
            }
            catch
            {
                DisposePipe();
                return false;
            }
        }

        private void DisposePipe()
        {
            try
            {
                _pipe?.Dispose();
            }
            catch
            {
                // ignored
            }
            finally
            {
                _pipe = null;
            }
        }
    }
}
