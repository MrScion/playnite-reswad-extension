using System;
using System.Collections.Generic;
using RewasdProfileSwitcher.Core.Rewasd;
using Xunit;

namespace RewasdProfileSwitcher.Core.Tests.Rewasd
{
    public class RewasdProfileResolverTests
    {
        private static readonly Guid SteamLibraryId = Guid.Parse("cb91dfc9-b977-43bf-8e70-55f46e410fab");
        private static readonly Guid TrleLibraryId = Guid.Parse("de0fac0d-1372-4e71-a453-09f0c6b4201f");
        private static readonly Guid UnmappedLibraryId = Guid.NewGuid();

        private static List<RewasdLibraryProfile> SampleProfiles()
        {
            return new List<RewasdLibraryProfile>
            {
                new RewasdLibraryProfile { LibraryPluginId = SteamLibraryId, ProfilePath = @"C:\steam.rewasd", ProfileSlot = "slot2" },
                new RewasdLibraryProfile { LibraryPluginId = TrleLibraryId, ProfilePath = @"C:\trle.rewasd", ProfileSlot = "slot3" },
            };
        }

        [Fact]
        public void ResolveStartProfile_UsesLibraryOverrideWhenGamePluginIdMatches()
        {
            var result = RewasdProfileResolver.ResolveStartProfile(
                SteamLibraryId, @"C:\default.rewasd", "slot1", SampleProfiles());

            Assert.Equal(@"C:\steam.rewasd", result.ProfilePath);
            Assert.Equal("slot2", result.ProfileSlot);
        }

        [Fact]
        public void ResolveStartProfile_FallsBackToDefaultWhenNoLibraryMatches()
        {
            var result = RewasdProfileResolver.ResolveStartProfile(
                UnmappedLibraryId, @"C:\default.rewasd", "slot1", SampleProfiles());

            Assert.Equal(@"C:\default.rewasd", result.ProfilePath);
            Assert.Equal("slot1", result.ProfileSlot);
        }

        [Fact]
        public void ResolveStartProfile_FallsBackToDefaultWhenLibraryListIsNull()
        {
            var result = RewasdProfileResolver.ResolveStartProfile(
                SteamLibraryId, @"C:\default.rewasd", "slot1", null);

            Assert.Equal(@"C:\default.rewasd", result.ProfilePath);
            Assert.Equal("slot1", result.ProfileSlot);
        }

        [Fact]
        public void ResolveStartProfile_FallsBackToDefaultWhenMatchingEntryHasBlankPath()
        {
            var profiles = new List<RewasdLibraryProfile>
            {
                new RewasdLibraryProfile { LibraryPluginId = SteamLibraryId, ProfilePath = "", ProfileSlot = "slot2" },
            };

            var result = RewasdProfileResolver.ResolveStartProfile(
                SteamLibraryId, @"C:\default.rewasd", "slot1", profiles);

            Assert.Equal(@"C:\default.rewasd", result.ProfilePath);
            Assert.Equal("slot1", result.ProfileSlot);
        }

        [Fact]
        public void ResolveStartProfile_PicksCorrectLibraryOutOfMultiple()
        {
            var result = RewasdProfileResolver.ResolveStartProfile(
                TrleLibraryId, @"C:\default.rewasd", "slot1", SampleProfiles());

            Assert.Equal(@"C:\trle.rewasd", result.ProfilePath);
            Assert.Equal("slot3", result.ProfileSlot);
        }
    }
}
