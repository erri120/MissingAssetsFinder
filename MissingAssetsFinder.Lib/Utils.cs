using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Subjects;
using System.Text;
using JetBrains.Annotations;
using MissingAssetsFinder.Lib.BSA;

namespace MissingAssetsFinder.Lib
{
    public static class Utils
    {
        private static readonly Encoding Windows1252;

        static Utils()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Windows1252 = Encoding.GetEncoding(1252);
        }

        private static readonly Subject<StatusMessage> LoggerSubject = new Subject<StatusMessage>();
        public static IObservable<StatusMessage> LogMessages => LoggerSubject;

        public static void Log(string s)
        {
            LoggerSubject.OnNext(new StatusMessage(s));
            Console.WriteLine(s);
        }

        public static bool IsEmpty(this string? s)
        {
            return string.IsNullOrEmpty(s);
        }

        public static void Do<T>(this IEnumerable<T> enumerable, [InstantHandle] Action<T> action)
        {
            foreach (var item in enumerable) action(item);
        }

        private static Encoding GetEncoding(VersionType version)
        {
            if (version == VersionType.TES3)
                return Encoding.ASCII;
            if (version == VersionType.SSE)
                return Windows1252;
            return Encoding.UTF7;
        }

        public static string ReadStringLen(this BinaryReader rdr, VersionType version)
        {
            var len = rdr.ReadByte();
            if (len == 0)
                //rdr.ReadByte();
                return "";

            var bytes = rdr.ReadBytes(len - 1);
            rdr.ReadByte();
            return GetEncoding(version).GetString(bytes);
        }

        public static string ReadStringLenNoTerm(this BinaryReader rdr, VersionType version)
        {
            var len = rdr.ReadByte();
            var bytes = rdr.ReadBytes(len);
            return GetEncoding(version).GetString(bytes);
        }

        public static string ReadStringTerm(this BinaryReader rdr, VersionType version)
        {
            var acc = new List<byte>();
            while (true)
            {
                var c = rdr.ReadByte();

                if (c == '\0') break;

                acc.Add(c);
            }

            return GetEncoding(version).GetString(acc.ToArray());
        }
    }
}
