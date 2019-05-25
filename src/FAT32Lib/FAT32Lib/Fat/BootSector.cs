/*
 * This library (fat32lib.NET) is a port of the library fat32lib obtained from
 * https://android.googlesource.com/platform/external/fat32lib/
 * The original license for this file is replicated below.
 * 
 * Copyright (C) 2003-2009 JNode.org
 *               2009,2010 Matthias Treydte <mt@waldheinz.de>
 *
 * This library is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as published
 * by the Free Software Foundation; either version 2.1 of the License, or
 * (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of MERCHANTABILITY
 * or FITNESS FOR A PARTICULAR PURPOSE. See the GNU Lesser General Public
 * License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public License
 * along with this library; If not, write to the Free Software Foundation, Inc.,
 * 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
 */

using System;
using System.IO;
using System.Text;

namespace FAT32Lib.Fat {

    /**
     * The boot sector.
     *
     * @author Ewout Prangsma &lt;epr at jnode.org&gt;
     * @author Matthias Treydte &lt;waldheinz at gmail.com&gt;
     */
    public abstract class BootSector : Sector {

        /// <summary>
        /// Offset to the byte specifying the number of FATs.
        /// </summary>
        /// <seealso cref="GetNrFats()"/>
        /// <seealso cref="SetNrFats(int)"/>
        public const int FAT_COUNT_OFFSET = 16;
        public const int RESERVED_SECTORS_OFFSET = 14;

        public const int TOTAL_SECTORS_16_OFFSET = 19;
        public const int TOTAL_SECTORS_32_OFFSET = 32;

        /// <summary>
        /// The length of the file system type string.
        /// </summary>
        /// <seealso cref="GetFileSystemTypeLabel()"/>
        public const int FILE_SYSTEM_TYPE_LENGTH = 8;

        /// <summary>
        /// The offset to the sectors per cluster value stored in a boot sector.
        /// </summary>
        /// <seealso cref="GetSectorsPerCluster()"/>
        /// <seealso cref="SetSectorsPerCluster(int)"/>
        public const int SECTORS_PER_CLUSTER_OFFSET = 0x0d;

        public const int EXTENDED_BOOT_SIGNATURE = 0x29;

        /// <summary>
        /// The size of a boot sector in bytes.
        /// </summary>
        public const int SIZE = 512;

        protected BootSector(IBlockDevice device) : base(device, 0, SIZE) {
            MarkDirty();
        }

