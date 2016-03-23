using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace StardewModdingAPI
{
    public struct Version
    {
        public int MajorVersion { get; set; }
        public int MinorVersion { get; set; }
        public int PatchVersion { get; set; }
        public string Build { get; set; }

        [JsonIgnore]
        public string VersionString => $"{MajorVersion}.{MinorVersion}.{PatchVersion} {Build}";

        public Version(int major, int minor, int patch, string build)
        {
            MajorVersion = major;
            MinorVersion = minor;
            PatchVersion = patch;
            Build = build;
        }
    }
}
