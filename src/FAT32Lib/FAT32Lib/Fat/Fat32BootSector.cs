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

namespace FAT32Lib.Fat {

    /// <summary>
    /// Contains the FAT32 specific parts of the boot sector.
    /// </summary>
    public sealed class Fat32BootSector : BootSector {

        /// <summary>
        /// The offset to the entry specifying the first cluster of the FAT32
        /// root directory.
        /// </summary>
        public const int ROOT_DIR_FIRST_CLUSTER_OFFSET = 0x2c;

        /// <summary>
        /// The offset to the 4 bytes specifying the sectors per FAT value.
        /// </summary>
        public const int SECTORS_PER_FAT_OFFSET = 0x24;

        /// <summary>
        /// Offset to the file system type label.
        /// </summary>
        public const int FILE_SYSTEM_TYPE_OFFSET = 0x52;

        public const int VERSION_OFFSET = 0x2a;
        public const int VERSION = 0;

        public const int FS_INFO_SECTOR_OFFSET = 0x30;
        public const int BOOT_SECTOR_COPY_OFFSET = 0x32;
        public const int EXTENDED_BOOT_SIGNATURE_OFFSET = 0x42;

        /// <summary>
        /// TODO: make this constructor private
        /// </summary>
        /// <param name="device"></param>
        public Fat32BootSector(IBlockDevice device) : base(device) { }

        public override void Init() {
            base.Init();

            Set16(VERSION_OFFSET, VERSION);

            SetBootSectorCopySector(6); /* as suggested by M$ */
        }

        /// <summary>
        /// Returns the first cluster in the FAT that contains the root directory.
        /// </summary>
        /// <returns>the root directory's first cluster</returns>
        public long GetRootDirFirstCluster() {
            return Get32(ROOT_DIR_FIRST_CLUSTER_OFFSET);
        }

        /// <summary>
        /// Sets the first cluster of the root directory.
        /// </summary>
        /// <param name="value">the root directory's first cluster</param>
        public void SetRootDirFirstCluster(long value) {
            if (GetRootDirFirstCluster() == value) return;

            Set32(ROOT_DIR_FIRST_CLUSTER_OFFSET, value);
        }

        /// <summary>
        /// Sets the sectur number that contains a copy of the boot sector.
        /// </summary>
        /// <param name="sectNr">the sector that contains a boot sector copy</param>
        public void SetBootSectorCopySector(int sectNr) {
            if (GetBootSectorCopySector() == sectNr) return;
            if (sectNr < 0) throw new ArgumentException(
                    "boot sector copy sector must be >= 0");

            Set16(BOOT_SECTOR_COPY_OFFSET, sectNr);
        }

        /// <summary>
        /// Returns the sector that contains a copy of the boot sector, or 0 if
        /// there is no copy.
        /// </summary>
        /// <returns>the sector number of the boot sector copy</returns>
        public int GetBootSectorCopySector() {
            return Get16(BOOT_SECTOR_COPY_OFFSET);
        }

        /// <summary>
        /// Sets the 11-byte volume label stored at offset 0x47.
        /// </summary>
        /// <param name="label">the new volume label, may be null</param>
        public void SetVolumeLabel(string label) {
            for (int i = 0; i < 11; i++) {
                byte c =
                        (byte)((label == null) ? 0 :
                        (label.Length > i) ? (byte)label[i] : 0x20);

                Set8(0x47 + i, c);
            }
        }

        public int GetFsInfoSectorNr() {
            return Get16(FS_INFO_SECTOR_OFFSET);
        }

        public void SetFsInfoSectorNr(int offset) {
            if (GetFsInfoSectorNr() == offset) return;

            Set16(FS_INFO_SECTOR_OFFSET, offset);
        }

        public override void SetSectorsPerFat(long v) {
            if (GetSectorsPerFat() == v) return;

            Set32(SECTORS_PER_FAT_OFFSET, v);
        }

        public override long GetSectorsPerFat() {
            return Get32(SECTORS_PER_FAT_OFFSET);
        }

        public override FatType GetFatType() {
            return FatType.BASE_FAT32;
        }

        public override void SetSectorCount(long count) {
            SetNrTotalSectors(count);
        }

        public override long GetSectorCount() {
            return GetNrTotalSectors();
        }

        /// <summary>
        /// This is always 0 for FAT32.
        /// </summary>
        /// <returns>always 0</returns>
        public override int GetRootDirEntryCount() {
            return 0;
        }

        public void SetFileSystemId(int id) {
            Set32(0x43, id);
        }

        public int GetFileSystemId() {
            return (int)Get32(0x43);
        }

        /// <summary>
        /// Writes a copy of this boot sector to the specified device, if a copy
        /// is requested.
        /// </summary>
        /// <param name="device">the device to write the boot sector copy to</param>
        /// <exception cref="System.IO.IOException">IOException on write error</exception>
        /// <seealso cref="GetBootSectorCopySector"/>
        public void WriteCopy(IBlockDevice device) {
            if (GetBootSectorCopySector() > 0) {
                long offset = GetBootSectorCopySector() * SIZE;
                buffer.Position = 0;
                device.Write(offset, buffer);
            }
        }

        public override int GetFileSystemTypeLabelOffset() {
            return FILE_SYSTEM_TYPE_OFFSET;
        }

        public override int GetExtendedBootSignatureOffset() {
            return EXTENDED_BOOT_SIGNATURE_OFFSET;
        }
    }

}