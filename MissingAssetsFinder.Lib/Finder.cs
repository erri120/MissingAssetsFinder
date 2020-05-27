using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JetBrains.Annotations;
using MissingAssetsFinder.Lib.BSA;
using Mutagen.Bethesda;
using Mutagen.Bethesda.Skyrim;
using Noggog;

namespace MissingAssetsFinder.Lib
{
    public struct MissingAsset
    {
        public ISkyrimMajorRecordGetter Record { get; set; }
        public HashSet<string> Files { get; set; }

        public MissingAsset(ISkyrimMajorRecordGetter record, string path)
        {
            Record = record;
            Files = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { path };
        }
    }

    public class Finder
    {
        private readonly string _dataFolder;
        private readonly HashSet<string> _fileSet;
        public readonly List<MissingAsset> MissingAssets;

        public Finder(string dataFolder)
        {
            _dataFolder = dataFolder;
            _fileSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            MissingAssets = new List<MissingAsset>();
        }

        private static readonly List<string> AllowedExtensions = new List<string>
        {
            ".nif",
            ".dds",
            ".tri"
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
                    .Where(x => !_fileSet.Contains(x))
                    .ToList();

                Utils.Log($"BSA {bsa} has {allowedFiles.Count} allowed files");
                allowedFiles.Do(f => _fileSet.Add(f));

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
                    .Where(x => !_fileSet.Contains(x))
                    .ToList());

            Utils.Log($"Found {files.Count} loose files");

            files.Do(f => _fileSet.Add(f));
        }

        private bool CanAdd(string file)
        {
            return !_fileSet.Contains(file) && MissingAssets.All(x => !x.Files.Contains(file));
        }

        private static string ToDataString(string s)
        {
            if (s.StartsWith("\\"))
                s = s.Substring(1, s.Length);

            if (s.EndsWith(".nif") || s.EndsWith(".tri"))
                return $"meshes\\{s}";

            if (s.EndsWith(".dds"))
                return $"textures\\{s}";

            return s;
        }

        private void TryAdd(ISkyrimMajorRecordGetter record, string? file)
        {
            if (file == null) return;
            if (_blackList.Contains(record.FormKey.ID))
                return;
            // ToLower is just for clean visualization
            file = file.ToLower();
            file = ToDataString(file);
            if (!CanAdd(file))
                return;

            if (MissingAssets.Any(x => x.Record.Equals(record)))
            {
                MissingAssets.First(x => x.Record.Equals(record)).Files.Add(file);
                return;
            }

            MissingAssets.Add(new MissingAsset(record, file));
        }

        /*private List<uint> KnownRaces = new List<uint>
        {
            0x13740, //ArgonianRace
            0x13741, //BretonRace
            0x13742, //DarkElfRace
            0x13743, //HighElfRace
            0x13744, //ImperialRace
            0x13745, //KhajitRace
            0x13746, //NordRace
            0x13747, //OrcRace
            0x13748, //ReguardRace
            0x13749, //WoodElfRace

            0x2C659, //ImperialRaceChild
            0x2C65A, //RedguardRaceChild
            0x2C65B, //NordRaceChild
            0x2C65C, //BretonRaceChild
            
            //0x7EAF3, //NordRaceAstrid

            0x88794, //NordRaceVampire
            0x888CA, //ArgonianRaceVampire
            0x8883C, //BretonRaceVampire
            0x8883D, //DarkElfRaceVampire
            0x88840, //HighElfRaceVampire
            0x88844, //ImperialRaceVampire
            0x88845, //KhajitRaceVampire
            0x88846, //ReguardRaceVampire
            0x88884, //WoodElfRaceVampire
        };*/

        private readonly List<uint> _blackList = new List<uint>
        {
            0x7 //Player
        };

        public void FindMissingAssets(bool useLoadOrder)
        {
            if (!useLoadOrder)
                return;

            var path = Path.Combine(Environment.GetEnvironmentVariable("LocalAppData") ?? string.Empty,
                "Skyrim Special Edition", "loadorder.txt");
            Utils.Log($"Using loadorder.txt at {path}");
            var mods = LoadOrder.ProcessLoadOrder(path);
            Utils.Log($"Finished finding missing assets for {mods.Count} mods");

            MissingAssets.Sort((x, y) => FormKey.LoadOrderComparer(mods).Compare(x.Record.FormKey, y.Record.FormKey));
        }

        public void FindMissingAssets(string plugin)
        {
            Utils.Log($"Start finding missing assets for {plugin}");

            using var mod = SkyrimMod.CreateFromBinaryOverlay(plugin);

            Utils.Log($"Finished loading plugin {plugin}");

            FindMissingAssets(mod);
        }

        public void FindMissingAssets(ISkyrimModDisposableGetter mod, ILinkCache? linkCache = null)
        {
            mod.Armors.Records.NotNull().Do(r =>
            {
                TryAdd(r, r.WorldModel?.Female?.Model?.File);
                TryAdd(r, r.WorldModel?.Male?.Model?.File);
            });

            mod.TextureSets.Records.Do(r =>
            {
                TryAdd(r, r.Diffuse);
                TryAdd(r, r.NormalOrGloss);
                TryAdd(r, r.EnvironmentMaskOrSubsurfaceTint);
                TryAdd(r, r.GlowOrDetailMap);
                TryAdd(r, r.Height);
                TryAdd(r, r.Environment);
                TryAdd(r, r.Multilayer);
                TryAdd(r, r.BacklightMaskOrSpecular);
            });

            mod.Weapons.Records
                .Do(r =>
                {
                    TryAdd(r, r.Model?.File);
                });

            mod.Statics.Records
                .Do(r =>
                {
                    TryAdd(r, r.Model?.File);
                });

            mod.HeadParts.Records.Do(r =>
            {
                TryAdd(r, r.Model?.File);

                r.Parts
                    .Do(p =>
                    {
                        TryAdd(r, p.FileName);
                    });
            });

            mod.Npcs.Records.Do(r =>
            {
                if (r.Configuration.Flags.HasFlag(NpcConfiguration.Flag.IsCharGenFacePreset))
                    return;

                if (r.TintLayers == null || r.TintLayers.Count == 0)
                {
                    if (r.Race.TryResolve(linkCache ?? mod.CreateLinkCache(), out var race))
                    {
                        if (!race.Flags.HasFlag(Race.Flag.FaceGenHead))
                            return;
                    }
                    else
                    {
                        Utils.Log(r.Race.TryGetModKey(out var modKey)
                            ? $"Unable to resolve race {r.Race.FormKey} from {modKey} of NPC_ {r.FormKey} from {mod.ModKey}"
                            : $"Unable to resolve race {r.Race.FormKey} of NPC_ {r.FormKey} from {mod.ModKey}");
                    }
                }

                TryAdd(r, $"actors\\character\\facegendata\\facegeom\\{r.FormKey.ModKey.FileName}\\{r.FormKey.ID:x8}.nif");
                TryAdd(r, $"actors\\character\\facegendata\\facetint\\{r.FormKey.ModKey.FileName}\\{r.FormKey.ID:x8}.dds");
            });

            Utils.Log($"Finished finding missing assets for {mod.ModKey}. Found: {MissingAssets.Count} missing assets");
        }
    }
}
