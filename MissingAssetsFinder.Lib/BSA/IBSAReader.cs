using System;
using System.Collections.Generic;

namespace MissingAssetsFinder.Lib.BSA
{
    public interface IBSAReader : IAsyncDisposable
    {
        IEnumerable<IFile> Files { get; }
    }

    public class ArchiveStateObject
    {

    }

    public class FileStateObject
    {
        public int Index { get; set; }
        public string Path { get; set; } = string.Empty;
    }

    public interface IFile
    {
        string Path { get; }

        uint Size { get; }

        //FileStateObject State { get; }
    }
}
