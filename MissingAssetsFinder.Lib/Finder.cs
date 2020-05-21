using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using MissingAssetsFinder.Lib.BSA;
using Mutagen.Bethesda.Skyrim;

namespace MissingAssetsFinder.Lib
{
    public class Finder
    {
        public struct MissingAsset
        {
            public ISkyrimMajorRecordGetter Record;
            public List<string> Files;

            public MissingAsset(ISkyrimMajorRecordGetter record, string path)
            {
                Record = record;
                Files = new List<string>{path};
            }
        }

        private readonly string _dataFolder;
        private readonly List<string> _fileList;
        public readonly List<MissingAsset> MissingAssets;

        public Finder(string dataFolder)
        {
            _dataFolder = dataFolder;
            _fileList = new List<string>();
            MissingAssets = new List<MissingAsset>();
        }

        private static readonly List<string> AllowedExtensions = new List<string>
        {
            ".nif",
            ".dds"
            //".hkx"
        };

        public async Task BuildBSACacheAsync()
        {
            Utils.Log("Start building BSA cache");
            
            var bsaList = await Task.Run(() => Directory.EnumerateFiles(_dataFolder, "*.bsa", SearchOption.TopDirectoryOnly).ToList());
            Utils.Log($"Found {bsaList.Count} BSAs in {_dataFolder}");

            bsaList.Do(async bsa =>
            {
                Utils.Log($"Start parsing BSA {bsa}");
                await using var reader = new BSAReader(bsa);

                var allowedFiles = reader.Files
                    .Where(x => AllowedExtensions.Contains(Path.GetExtension(x.Path)))
                    .Select(x => x.Path)
                    .Where(x => !_fileList.Contains(x))
                    .ToList();

                Utils.Log($"BSA {bsa} has {allowedFiles.Count} allowed files");
                _fileList.AddRange(allowedFiles);

                Utils.Log($"Finished parsing BSA {bsa}");
            });

            Utils.Log($"Finished parsing of {bsaList.Count} BSAs");
        }

        public async Task BuildLooseFileCacheAsync()
        {
            Utils.Log("Start building loose file cache");

            var files = await Task.Run(() =>
                Directory.EnumerateFiles(_dataFolder, "*", SearchOption.AllDirectories)
                    .Where(x => AllowedExtensions.Contains(Path.GetExtension(x)))
                    .Select(x => x.Replace(_dataFolder, ""))
                    .Select(x => x.StartsWith("\\") ? x[1..] : x)
                    .Where(x => !_fileList.Contains(x))
                    .ToList());

            Utils.Log($"Found {files.Count} loose files");

            _fileList.AddRange(files);
        }

        private bool CanAdd(string file)
        {
            return !_fileList.Contains(file) && MissingAssets.All(x => !x.Files.Contains(file));
        }

        private void TryAdd(ISkyrimMajorRecordGetter record, string file)
        {
            if (!CanAdd(file))
                return;

            if (MissingAssets.Any(x => x.Record.Equals(record)))
            {
                MissingAssets.First(x => x.Record.Equals(record)).Files.Add(file);
                return;
            }

            MissingAssets.Add(new MissingAsset(record, file));
        }

        public void FindMissingAssets(string plugin)
        {
            Utils.Log($"Start finding missing assets for {plugin}");

            using var mod = SkyrimMod.CreateFromBinaryOverlay(plugin);

            Utils.Log($"Finished loading plugin {plugin}");

            mod.Armors.Records
                .Do(r =>
            {
                if (r?.WorldModel?.Female?.Model != null && !r.WorldModel.Female.Model.File.IsEmpty())
                {
                    TryAdd(r, r.WorldModel.Female.Model.File);
                }

                if (r?.WorldModel?.Male?.Model != null && !r.WorldModel.Male.Model.File.IsEmpty())
                {
                    TryAdd(r, r.WorldModel.Male.Model.File);
                }
            });

            mod.TextureSets.Records.Do(r =>
            {
                if (!r.Diffuse.IsEmpty())
                {
                    TryAdd(r, r.Diffuse!);
                }

                if (!r.NormalOrGloss.IsEmpty())
                {
                    TryAdd(r, r.NormalOrGloss!);
                }

                if (!r.EnvironmentMaskOrSubsurfaceTint.IsEmpty())
                {
                    TryAdd(r, r.EnvironmentMaskOrSubsurfaceTint!);
                }

                if (!r.GlowOrDetailMap.IsEmpty())
                {
                    TryAdd(r, r.GlowOrDetailMap!);
                }

                if (!r.Height.IsEmpty())
                {
                    TryAdd(r, r.Height!);
                }

                if (!r.Environment.IsEmpty())
                {
                    TryAdd(r, r.Environment!);
                }

                if (!r.Multilayer.IsEmpty())
                {
                    TryAdd(r, r.Multilayer!);
                }

                if (!r.BacklightMaskOrSpecular.IsEmpty())
                {
                    TryAdd(r, r.BacklightMaskOrSpecular!);
                }
            });

            mod.Weapons.Records.Do(r =>
            {
                if (r.Model == null)
                    return;

                TryAdd(r, r.Model.File);
            });

            Utils.Log($"Finished finding missing assets. Found: {MissingAssets.Count} missing assets");
        }
    }
}
