using MissingAssetsFinder.Lib;
using Xunit;

namespace MissingAssetsFinder.Test
{
    public class FinderTest
    {
        [Fact]
        public async void TestFinder()
        {
            var dataFolder = "N:\\SteamLibrary\\steamapps\\common\\Skyrim Special Edition\\Data";

            var finder = new Finder(dataFolder);

            await finder.BuildBSACacheAsync();
            await finder.BuildLooseFileCacheAsync();

            finder.FindMissingAssets("N:\\SteamLibrary\\steamapps\\common\\Skyrim Special Edition\\Data\\Judgment Wenches.esp");
        }
    }
}
