using System.IO;

namespace NsoServer.Net
{
    // Static payloads captured byte-for-byte from the original server (version/data/map/skill/item .bin files).
    public sealed class FileStaticData : IStaticData
    {
        public byte[] Version { get; private set; }
        public byte[] Data { get; private set; }
        public byte[] Map { get; private set; }
        public byte[] Skill { get; private set; }
        public byte[] Item { get; private set; }

        public FileStaticData(string directory)
        {
            Version = File.ReadAllBytes(Path.Combine(directory, "version.bin"));
            Data = File.ReadAllBytes(Path.Combine(directory, "data.bin"));
            Map = File.ReadAllBytes(Path.Combine(directory, "map.bin"));
            Skill = File.ReadAllBytes(Path.Combine(directory, "skill.bin"));
            Item = File.ReadAllBytes(Path.Combine(directory, "item.bin"));
        }
    }
}
