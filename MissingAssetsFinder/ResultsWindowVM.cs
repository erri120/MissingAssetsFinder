using System.Collections.Generic;
using MissingAssetsFinder.Lib;

namespace MissingAssetsFinder
{
    public class ResultsWindowVM : ViewModel
    {
        public List<MissingAsset> MissingAssets;

        public ResultsWindowVM(List<MissingAsset> missingAssets)
        {
            MissingAssets = missingAssets;
        }
    }
}
