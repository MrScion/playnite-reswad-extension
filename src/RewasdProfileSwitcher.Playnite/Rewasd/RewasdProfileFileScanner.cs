using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RewasdProfileSwitcher.Core.Rewasd;

namespace RewasdProfileSwitcher.Playnite.Rewasd
{
    /// <summary>
    /// Recursively finds every .rewasd file under a configured reWASD
    /// profiles folder, for the profile pickers in Settings (see
    /// RewasdSettingsViewModel.PickProfileFile). Real directory I/O, so —
    /// same reasoning as GamepadDetector/RewasdCliController — this isn't
    /// unit tested; the one piece of actual logic (turning a file path into
    /// a friendly label) lives in the tested RewasdProfileFileNaming
    /// instead. Never throws: an unconfigured/missing folder or an
    /// access-denied subfolder just yields fewer (or zero) results, which
    /// falls back to manual file browsing in the caller.
    /// </summary>
    public static class RewasdProfileFileScanner
    {
        public static List<RewasdProfileFile> Scan(string profilesFolder)
        {
            var result = new List<RewasdProfileFile>();
            if (string.IsNullOrEmpty(profilesFolder) || !Directory.Exists(profilesFolder))
            {
                return result;
            }

            try
            {
                foreach (var file in Directory.EnumerateFiles(profilesFolder, "*.rewasd", SearchOption.AllDirectories))
                {
                    result.Add(new RewasdProfileFile(file, RewasdProfileFileNaming.BuildDisplayName(file)));
                }
            }
            catch
            {
                // Best-effort only — e.g. access denied on a subfolder.
            }

            return result.OrderBy(f => f.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();
        }
    }
}
