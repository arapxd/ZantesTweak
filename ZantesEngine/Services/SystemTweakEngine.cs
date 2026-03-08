using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Management;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;

namespace ZantesEngine.Services
{
    public sealed class SystemTweakResult
    {
        public required string Title { get; init; }
        public bool Success { get; init; }
        public string Output { get; init; } = string.Empty;
    }

    public static class SystemTweakEngine
    {
        public static bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        public static (bool Success, string Message) CreateRestorePoint(string description)
        {
            try
            {
                using var mc = new ManagementClass(@"\\.\root\default:SystemRestore");
                using var inParams = mc.GetMethodParameters("CreateRestorePoint");

                inParams["Description"] = description;
                inParams["RestorePointType"] = 0;
                inParams["EventType"] = 100;

                using var outParams = mc.InvokeMethod("CreateRestorePoint", inParams, null);
                uint code = Convert.ToUInt32(outParams?["ReturnValue"] ?? uint.MaxValue);

                return code switch
                {
                    0 => (true, "Restore point created successfully."),
                    5 => (false, "System Restore is disabled on this machine."),
                    6 => (false, "A restore point is already being created."),
                    7 => (false, "Restore point interval not reached yet."),
                    _ => (false, $"CreateRestorePoint returned code {code}.")
                };
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        }

        public static async Task<IReadOnlyList<SystemTweakResult>> ApplyAsync(
            IEnumerable<SystemTweakDefinition> tweaks,
            CancellationToken cancellationToken)
        {
            var results = new List<SystemTweakResult>();

            foreach (var tweak in tweaks)
            {
                cancellationToken.ThrowIfCancellationRequested();

                bool success = true;
                var logs = new List<string>();

                foreach (var command in tweak.Commands)
                {
                    var (exitCode, output, error) = await RunCmdAsync(command, cancellationToken);
                    logs.Add($"> {command}");
                    if (!string.IsNullOrWhiteSpace(output))
                        logs.Add(output.Trim());
                    if (!string.IsNullOrWhiteSpace(error))
                        logs.Add(error.Trim());

                    if (exitCode != 0)
                    {
                        success = false;
                        break;
                    }
                }

                results.Add(new SystemTweakResult
                {
                    Title = tweak.Title,
                    Success = success,
                    Output = string.Join(Environment.NewLine, logs)
                });
            }

            return results;
        }

        private static async Task<(int ExitCode, string Output, string Error)> RunCmdAsync(string command, CancellationToken token)
        {
            using var process = new Process();
            process.StartInfo = new ProcessStartInfo
            {
                FileName = "cmd.exe",
                Arguments = $"/c {command}",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            process.Start();

            string output = await process.StandardOutput.ReadToEndAsync();
            string error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync(token);
            return (process.ExitCode, output, error);
        }
    }
}
