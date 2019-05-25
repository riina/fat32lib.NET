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

using System;
using System.Text;

namespace FAT32Lib.Fat {

    /// <summary>
    /// The boot sector layout as used by the FAT12 / FAT16 variants.
    /// </summary>
    public sealed class Fat16BootSector : BootSector {

        /// <summary>
        /// The default number of entries for the root directory.
        /// </summary>
        /// <seealso cref="BootSector.GetRootDirEntryCount"/>
        /// <seealso cref="BootSector.SetRootDirEntryCount(int)"/>
        public const int DEFAULT_ROOT_DIR_ENTRY_COUNT = 512;

        /// <summary>
        /// The default volume label.
        /// </summary>
        public const string DEFAULT_VOLUME_LABEL = "NO NAME";

        /// <summary>
        /// The maximum number of clusters for a FAT12 file system. This is 
        /// the number of clusters where mkdosfs stop complaining about a 
        /// partition having not enough sectors, so it would be misinterpreted
        /// as FAT12 without special handling.
        /// </summary>
        /// <seealso cref="BootSector.GetNrLogicalSectors"/>
        public const int MAX_FAT12_CLUSTERS = 4084;

        public const int MAX_FAT16_CLUSTERS = 65524;

        /// <summary>
        /// The offset to the sectors per FAT value.
        /// </summary>
        public const int SECTORS_PER_FAT_OFFSET = 0x16;

        /// <summary>
        /// The offset to the root directory entry count value.
        /// </summary>
        /// <seealso cref="BootSector.GetRootDirEntryCount"/>
        /// <seealso cref="SetRootDirEntryCount(int)"/>
        public const int ROOT_DIR_ENTRIES_OFFSET = 0x11;

        /// <summary>
        /// The offset to the first byte of the volume label.
        /// </summary>
        public const int VOLUME_LABEL_OFFSET = 0x2b;

        /// <summary>
        /// Offset to the FAT file system type string.
        /// </summary>
        /// <seealso cref="BootSector.GetFileSystemTypeLabel"
        public const int FILE_SYSTEM_TYPE_OFFSET = 0x36;

        /// <summary>
        /// The maximum length of the volume label.
        /// </summary>
        public const int MAX_VOLUME_LABEL_LENGTH = 11;

        public const int EXTENDED_BOOT_SIGNATURE_OFFSET = 0x26;

        /// <summary>
        /// Creates a new <see cref="Fat16BootSector"/> for the specified device.
        /// </summary>
        /// <param name="device">the <see cref="IBlockDevice"/> holding the boot sector</param>
        public Fat16BootSector(IBlockDevice device) : base(device) { }

        /// <summary>
        /// Returns the volume label that is stored in this boot sector.
        /// </summary>
        /// <returns>the volume label</returns>
        public string GetVolumeLabel() {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < MAX_VOLUME_LABEL_LENGTH; i++) {
                char c = (char)Get8(VOLUME_LABEL_OFFSET + i);

                if (c != 0) {
                    sb.Append(c);
                }
                else {
                    break;
                }
            }

            return sb.ToString();
        }

        /**
         * Sets the volume label that is stored in this boot sector.
         *
         * @param label the new volume label
         * @throws IllegalArgumentException if the specified label is longer
         *      than {@link #MAX_VOLUME_LABEL_LENGTH}
         */
        /// <summary>
        /// Sets the volume label that is stored in this boot sector.
        /// </summary>
        /// <param name="label">the new volume label</param>
        /// <exception cref="ArgumentException">ArgumentException if the specified label is longer
        ///     than <see cref="MAX_VOLUME_LABEL_LENGTH"/></exception>
        public void SetVolumeLabel(string label) {
            if (label.Length > MAX_VOLUME_LABEL_LENGTH)
                throw new ArgumentException("volume label too long");

            for (int i = 0; i < MAX_VOLUME_LABEL_LENGTH; i++) {
                Set8(VOLUME_LABEL_OFFSET + i,
                        i < label.Length ? label[i] : 0);
            }
        }

        /// <summary>
        /// Gets the number of sectors/fat for FAT 12/16.
        /// </summary>
        /// <returns>int</returns>
        public override long GetSectorsPerFat() {
            return Get16(SECTORS_PER_FAT_OFFSET);
        }

        /// <summary>
        /// Sets the number of sectors/fat
        /// </summary>
        /// <param name="v">the new number of sectors per fat</param>
        public override void SetSectorsPerFat(long v) {
            if (v == GetSectorsPerFat()) return;
            if (v > 0x7FFF) throw new ArgumentException(
                    "too many sectors for a FAT12/16");

            Set16(SECTORS_PER_FAT_OFFSET, (int)v);
        }

        public override FatType GetFatType() {
            long rootDirSectors = ((GetRootDirEntryCount() * 32) +
                    (GetBytesPerSector() - 1)) / GetBytesPerSector();
            long dataSectors = GetSectorCount() -
                    (GetNrReservedSectors() + (GetNrFats() * GetSectorsPerFat()) +
                    rootDirSectors);
            long clusterCount = dataSectors / GetSectorsPerCluster();

            if (clusterCount > MAX_FAT16_CLUSTERS) throw new InvalidOperationException(
                    "too many clusters for FAT12/16: " + clusterCount);

            return clusterCount > MAX_FAT12_CLUSTERS ?
                FatType.BASE_FAT16 : FatType.BASE_FAT12;
        }

        public override void SetSectorCount(long count) {
            if (count > 65535) {
                SetNrLogicalSectors(0);
                SetNrTotalSectors(count);
            }
            else {
                SetNrLogicalSectors((int)count);
                SetNrTotalSectors(count);
            }
        }

        public override long GetSectorCount() {
            if (GetNrLogicalSectors() == 0) return GetNrTotalSectors();
            else return GetNrLogicalSectors();
        }

        /// <summary>
        /// Gets the number of entries in the root directory.
        /// </summary>
        /// <returns>int the root directory entry count</returns>
        public override int GetRootDirEntryCount() {
            return Get16(ROOT_DIR_ENTRIES_OFFSET);
        }

        /// <summary>
        /// Sets the number of entries in the root directory
        /// </summary>
        /// <param name="v">the new number of entries in the root directory</param>
        /// <exception cref="ArgumentException">ArgumentException for negative values</exception>
        public void SetRootDirEntryCount(int v) {
            if (v < 0) throw new ArgumentException();
            if (v == GetRootDirEntryCount()) return;

            Set16(ROOT_DIR_ENTRIES_OFFSET, v);
        }

        public override void Init() {
            base.Init();
            SetRootDirEntryCount(DEFAULT_ROOT_DIR_ENTRY_COUNT);
            SetVolumeLabel(DEFAULT_VOLUME_LABEL);
        }

        public override int GetFileSystemTypeLabelOffset() {
            return FILE_SYSTEM_TYPE_OFFSET;
        }

        public override int GetExtendedBootSignatureOffset() {
            return EXTENDED_BOOT_SIGNATURE_OFFSET;
        }

    }

}