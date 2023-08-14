using System;

namespace RockSweeper.Utility
{
    public class BinaryFile
    {
        public int Id { get; set; }

        public Guid Guid { get; set; }

        public string FileName { get; set; }

        public string MimeType { get; set; }

        public long? FileSize { get; set; }

        public int? Width { get; set; }

        public int? Height { get; set; }
    }
}
