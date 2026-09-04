using System;
using System.IO;
using RewasdProfileSwitcher.Core.Rewasd;
using Xunit;

namespace RewasdProfileSwitcher.Core.Tests.Rewasd
{
    public class RewasdCliControllerTests
    {
        [Fact]
        public void BuildArguments_QuotesEachValue()
        {
            var args = RewasdCliController.BuildArguments("123", @"C:\profiles\a.rewasd", "slot2");
            Assert.Equal("apply --id \"123\" --path \"C:\\profiles\\a.rewasd\" --slot \"slot2\"", args);
        }

        [Theory]
        [InlineData("", "1", "path", "slot1")]
        [InlineData("cli.exe", "", "path", "slot1")]
        [InlineData("cli.exe", "1", "", "slot1")]
        [InlineData("cli.exe", "1", "path", "")]
        [InlineData(null, "1", "path", "slot1")]
        public void ApplyProfile_NoOpWhenAnyRequiredParameterIsBlank(string cliPath, string deviceId, string configPath, string slot)
        {
            // Must not throw and must not attempt to start any process.
            RewasdCliController.ApplyProfile(cliPath, deviceId, configPath, slot);
        }

        [Fact]
        public void ApplyProfile_ThrowsWhenCliExecutableDoesNotExist()
        {
            var missingCli = Path.Combine(Path.GetTempPath(), "RewasdProfileSwitcherTests_missing_" + Guid.NewGuid().ToString("N") + ".exe");
            Assert.ThrowsAny<Exception>(() => RewasdCliController.ApplyProfile(missingCli, "1", @"C:\profile.rewasd", "slot1"));
        }
    }
}
