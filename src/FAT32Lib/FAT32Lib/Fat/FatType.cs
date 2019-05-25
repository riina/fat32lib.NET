/*
 * This library (fat32lib.NET) is a port of the library fat32lib obtained from
 * https://android.googlesource.com/platform/external/fat32lib/
 * The original license for this file is replicated below.
 * 
 * Copyright (C) 2009,2010 Matthias Treydte <mt@waldheinz.de>
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


using System.Collections.Generic;

namespace FAT32Lib.Fat {
    /// <summary>
    /// Enumerates the different entry sizes of 12, 16 and 32 bits for the different
    /// FAT flavours.
    /// </summary>
    public abstract class FatType {

        public static readonly FatType BASE_FAT12 = new FAT12();
        public static readonly FatType BASE_FAT16 = new FAT16();
        public static readonly FatType BASE_FAT32 = new FAT32();

        public static IEnumerable<FatType> Values {
            get {
                yield return BASE_FAT12;
                yield return BASE_FAT16;
                yield return BASE_FAT32;
            }
        }

        private readonly long minReservedEntry;
        private readonly long maxReservedEntry;
        private readonly long eofCluster;
        private readonly long eofMarker;
        private readonly long bitMask;
        private readonly int maxClusters;
        private readonly string label;
        private readonly float entrySize;

        private FatType(int maxClusters,
            long bitMask, float entrySize, string label) {

            minReservedEntry = (0xFFFFFF0L & bitMask);
            maxReservedEntry = (0xFFFFFF6L & bitMask);
            eofCluster = (0xFFFFFF8L & bitMask);
            eofMarker = (0xFFFFFFFL & bitMask);
            this.entrySize = entrySize;
            this.label = label;
            this.maxClusters = maxClusters;
            this.bitMask = bitMask;
        }

        internal abstract long ReadEntry(byte[] data, int index);

        internal abstract void WriteEntry(byte[] data, int index, long entry);

        /// <summary>
        /// Returns the maximum number of clusters this file system can address.
        /// </summary>
        /// <returns>the maximum cluster count supported</returns>
        internal long MaxClusters() {
            return maxClusters;
        }

        /// <summary>
        /// Returns the human-readable FAT name string as written to the
        /// <see cref="BootSector"/>.
        /// </summary>
        /// <returns>the boot sector label for this FAT type</returns>
        internal string GetLabel() {
            return label;
        }

        internal bool IsReservedCluster(long entry) {
            return ((entry >= minReservedEntry) && (entry <= maxReservedEntry));
        }

        internal bool IsEofCluster(long entry) {
            return (entry >= eofCluster);
        }

        internal long GetEofMarker() {
            return eofMarker;
        }

        internal float GetEntrySize() {
            return entrySize;
        }

        internal long GetBitMask() {
            return bitMask;
        }

        /// <summary>
        /// Represents a 12-bit file allocation table.
        /// </summary>
        internal sealed class FAT12 : FatType {
            internal FAT12() : base((1 << 12) - 16, 0xFFFL, 1.5f, "FAT12   ") { }

            internal override long ReadEntry(byte[] data, int index) {
                int idx = (int)(index * 1.5);
                int b1 = data[idx] & 0xFF;
                int b2 = data[idx + 1] & 0xFF;
                int v = (b2 << 8) | b1;

                if ((index % 2) == 0) {
                    return v & 0xFFF;
                }
                else {
                    return v >> 4;
                }
            }

            internal override void WriteEntry(byte[] data, int index, long entry) {
                int idx = (int)(index * 1.5);

                if ((index % 2) == 0) {
                    data[idx] = (byte)(entry & 0xFF);
                    data[idx + 1] = (byte)((entry >> 8) & 0x0F);
                }
                else {
                    data[idx] |= (byte)((entry & 0x0F) << 4);
                    data[idx + 1] = (byte)((entry >> 4) & 0xFF);
                }
            }
        }

        /// <summary>
        /// Represents a 16-bit file allocation table.
        /// </summary>
        internal sealed class FAT16 : FatType {
            internal FAT16() : base((1 << 16) - 16, 0xFFFFL, 2.0f, "FAT16   ") { }

            internal override long ReadEntry(byte[] data, int index) {
                int idx = index << 1;
                int b1 = data[idx] & 0xFF;
                int b2 = data[idx + 1] & 0xFF;
                return (b2 << 8) | b1;
            }

            internal override void WriteEntry(byte[] data, int index, long entry) {
                int idx = index << 1;
                data[idx] = (byte)(entry & 0xFF);
                data[idx + 1] = (byte)((entry >> 8) & 0xFF);
            }
        }

        /// <summary>
        /// Represents a 32-bit file allocation table.
        /// </summary>
        internal sealed class FAT32 : FatType {
            internal FAT32() : base((1 << 28) - 16, 0xFFFFFFFFL, 4.0f, "FAT32   ") { }

            internal override long ReadEntry(byte[] data, int index) {
                int idx = index * 4;
                long l1 = data[idx] & 0xFF;
                long l2 = data[idx + 1] & 0xFF;
                long l3 = data[idx + 2] & 0xFF;
                long l4 = data[idx + 3] & 0xFF;
                return (l4 << 24) | (l3 << 16) | (l2 << 8) | l1;
            }

            internal override void WriteEntry(byte[] data, int index, long entry) {
                int idx = index << 2;
                data[idx] = (byte)(entry & 0xFF);
                data[idx + 1] = (byte)((entry >> 8) & 0xFF);
                data[idx + 2] = (byte)((entry >> 16) & 0xFF);
                data[idx + 3] = (byte)((entry >> 24) & 0xFF);
            }
        }
    }
}
