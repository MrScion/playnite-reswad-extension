using System.Collections.Generic;
using RewasdProfileSwitcher.Core.Rewasd;

namespace RewasdProfileSwitcher.Playnite.Settings
{
    /// <summary>
    /// Persisted settings: a master on/off switch plus zero or more managed
    /// reWASD devices, each with its own default profile and optional
    /// per-Playnite-library profile overrides.
    /// </summary>
    public class RewasdSettings
    {
        public bool IntegrationEnabled { get; set; }

        /// <summary>Path to reWASDCommandLine.exe — one shared CLI install targets every device via its own <c>--id</c>.</summary>
        public string CliPath { get; set; } = "";

        public List<RewasdDeviceSettings> Devices { get; set; } = new List<RewasdDeviceSettings>();
    }
}
