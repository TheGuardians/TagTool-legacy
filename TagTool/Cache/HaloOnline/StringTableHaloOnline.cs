﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TagTool.Common;
using TagTool.IO;

namespace TagTool.Cache.HaloOnline
{
    public class StringTableHaloOnline : StringTable
    {
        public FileInfo StringIdCacheFile;
        public FileStream OpenStringIdCacheRead() => StringIdCacheFile.OpenRead();
        public FileStream OpenStringIdCacheWrite() => StringIdCacheFile.Open(FileMode.Open, FileAccess.Write);
        public FileStream OpenStringIdCacheReadWrite() => StringIdCacheFile.Open(FileMode.Open, FileAccess.ReadWrite);

        public StringTableHaloOnline(CacheVersion version, DirectoryInfo directory)
        {
            Version = version;

            var files = directory.GetFiles("string_ids.dat");
            if (files.Length == 0)
                throw new FileNotFoundException(Path.Combine(directory.FullName, "string_ids.dat"));
            StringIdCacheFile = files[0];


            Resolver = null;

            if (CacheVersionDetection.Compare(Version, CacheVersion.HaloOnline700123) >= 0)
                Resolver = new StringIdResolverMS30();
            else if (CacheVersionDetection.Compare(Version, CacheVersion.HaloOnline498295) >= 0)
                Resolver = new StringIdResolverMS28();
            else
                Resolver = new StringIdResolverMS23();

            using (var stream = OpenStringIdCacheRead())
            {
                if (stream.Length != 0)
                    Load(stream);
                else
                    Clear();
            }
        }

        public override StringId AddString(string newString)
        {
            var strIndex = Count;
            Add(newString);
            return GetStringId(strIndex);
        }

        public override void Save()
        {
            using (var stream = OpenStringIdCacheReadWrite())
            {
                Save(stream);
            }
        }

        public void Save(Stream stream)
        {
            var writer = new EndianWriter(stream, EndianFormat.LittleEndian);

            // Write the string count and then skip over the offset table, because it will be filled in last
            writer.Write(Count);
            writer.BaseStream.Position += 4 + Count * 4; // 4 byte data size + 4 bytes per string offset

            // Write string data and keep track of offsets
            var stringOffsets = new int[Count];
            var dataOffset = (int)writer.BaseStream.Position;
            var currentOffset = 0;
            for (var i = 0; i < Count; i++)
            {
                var str = this[i];
                if (str == null)
                {
                    // Null string - set offset to -1
                    stringOffsets[i] = -1;
                    continue;
                }

                // Write the string as null-terminated ASCII
                stringOffsets[i] = currentOffset;
                var data = Encoding.ASCII.GetBytes(str);
                writer.Write(data, 0, data.Length);
                writer.Write((byte)0);
                currentOffset += data.Length + 1;
            }

            // Now go back and write the string offsets
            writer.BaseStream.Position = 0x4;
            writer.Write(currentOffset); // Data size
            foreach (var offset in stringOffsets)
                writer.Write(offset);
            writer.BaseStream.SetLength(dataOffset + currentOffset);
        }

        private void Load(Stream stream)
        {
            var reader = new EndianReader(stream, EndianFormat.LittleEndian);

            // Read the header
            var stringCount = reader.ReadInt32();  // int32 string count
            var dataSize = reader.ReadInt32();     // int32 string data size

            // Read the string offsets into a list of (index, offset) pairs, and then sort by offset
            // This lets us know the length of each string without scanning for a null terminator
            var stringOffsets = new List<Tuple<int, int>>(stringCount);
            for (var i = 0; i < stringCount; i++)
            {
                var offset = reader.ReadInt32();
                if (offset >= 0 && offset < dataSize)
                    stringOffsets.Add(Tuple.Create(i, offset));
            }
            stringOffsets.Sort((x, y) => x.Item2 - y.Item2);

            // Seek to each offset and read each string
            var dataOffset = reader.BaseStream.Position;
            var strings = new string[stringCount];
            for (var i = 0; i < stringOffsets.Count; i++)
            {
                var index = stringOffsets[i].Item1;
                var offset = stringOffsets[i].Item2;
                var nextOffset = (i < stringOffsets.Count - 1) ? stringOffsets[i + 1].Item2 : dataSize;
                var length = Math.Max(0, nextOffset - offset - 1); // Subtract 1 for null terminator
                reader.BaseStream.Position = dataOffset + offset;
                strings[index] = Encoding.ASCII.GetString(reader.ReadBytes(length));
            }
            Clear();
            AddRange(strings.ToList());
        }
    }
}
