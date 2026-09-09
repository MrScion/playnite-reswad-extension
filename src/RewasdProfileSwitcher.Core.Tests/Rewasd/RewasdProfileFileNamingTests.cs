using RewasdProfileSwitcher.Core.Rewasd;
using Xunit;

namespace RewasdProfileSwitcher.Core.Tests.Rewasd
{
    public class RewasdProfileFileNamingTests
    {
        [Fact]
        public void BuildDisplayName_StandardLayout_CombinesProfileAndFileName()
        {
            var result = RewasdProfileFileNaming.BuildDisplayName(@"C:\ReWASD\Profiles\Fortnite\Controller\Double movement.rewasd");
            Assert.Equal("Fortnite — Double movement", result);
        }

        [Fact]
        public void BuildDisplayName_ControllerFolderMatchIsCaseInsensitive()
        {
            var result = RewasdProfileFileNaming.BuildDisplayName(@"C:\ReWASD\Profiles\Fortnite\controller\Double movement.rewasd");
            Assert.Equal("Fortnite — Double movement", result);
        }

        [Fact]
        public void BuildDisplayName_ProfileNameSameAsFileName_ReturnsFileNameOnly()
        {
            var result = RewasdProfileFileNaming.BuildDisplayName(@"C:\ReWASD\Profiles\Fortnite\Controller\Fortnite.rewasd");
            Assert.Equal("Fortnite", result);
        }

        [Fact]
        public void BuildDisplayName_NotInsideControllerFolder_ReturnsFileNameOnly()
        {
            var result = RewasdProfileFileNaming.BuildDisplayName(@"C:\ReWASD\Profiles\Fortnite\Fortnite.rewasd");
            Assert.Equal("Fortnite", result);
        }

        [Fact]
        public void BuildDisplayName_NoParentFolders_ReturnsFileNameOnly()
        {
            var result = RewasdProfileFileNaming.BuildDisplayName(@"C:\Double movement.rewasd");
            Assert.Equal("Double movement", result);
        }
    }
}
