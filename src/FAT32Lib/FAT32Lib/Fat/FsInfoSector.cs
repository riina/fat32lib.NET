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

using System.IO;

namespace FAT32Lib.Fat {

    /// <summary>
    /// The FAT32 File System Information Sector.
    /// </summary>
    /// <remarks>
    /// See http://en.wikipedia.org/wiki/File_Allocation_Table#FS_Information_Sector
    /// </remarks>
    public sealed class FsInfoSector : Sector {

        /// <summary>
        /// The offset to the free cluster count value in the FS info sector.
        /// </summary>
        public const int FREE_CLUSTERS_OFFSET = 0x1e8;

        /// <summary>
        /// The offset to the "last allocated cluster" value in this sector.
        /// </summary>
        public const int LAST_ALLOCATED_OFFSET = 0x1ec;

        /// <summary>
        /// The offset to the signature of this sector.
        /// </summary>
        public const int SIGNATURE_OFFSET = 0x1fe;

        private FsInfoSector(IBlockDevice device, long offset) : base(device, offset, BootSector.SIZE) { }

        /// <summary>
        /// Reads a <see cref="FsInfoSector"/> as specified by the given
        /// <see cref="Fat32BootSector"/>.
        /// </summary>
        /// <param name="bs">the boot sector that specifies where the FS info sector is
        ///     stored</param>
        /// <returns>the FS info sector that was read</returns>
        /// <exception cref="IOException">IOException on read error</exception>
        /// <seealso cref="Fat32BootSector.GetFsInfoSectorNr"/>
        public static FsInfoSector Read(Fat32BootSector bs) {
            var result =
                    new FsInfoSector(bs.GetDevice(), Offset(bs));

            result.Read();
            result.Verify();
            return result;
        }

        /// <summary>
        /// Creates an new <see cref="FsInfoSector"/> where the specified
        /// <see cref="Fat32BootSector"/> indicates it should be.
        /// </summary>
        /// <param name="bs">the boot sector specifying the FS info sector storage</param>
        /// <returns>the FS info sector instance that was created</returns>
        /// <exception cref="IOException">IOException on write error</exception>
        /// <seealso cref="Fat32BootSector.GetFsInfoSectorNr"/>
        public static FsInfoSector Create(Fat32BootSector bs) {
            var offset = Offset(bs);

            if (offset == 0) throw new IOException(
                    "creating a FS info sector at offset 0 is strange");

            var result =
                   new FsInfoSector(bs.GetDevice(), Offset(bs));

            result.Init();
            result.Write();
            return result;
        }

        private static int Offset(Fat32BootSector bs) {
            return bs.GetFsInfoSectorNr() * bs.GetBytesPerSector();
        }

        /// <summary>
        /// Sets the number of free clusters on the file system stored at
        /// <see cref="FREE_CLUSTERS_OFFSET"/>.
        /// </summary>
        /// <param name="value">the new free cluster count</param>
        /// <seealso cref="Fat.GetFreeClusterCount"/>
        public void SetFreeClusterCount(long value) {
            if (GetFreeClusterCount() == value) return;

            Set32(FREE_CLUSTERS_OFFSET, value);
        }

        /// <summary>
        /// Returns the number of free clusters on the file system as sepcified by
        /// the 32-bit value at <see cref="FREE_CLUSTERS_OFFSET"/>.
        /// </summary>
        /// <returns>the number of free clusters</returns>
        /// <seealso cref="Fat.GetFreeClusterCount"/>
        public long GetFreeClusterCount() {
            return Get32(FREE_CLUSTERS_OFFSET);
        }

        /// <summary>
        /// Sets the last allocated cluster that was used in the <see cref="Fat"/>.
        /// </summary>
        /// <param name="value">the FAT's last allocated cluster number</param>
        /// <seealso cref="Fat.GetLastAllocatedCluster"/>
        public void SetLastAllocatedCluster(long value) {
            if (GetLastAllocatedCluster() == value) return;

            Set32(LAST_ALLOCATED_OFFSET, value);
        }

        /// <summary>
        /// Returns the last allocated cluster number of the <see cref="Fat"/> of the
        /// file system this FS info sector is part of.
        /// </summary>
        /// <returns>the last allocated cluster number</returns>
        /// <seealso cref="Fat.GetLastAllocatedCluster"/>
        public long GetLastAllocatedCluster() {
            return Get32(LAST_ALLOCATED_OFFSET);
        }

        private void Init() {
            Buffer.Position = 0;
            Buffer.WriteByte(0x52);
            Buffer.WriteByte(0x52);
            Buffer.WriteByte(0x61);
            Buffer.WriteByte(0x41);

            /* 480 reserved bytes */

            Buffer.Position = 0x1e4;
            Buffer.WriteByte(0x72);
            Buffer.WriteByte(0x72);
            Buffer.WriteByte(0x41);
            Buffer.WriteByte(0x61);

            SetFreeClusterCount(-1);
            SetLastAllocatedCluster(Fat.FIRST_CLUSTER);

            Buffer.Position = SIGNATURE_OFFSET;
            Buffer.WriteByte(0x55);
            Buffer.WriteByte(0xaa);

            MarkDirty();
        }

        private void Verify() {
            if (!(Get8(SIGNATURE_OFFSET) == 0x55) ||
                    !(Get8(SIGNATURE_OFFSET + 1) == 0xaa)) {

                throw new IOException("invalid FS info sector signature");
            }
        }

    }

}