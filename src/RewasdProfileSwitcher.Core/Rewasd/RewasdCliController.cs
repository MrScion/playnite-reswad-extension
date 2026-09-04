using System;
using System.Diagnostics;

namespace RewasdProfileSwitcher.Core.Rewasd
{
    /// <summary>
    /// Switches the active reWASD gamepad profile by shelling out to
    /// reWASDCommandLine.exe ("apply --id &lt;deviceId&gt; --path &lt;configPath&gt;
    /// --slot &lt;slot&gt;").
    /// </summary>
    public static class RewasdCliController
    {
        private const int ProcessTimeoutMs = 10000;

        /// <summary>
        /// Runs reWASDCommandLine.exe to apply <paramref name="configPath"/>
        /// to <paramref name="slot"/> for device <paramref name="deviceId"/>.
        /// A no-op if any required parameter is blank — callers gate this on
        /// their own "enabled" setting first, this is just a safety net.
        /// Throws on a missing executable, a timeout, or a non-zero exit
        /// code; callers catch and log.
        /// </summary>
        public static void ApplyProfile(string cliPath, string deviceId, string configPath, string slot)
        {
            if (string.IsNullOrEmpty(cliPath) || string.IsNullOrEmpty(deviceId) || string.IsNullOrEmpty(configPath) || string.IsNullOrEmpty(slot))
            {
                return;
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = cliPath,
                Arguments = BuildArguments(deviceId, configPath, slot),
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
            };

            using (var process = Process.Start(startInfo))
            {
                if (process == null)
                {
                    throw new InvalidOperationException("Could not start reWASDCommandLine.exe.");
                }

                if (!process.WaitForExit(ProcessTimeoutMs))
                {
                    throw new TimeoutException("reWASDCommandLine.exe did not respond in time.");
                }

                if (process.ExitCode != 0)
                {
                    var stderr = process.StandardError.ReadToEnd();
                    throw new InvalidOperationException(string.IsNullOrWhiteSpace(stderr)
                        ? $"reWASDCommandLine.exe returned exit code {process.ExitCode}."
                        : $"reWASDCommandLine.exe returned exit code {process.ExitCode}: {stderr.Trim()}");
                }
            }
        }

        /// <summary>Arguments for the "apply" subcommand — split out so the format can be tested without launching a process.</summary>
        public static string BuildArguments(string deviceId, string configPath, string slot)
        {
            return $"apply --id \"{deviceId}\" --path \"{configPath}\" --slot \"{slot}\"";
        }
    }
}
