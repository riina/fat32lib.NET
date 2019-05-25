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
using System.IO;

namespace FAT32Lib.Fat {

    /// <summary>
    /// The root directory of a FAT12/16 partition.
    /// </summary>
    internal sealed class Fat16RootDirectory : AbstractDirectory {
        private readonly IBlockDevice device;
        private readonly long deviceOffset;

        private Fat16RootDirectory(Fat16BootSector bs, bool readOnly)
            : base(bs.GetRootDirEntryCount(), readOnly, true) {

            if (bs.GetRootDirEntryCount() <= 0) throw new ArgumentException(
                    "root directory size is " + bs.GetRootDirEntryCount());

            deviceOffset = FatUtils.GetRootDirOffset(bs);
            device = bs.GetDevice();
        }

        /// <summary>
        /// Reads a <see cref="Fat16RootDirectory"/> as indicated by the specified
        /// <see cref="Fat16BootSector"/>.
        /// </summary>
        /// <param name="bs">the boot sector that describes the root directory to read</param>
        /// <param name="readOnly">if the directory shold be created read-only</param>
        /// <returns>the directory that was read</returns>
        /// <exception cref="IOException">IOException on read error</exception>
        public static Fat16RootDirectory Read(Fat16BootSector bs, bool readOnly) {
            Fat16RootDirectory result = new Fat16RootDirectory(bs, readOnly);
            result.Read();
            return result;
        }

        /// <summary>
        /// Creates a new <see cref="Fat16RootDirectory"/> as indicated by the specified
        /// <see cref="Fat16BootSector"/>. The directory will always be created in
        /// read-write mode.
        /// </summary>
        /// <param name="bs">the boot sector that describes the root directory to create</param>
        /// <returns>the directory that was created</returns>
        /// <exception cref="IOException">IOException on write error</exception>
        public static Fat16RootDirectory Create(Fat16BootSector bs) {
            Fat16RootDirectory result = new Fat16RootDirectory(bs, false);
            result.Flush();
            return result;
        }

        protected override void Read(MemoryStream data) {
            device.Read(deviceOffset, data);
        }

        protected override void Write(MemoryStream data) {
            device.Write(deviceOffset, data);
        }

        /// <summary>
        /// By convention always returns 0, as the FAT12/16 root directory is not
        /// stored in a cluster chain.
        /// </summary>
        /// <returns>always 0</returns>
        protected override long GetStorageCluster() {
            return 0;
        }

        /// <summary>
        /// As a FAT12/16 root directory can not change it's size, this method
        /// throws a <see cref="DirectoryFullException"/> if the requested size is
        /// larger than <see cref="AbstractDirectory.GetCapacity"/> and does nothing else.
        /// </summary>
        /// <param name="entryCount"></param>
        internal override void ChangeSize(int entryCount) {
            if (GetCapacity() < entryCount) {
                throw new DirectoryFullException(GetCapacity(), entryCount);
            }
        }

    }

}