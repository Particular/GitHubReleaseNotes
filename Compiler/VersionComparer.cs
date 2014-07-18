using System;
using System.Collections.Generic;
using System.Linq;

namespace ReleaseNotesCompiler
{
    class VersionComparer : IComparer<string>
    {
        public static VersionComparer Default = new VersionComparer();

        public int Compare(string x, string y)
        {
            var xver = ToVersion(x);
            var yver = ToVersion(y);

            var result = xver.CompareTo(yver);
            if (result != 0)
                return result;

            var xpre = GetPrereleaseInfo(x);
            var ypre = GetPrereleaseInfo(y);

            if (string.IsNullOrEmpty(xpre) && !string.IsNullOrEmpty(ypre))
                return 1;

            if (!string.IsNullOrEmpty(xpre) && string.IsNullOrEmpty(ypre))
                return -1;

            return StringComparer.OrdinalIgnoreCase.Compare(xpre, ypre);
        }

        private static string GetPrereleaseInfo(string ver)
        {
            var splitVer = ver.Split('-');
            if (splitVer.Length < 2)
                return "";

            return string.Join("-", splitVer.Skip(1));
        }

        private static Version ToVersion(string ver)
        {
            var nameWithoutPrerelease = ver.Split('-')[0];
            Version parsedVersion;

            if (!Version.TryParse(nameWithoutPrerelease, out parsedVersion))
            {
                return new Version(0, 0);
            }

            return parsedVersion;
        }
    }
}