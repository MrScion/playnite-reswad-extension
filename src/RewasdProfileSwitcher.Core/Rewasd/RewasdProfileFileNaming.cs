using System;
using System.IO;

namespace RewasdProfileSwitcher.Core.Rewasd
{
    /// <summary>
    /// Builds a friendly picker label for a <c>.rewasd</c> profile file found
    /// on disk, based on reWASD's own profile folder layout:
    /// <c>&lt;ProfilesFolder&gt;\Profiles\&lt;ProfileName&gt;\Controller\&lt;file&gt;.rewasd</c>
    /// (a background image for the profile sits next to the Controller
    /// folder, at the <c>&lt;ProfileName&gt;</c> level — this switcher has no
    /// use for it). Pure path logic only, so it can be unit tested; the
    /// actual directory scan lives in the Playnite project's
    /// <c>RewasdProfileFileScanner</c> (real I/O, not testable without files
    /// on disk — same reasoning as GamepadDetector).
    /// </summary>
    public static class RewasdProfileFileNaming
    {
        public static string BuildDisplayName(string rewasdFilePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(rewasdFilePath ?? "");
            var controllerDir = Path.GetDirectoryName(rewasdFilePath);
            var controllerDirName = controllerDir != null ? Path.GetFileName(controllerDir) : null;

            if (!string.Equals(controllerDirName, "Controller", StringComparison.OrdinalIgnoreCase))
            {
                return fileName;
            }

            var profileDir = Path.GetDirectoryName(controllerDir);
            var profileName = profileDir != null ? Path.GetFileName(profileDir) : null;

            if (string.IsNullOrEmpty(profileName) || string.Equals(profileName, fileName, StringComparison.OrdinalIgnoreCase))
            {
                return fileName;
            }

            return $"{profileName} — {fileName}";
        }
    }
}
