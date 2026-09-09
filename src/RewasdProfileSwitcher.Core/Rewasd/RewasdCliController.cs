using System;
using System.Diagnostics;

namespace RewasdProfileSwitcher.Core.Rewasd
{
    /// <summary>
    /// Drives reWASDCommandLine.exe: applying a profile to a slot
    /// ("apply --id &lt;deviceId&gt; --path &lt;configPath&gt; --slot &lt;slot&gt;"), and
    /// toggling remap on/off for a device ("remap --id &lt;deviceId&gt;
    /// --state on|off") — remap off releases the virtual controller and
    /// restores the real physical device to Windows (see
    /// <see cref="SetRemapState"/>).
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

            RunCli(cliPath, BuildArguments(deviceId, configPath, slot));
        }

        /// <summary>
        /// Runs reWASDCommandLine.exe to turn remap on or off for
        /// <paramref name="deviceId"/>. With remap off, reWASD stops hiding
        /// the physical device and removes the virtual controller — the raw
        /// device becomes visible to Windows/other apps (e.g. Steam Input)
        /// exactly as it identifies itself, with none of reWASD's mapping
        /// applied. A no-op if any required parameter is blank; throws on a
        /// missing executable, a timeout, or a non-zero exit code — callers
        /// catch and log, same as <see cref="ApplyProfile"/>.
        /// </summary>
        public static void SetRemapState(string cliPath, string deviceId, bool enabled)
        {
            if (string.IsNullOrEmpty(cliPath) || string.IsNullOrEmpty(deviceId))
            {
                return;
            }

            RunCli(cliPath, BuildRemapArguments(deviceId, enabled));
        }

        /// <summary>Arguments for the "apply" subcommand — split out so the format can be tested without launching a process.</summary>
        public static string BuildArguments(string deviceId, string configPath, string slot)
        {
            return $"apply --id \"{deviceId}\" --path \"{configPath}\" --slot \"{slot}\"";
        }

        /// <summary>Arguments for the "remap" subcommand — split out so the format can be tested without launching a process.</summary>
        public static string BuildRemapArguments(string deviceId, bool enabled)
        {
            return $"remap --id \"{deviceId}\" --state {(enabled ? "on" : "off")}";
        }

        private static void RunCli(string cliPath, string arguments)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = cliPath,
                Arguments = arguments,
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
    }
}
