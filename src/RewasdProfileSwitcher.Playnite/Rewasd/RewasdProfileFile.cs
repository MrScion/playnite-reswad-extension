namespace RewasdProfileSwitcher.Playnite.Rewasd
{
    /// <summary>One .rewasd profile file found under the configured profiles folder — see RewasdProfileFileScanner.</summary>
    public sealed class RewasdProfileFile
    {
        public string FilePath { get; }
        public string DisplayName { get; }

        public RewasdProfileFile(string filePath, string displayName)
        {
            FilePath = filePath;
            DisplayName = displayName;
        }
    }
}
