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

        /// <summary>reWASD's install folder — used only to auto-detect <see cref="CliPath"/> below; not read by the CLI controller itself.</summary>
        public string InstallFolder { get; set; } = @"C:\Program Files\reWASD";

        /// <summary>Path to reWASDCommandLine.exe — one shared CLI install targets every device via its own <c>--id</c>.</summary>
        public string CliPath { get; set; } = "";

        /// <summary>
        /// Folder holding reWASD's own <c>Profiles\&lt;name&gt;\Controller\*.rewasd</c>
        /// layout — used only to offer a picker of found .rewasd files when
        /// choosing a device's default profile or a per-library override;
        /// never read outside Settings. No fixed default (unlike
        /// <see cref="InstallFolder"/>) since reWASD doesn't put this in a
        /// single well-known location.
        /// </summary>
        public string ProfilesFolder { get; set; } = "";

        public List<RewasdDeviceSettings> Devices { get; set; } = new List<RewasdDeviceSettings>();
    }
}
