using System;
using System.Collections.Generic;

namespace RewasdProfileSwitcher.Core.Rewasd
{
    /// <summary>
    /// One reWASD-managed gamepad: its device id, the profile applied when
    /// no game is running (or as a fallback for a game whose library has no
    /// override), and optional per-Playnite-library overrides applied on
    /// game start.
    /// </summary>
    public class RewasdDeviceSettings
    {
        /// <summary>Stable identity for the settings UI list (add/remove/select) — not passed to reWASD.</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        public string DisplayName { get; set; } = "";

        /// <summary>reWASD device id ("Copy device ID" in reWASD).</summary>
        public string DeviceId { get; set; } = "";

        /// <summary>Applied when any game closes, and as the start-time fallback for a library with no override below.</summary>
        public string DefaultProfilePath { get; set; } = "";
        public string DefaultProfileSlot { get; set; } = "slot1";

        public List<RewasdLibraryProfile> LibraryProfiles { get; set; } = new List<RewasdLibraryProfile>();
    }
}
