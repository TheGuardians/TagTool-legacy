using TagTool.Cache;
using TagTool.Common;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using static TagTool.Tags.TagFieldFlags;

namespace TagTool.Tags.Definitions.Gen2
{
    [TagStructure(Name = "cellular_automata", Tag = "devo", Size = 0x224)]
    public class CellularAutomata : TagStructure
    {
        public short UpdatesPerSecond; // Hz
        public short XWidth; // cells
        public short YDepth; // cells
        public short ZHeight; // cells
        public float XWidth1; // world units
        public float YDepth1; // world units
        public float ZHeight1; // world units
        [TagField(Length = 0x20, Flags = TagFieldFlags.Padding)]
        public byte[] Padding;
        public StringId Marker;
        public float CellBirthChance; // [0,1]
        [TagField(Length = 0x20, Flags = TagFieldFlags.Padding)]
        public byte[] Padding1;
        public int CellGeneMutates1In; // times
        public int VirusGeneMutations1In; // times
        [TagField(Length = 0x20, Flags = TagFieldFlags.Padding)]
        public byte[] Padding2;
        /// <summary>
        /// the lifespan of a cell once infected
        /// </summary>
        public Bounds<short> InfectedCellLifespan; // updates
        /// <summary>
        /// no cell can be infected before it has been alive this number of updates
        /// </summary>
        public short MinimumInfectionAge; // updates
        [TagField(Length = 0x2, Flags = TagFieldFlags.Padding)]
        public byte[] Padding3;
        public float CellInfectionChance; // [0,1]
        /// <summary>
        /// 0.0 is most difficult for the virus, 1.0 means any virus can infect any cell
        /// </summary>
        public float InfectionThreshold; // [0,1]
        [TagField(Length = 0x20, Flags = TagFieldFlags.Padding)]
        public byte[] Padding4;
        public float NewCellFilledChance; // [0,1]
        public float NewCellInfectedChance; // [0,1]
        [TagField(Length = 0x20, Flags = TagFieldFlags.Padding)]
        public byte[] Padding5;
        public float DetailTextureChangeChance; // [0,1]
        [TagField(Length = 0x20, Flags = TagFieldFlags.Padding)]
        public byte[] Padding6;
        /// <summary>
        /// the number of cells repeating across the detail texture in both dimensions
        /// </summary>
        public short DetailTextureWidth; // cells
        [TagField(Length = 0x2, Flags = TagFieldFlags.Padding)]
        public byte[] Padding7;
        [TagField(ValidTags = new [] { "bitm" })]
        public CachedTag DetailTexture;
        [TagField(Length = 0x20, Flags = TagFieldFlags.Padding)]
        public byte[] Padding8;
        [TagField(ValidTags = new [] { "bitm" })]
        public CachedTag MaskBitmap;
        [TagField(Length = 0xF0, Flags = TagFieldFlags.Padding)]
        public byte[] Padding9;
    }
}