        public static BootSector Read(IBlockDevice device) {
            byte[] b = new byte[512];
            MemoryStream bb = new MemoryStream(b);
            device.Read(0, bb);

            bb.Position = 510;
            if (bb.ReadByte() != 0x55) throw new IOException(
                 "missing boot sector signature");
            bb.Position = 511;
            if (bb.ReadByte() != 0xaa) throw new IOException(
                 "missing boot sector signature");

            bb.Position = SECTORS_PER_CLUSTER_OFFSET;
            byte sectorsPerCluster = (byte)bb.ReadByte();

            if (sectorsPerCluster <= 0) throw new IOException(
                    "suspicious sectors per cluster count " + sectorsPerCluster);

            byte[] rootDirEntriesB = new byte[2];
            bb.Position = Fat16BootSector.ROOT_DIR_ENTRIES_OFFSET;
            bb.Read(rootDirEntriesB, 0, 2);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(rootDirEntriesB, 0, 2);
            int rootDirEntries = BitConverter.ToUInt16(rootDirEntriesB, 0);

            int rootDirSectors = ((rootDirEntries * 32) +
                    (device.GetSectorSize() - 1)) / device.GetSectorSize();

            byte[] total16B = new byte[2];
            bb.Position = TOTAL_SECTORS_16_OFFSET;
            bb.Read(total16B, 0, 2);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(total16B, 0, 2);
            int total16 = BitConverter.ToUInt16(total16B, 0);

            byte[] total32B = new byte[4];
            bb.Position = TOTAL_SECTORS_32_OFFSET;
            bb.Read(total32B, 0, 4);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(total32B, 0, 4);
            long total32 = BitConverter.ToUInt32(total32B, 0);

            long totalSectors = total16 == 0 ? total32 : total16;

            byte[] fatSz16B = new byte[2];
            bb.Position = Fat16BootSector.SECTORS_PER_FAT_OFFSET;
            bb.Read(fatSz16B, 0, 2);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(fatSz16B, 0, 2);
            int fatSz16 = BitConverter.ToUInt16(fatSz16B, 0);

            byte[] fatSz32B = new byte[4];
            bb.Position = Fat32BootSector.SECTORS_PER_FAT_OFFSET;
            bb.Read(fatSz32B, 0, 4);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(fatSz32B, 0, 4);
            long fatSz32 = BitConverter.ToUInt32(fatSz32B, 0);

            long fatSz = fatSz16 == 0 ? fatSz32 : fatSz16;

            byte[] reservedSectorsB = new byte[2];
            bb.Position = RESERVED_SECTORS_OFFSET;
            bb.Read(reservedSectorsB, 0, 2);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(reservedSectorsB, 0, 2);
            int reservedSectors = BitConverter.ToUInt16(reservedSectorsB, 0);

            bb.Position = FAT_COUNT_OFFSET;
            int fatCount = bb.ReadByte();
            long dataSectors = totalSectors - (reservedSectors +
                    (fatCount * fatSz) + rootDirSectors);

            long clusterCount = dataSectors / sectorsPerCluster;

            BootSector result =
                    (clusterCount > Fat16BootSector.MAX_FAT16_CLUSTERS) ?
                (BootSector)new Fat32BootSector(device) : (BootSector)new Fat16BootSector(device);

            result.Read();
            return result;
        }

        public abstract FatType GetFatType();

        /**
         * Gets the number of sectors per FAT.
         * 
         * @return the sectors per FAT
         */
        public abstract long GetSectorsPerFat();

        /**
         * Sets the number of sectors/fat
         * 
         * @param v  the new number of sectors per fat
         */
        public abstract void SetSectorsPerFat(long v);

        public abstract void SetSectorCount(long count);

        public abstract int GetRootDirEntryCount();

        public abstract long GetSectorCount();

        /**
         * Returns the offset to the file system type label, as this differs
         * between FAT12/16 and FAT32.
         *
         * @return the offset to the file system type label
         */
        public abstract int GetFileSystemTypeLabelOffset();

        public abstract int GetExtendedBootSignatureOffset();

        public virtual void Init() {
            SetBytesPerSector(GetDevice().GetSectorSize());
            SetSectorCount(GetDevice().GetSize() / GetDevice().GetSectorSize());
            Set8(GetExtendedBootSignatureOffset(), EXTENDED_BOOT_SIGNATURE);

            /* magic bytes needed by some windows versions to recognize a boot
             * sector. these are x86 jump instructions which lead into
             * nirvana when executed, but we're currently unable to produce really
             * bootable images anyway. So... */
            Set8(0x00, 0xeb);
            Set8(0x01, 0x3c);
            Set8(0x02, 0x90);

            /* the boot sector signature */
            Set8(0x1fe, 0x55);
            Set8(0x1ff, 0xaa);
        }

        /**
         * Returns the file system type label string.
         *
         * @return the file system type string
         * @see #setFileSystemTypeLabel(java.lang.String)
         * @see #getFileSystemTypeLabelOffset() 
         * @see #FILE_SYSTEM_TYPE_LENGTH
         */
        public string GetFileSystemTypeLabel() {
            StringBuilder sb = new StringBuilder(FILE_SYSTEM_TYPE_LENGTH);

            for (int i = 0; i < FILE_SYSTEM_TYPE_LENGTH; i++) {
                sb.Append((char)Get8(GetFileSystemTypeLabelOffset() + i));
            }

            return sb.ToString();
        }

