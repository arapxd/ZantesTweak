using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Threading;
using System.Threading.Tasks;

namespace ZantesEngine.Services
{
    public enum NetworkTuneProfile
    {
        Safe,
        Balanced,
        Aggressive
    }

    public sealed class DnsProbeSample
    {
        public required string Label { get; init; }
        public required string Primary { get; init; }
        public required string Secondary { get; init; }
        public double AverageLatencyMs { get; init; }
    }

    public sealed class NetworkAutoTuneResult
    {
        public required IReadOnlyList<DnsProbeSample> DnsSamples { get; init; }
        public required DnsProbeSample BestDns { get; init; }
        public required int RecommendedMtuPayload { get; init; }
        public required NetworkTuneProfile Profile { get; init; }
        public required string Reason { get; init; }
    }

    public static class NetworkAutoTuneService
    {
        private static readonly (string Label, string Primary, string Secondary)[] DnsTargets =
        {
            ("Cloudflare", "1.1.1.1", "1.0.0.1"),
            ("Google", "8.8.8.8", "8.8.4.4"),
            ("Quad9", "9.9.9.9", "149.112.112.112"),
            ("OpenDNS", "208.67.222.222", "208.67.220.220")
        };

        public static async Task<NetworkAutoTuneResult> RunAsync(CancellationToken token)
        {
            var samples = new List<DnsProbeSample>(DnsTargets.Length);
            foreach (var target in DnsTargets)
            {
                token.ThrowIfCancellationRequested();
                double avg = await MeasurePingAverageAsync(target.Primary, attempts: 3, timeoutMs: 900, token);
                samples.Add(new DnsProbeSample
                {
                    Label = target.Label,
                    Primary = target.Primary,
                    Secondary = target.Secondary,
                    AverageLatencyMs = avg
                });
            }

            var bestDns = samples.OrderBy(s => s.AverageLatencyMs).First();
            int mtuPayload = await DiscoverMtuPayloadAsync(bestDns.Primary, token);
            if (mtuPayload < 1200)
                mtuPayload = await DiscoverMtuPayloadAsync("1.1.1.1", token);

            var profile = DecideProfile(bestDns.AverageLatencyMs, mtuPayload);
            string reason = profile switch
            {
                NetworkTuneProfile.Aggressive => "Low latency route and high MTU detected.",
                NetworkTuneProfile.Balanced => "Stable route detected with medium latency.",
                _ => "Conservative profile selected for route stability."
            };

            return new NetworkAutoTuneResult
            {
                DnsSamples = samples,
                BestDns = bestDns,
                RecommendedMtuPayload = mtuPayload,
                Profile = profile,
                Reason = reason
            };
        }

        private static NetworkTuneProfile DecideProfile(double dnsLatency, int mtuPayload)
        {
            if (dnsLatency <= 20 && mtuPayload >= 1472)
                return NetworkTuneProfile.Aggressive;

            if (dnsLatency <= 38 && mtuPayload >= 1452)
                return NetworkTuneProfile.Balanced;

            return NetworkTuneProfile.Safe;
        }

        private static async Task<double> MeasurePingAverageAsync(string host, int attempts, int timeoutMs, CancellationToken token)
        {
            double sum = 0;
            int ok = 0;

            using var ping = new Ping();
            for (int i = 0; i < attempts; i++)
            {
                token.ThrowIfCancellationRequested();
                try
                {
                    var reply = await ping.SendPingAsync(host, timeoutMs);
                    if (reply.Status == IPStatus.Success)
                    {
                        sum += reply.RoundtripTime;
                        ok++;
                    }
                }
                catch
                {
                    // ignored
                }
            }

            if (ok == 0)
                return 999;

            return sum / ok;
        }

        private static async Task<int> DiscoverMtuPayloadAsync(string host, CancellationToken token)
        {
            int low = 1200;
            int high = 1472;
            int best = 1200;

            while (low <= high)
            {
                token.ThrowIfCancellationRequested();
                int mid = (low + high) / 2;
                bool success = await CanPingWithoutFragmentAsync(host, mid, token);

                if (success)
                {
                    best = mid;
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return best;
        }

        private static async Task<bool> CanPingWithoutFragmentAsync(string host, int payloadSize, CancellationToken token)
        {
            using var ping = new Ping();
            var payload = new byte[payloadSize];
            var options = new PingOptions(64, true);

            try
            {
                var reply = await ping.SendPingAsync(host, 1300, payload, options);
                return reply.Status == IPStatus.Success;
            }
            catch
            {
                return false;
            }
        }
    }
}

