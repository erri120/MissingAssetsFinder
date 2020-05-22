using System.Collections.Generic;
using DynamicData.Binding;
using MissingAssetsFinder.Lib;

namespace MissingAssetsFinder
{
    public class ResultsWindowVM : ViewModel
    {
        //public List<MissingAsset> MissingAssets;

        public ObservableCollectionExtended<MissingAsset> MissingAssets;

        public ResultsWindowVM(IEnumerable<MissingAsset> missingAssets)
        {
            MissingAssets = new ObservableCollectionExtended<MissingAsset>(missingAssets);
        }
    }
}