        /**
         * 
         *
         * @param fsType the
         * @throws IllegalArgumentException if the length of the specified string
         *      does not equal {@link #FILE_SYSTEM_TYPE_LENGTH}
         */
        public void SetFileSystemTypeLabel(string fsType) {

            if (fsType.Length != FILE_SYSTEM_TYPE_LENGTH) {
                throw new ArgumentException();
            }

            for (int i = 0; i < FILE_SYSTEM_TYPE_LENGTH; i++) {
                Set8(GetFileSystemTypeLabelOffset() + i, fsType[i]);
            }
        }

        /**
         * Returns the number of clusters that are really needed to cover the
         * data-caontaining portion of the file system.
         *
         * @return the number of clusters usable for user data
         * @see #getDataSize() 
         */
        public long GetDataClusterCount() {
            return GetDataSize() / GetBytesPerCluster();
        }

        /**
         * Returns the size of the data-containing portion of the file system.
         *
         * @return the number of bytes usable for storing user data
         */
        private long GetDataSize() {
            return (GetSectorCount() * GetBytesPerSector()) -
                    FatUtils.GetFilesOffset(this);
        }

        /**
         * Gets the OEM name
         * 
         * @return String
         */
        public string getOemName() {
            StringBuilder b = new StringBuilder(8);

            for (int i = 0; i < 8; i++) {
                int v = Get8(0x3 + i);
                if (v == 0) break;
                b.Append((char)v);
            }

            return b.ToString();
        }


        /**
         * Sets the OEM name, must be at most 8 characters long.
         *
         * @param name the new OEM name
         */
        public void SetOemName(string name) {
            if (name.Length > 8) throw new ArgumentException(
                    "only 8 characters are allowed");

            for (int i = 0; i < 8; i++) {
                char ch;
                if (i < name.Length) {
                    ch = name[i];
                }
                else {
                    ch = (char)0;
                }

                Set8(0x3 + i, ch);
            }
        }

        /**
         * Gets the number of bytes/sector
         * 
         * @return int
         */
        public int GetBytesPerSector() {
            return Get16(0x0b);
        }

        /**
         * Sets the number of bytes/sector
         * 
         * @param v the new value for bytes per sector
         */
        public void SetBytesPerSector(int v) {
            if (v == GetBytesPerSector()) return;

            switch (v) {
                case 512:
                case 1024:
                case 2048:
                case 4096:
                    Set16(0x0b, v);
                    break;

                default:
                    throw new ArgumentException();
            }
        }

        private static bool IsPowerOfTwo(int n) {
            return ((n != 0) && (n & (n - 1)) == 0);
        }

        /**
         * Returns the number of bytes per cluster, which is calculated from the
         * {@link #getSectorsPerCluster() sectors per cluster} and the
         * {@link #getBytesPerSector() bytes per sector}.
         *
         * @return the number of bytes per cluster
         */
        public int GetBytesPerCluster() {
            return GetSectorsPerCluster() * GetBytesPerSector();
        }

        /**
         * Gets the number of sectors/cluster
         * 
         * @return int
         */
        public int GetSectorsPerCluster() {
            return Get8(SECTORS_PER_CLUSTER_OFFSET);
        }

        /**
         * Sets the number of sectors/cluster
         *
         * @param v the new number of sectors per cluster
         */
        public void SetSectorsPerCluster(int v) {
            if (v == GetSectorsPerCluster()) return;
            if (!IsPowerOfTwo(v)) throw new ArgumentException(
                    "value must be a power of two");

            Set8(SECTORS_PER_CLUSTER_OFFSET, v);
        }

        /**
         * Gets the number of reserved (for bootrecord) sectors
         * 
         * @return int
         */
        public int GetNrReservedSectors() {
            return Get16(RESERVED_SECTORS_OFFSET);
        }

        /**
         * Sets the number of reserved (for bootrecord) sectors
         * 
         * @param v the new number of reserved sectors
         */
        public void SetNrReservedSectors(int v) {
            if (v == GetNrReservedSectors()) return;
            if (v < 1) throw new ArgumentException(
                    "there must be >= 1 reserved sectors");
            Set16(RESERVED_SECTORS_OFFSET, v);
        }

