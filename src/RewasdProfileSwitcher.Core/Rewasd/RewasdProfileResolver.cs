using System;
using System.Collections.Generic;
using System.Linq;

namespace RewasdProfileSwitcher.Core.Rewasd
{
    /// <summary>Which (path, slot) to apply for a device, resolved from its per-library overrides and default.</summary>
    public struct RewasdResolvedProfile
    {
        public string ProfilePath;
        public string ProfileSlot;

        public RewasdResolvedProfile(string profilePath, string profileSlot)
        {
            ProfilePath = profilePath;
            ProfileSlot = profileSlot;
        }
    }

    /// <summary>
    /// Picks which profile a device should switch to when a game starts:
    /// the library-specific override if one is configured for that game's
    /// library, otherwise the device's default.
    /// </summary>
    public static class RewasdProfileResolver
    {
        public static RewasdResolvedProfile ResolveStartProfile(
            Guid gamePluginId,
            string defaultProfilePath,
            string defaultProfileSlot,
            IEnumerable<RewasdLibraryProfile> libraryProfiles)
        {
            var match = libraryProfiles?.FirstOrDefault(p =>
                p.LibraryPluginId == gamePluginId &&
                !string.IsNullOrEmpty(p.ProfilePath) &&
                !string.IsNullOrEmpty(p.ProfileSlot));

            return match != null
                ? new RewasdResolvedProfile(match.ProfilePath, match.ProfileSlot)
                : new RewasdResolvedProfile(defaultProfilePath, defaultProfileSlot);
        }
    }
}
