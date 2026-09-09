using System;
using System.Collections.Generic;
using System.Linq;

namespace RewasdProfileSwitcher.Core.Rewasd
{
    /// <summary>
    /// Which action to take for a device when a game starts: apply a
    /// profile, or (when the matching library is configured as passthrough)
    /// turn remap off and leave the real physical device untouched.
    /// </summary>
    public struct RewasdResolvedProfile
    {
        public bool IsPassthrough;
        public string ProfilePath;
        public string ProfileSlot;

        public RewasdResolvedProfile(string profilePath, string profileSlot)
        {
            IsPassthrough = false;
            ProfilePath = profilePath;
            ProfileSlot = profileSlot;
        }

        public static RewasdResolvedProfile Passthrough()
        {
            return new RewasdResolvedProfile { IsPassthrough = true, ProfilePath = "", ProfileSlot = "" };
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
                (p.Passthrough || (!string.IsNullOrEmpty(p.ProfilePath) && !string.IsNullOrEmpty(p.ProfileSlot))));

            if (match == null)
            {
                return new RewasdResolvedProfile(defaultProfilePath, defaultProfileSlot);
            }

            return match.Passthrough
                ? RewasdResolvedProfile.Passthrough()
                : new RewasdResolvedProfile(match.ProfilePath, match.ProfileSlot);
        }
    }
}