        /**
         * Gets the number of fats
         * 
         * @return int
         */
        public int GetNrFats() {
            return Get8(FAT_COUNT_OFFSET);
        }

        /**
         * Sets the number of fats
         *
         * @param v the new number of fats
         */
        public void SetNrFats(int v) {
            if (v == GetNrFats()) return;

            Set8(FAT_COUNT_OFFSET, v);
        }

        /**
         * Gets the number of logical sectors
         * 
         * @return int
         */
        protected int GetNrLogicalSectors() {
            return Get16(TOTAL_SECTORS_16_OFFSET);
        }

        /**
         * Sets the number of logical sectors
         * 
         * @param v the new number of logical sectors
         */
        protected void SetNrLogicalSectors(int v) {
            if (v == GetNrLogicalSectors()) return;

            Set16(TOTAL_SECTORS_16_OFFSET, v);
        }

        protected void SetNrTotalSectors(long v) {
            Set32(TOTAL_SECTORS_32_OFFSET, v);
        }

        protected long GetNrTotalSectors() {
            return Get32(TOTAL_SECTORS_32_OFFSET);
        }

        /**
         * Gets the medium descriptor byte
         * 
         * @return int
         */
        public int GetMediumDescriptor() {
            return Get8(0x15);
        }

        /**
         * Sets the medium descriptor byte
         * 
         * @param v the new medium descriptor
         */
        public void SetMediumDescriptor(int v) {
            Set8(0x15, v);
        }

        /**
         * Gets the number of sectors/track
         * 
         * @return int
         */
        public int GetSectorsPerTrack() {
            return Get16(0x18);
        }

        /**
         * Sets the number of sectors/track
         *
         * @param v the new number of sectors per track
         */
        public void SetSectorsPerTrack(int v) {
            if (v == GetSectorsPerTrack()) return;

            Set16(0x18, v);
        }

        /**
         * Gets the number of heads
         * 
         * @return int
         */
        public int GetNrHeads() {
            return Get16(0x1a);
        }

        /**
         * Sets the number of heads
         * 
         * @param v the new number of heads
         */
        public void SetNrHeads(int v) {
            if (v == GetNrHeads()) return;

            Set16(0x1a, v);
        }

        /**
         * Gets the number of hidden sectors
         * 
         * @return int
         */
        public long GetNrHiddenSectors() {
            return Get32(0x1c);
        }

        /**
         * Sets the number of hidden sectors
         *
         * @param v the new number of hidden sectors
         */
        public void SetNrHiddenSectors(long v) {
            if (v == GetNrHiddenSectors()) return;

            Set32(0x1c, v);
        }

        public override string ToString() {
            StringBuilder res = new StringBuilder(1024);
            res.Append("Bootsector :\n");
            res.Append("oemName=");
            res.Append(getOemName());
            res.Append('\n');
            res.Append("medium descriptor = ");
            res.Append(GetMediumDescriptor());
            res.Append('\n');
            res.Append("Nr heads = ");
            res.Append(GetNrHeads());
            res.Append('\n');
            res.Append("Sectors per track = ");
            res.Append(GetSectorsPerTrack());
            res.Append('\n');
            res.Append("Sector per cluster = ");
            res.Append(GetSectorsPerCluster());
            res.Append('\n');
            res.Append("byte per sector = ");
            res.Append(GetBytesPerSector());
            res.Append('\n');
            res.Append("Nr fats = ");
            res.Append(GetNrFats());
            res.Append('\n');
            res.Append("Nr hidden sectors = ");
            res.Append(GetNrHiddenSectors());
            res.Append('\n');
            res.Append("Nr logical sectors = ");
            res.Append(GetNrLogicalSectors());
            res.Append('\n');
            res.Append("Nr reserved sector = ");
            res.Append(GetNrReservedSectors());
            res.Append('\n');

            return res.ToString();
        }

    }

}