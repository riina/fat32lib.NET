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

namespace FAT32Lib {

    /// <summary>
    /// Indicates that it was not possible to determine the type of the file
    /// system being used on a block device.
    /// </summary>
    public sealed class UnknownFileSystemException : IOException {

        private readonly IBlockDevice device;

        /// <summary>
        /// Creates a new instance of <see cref="UnknownFileSystemException"/>.
        /// </summary>
        /// <param name="device">the <see cref="IBlockDevice"/> whose file system could not
        ///     be determined</param>
        public UnknownFileSystemException(IBlockDevice device) : base("cannot determine file system type") {
            this.device = device;
        }

        /// <summary>
        /// Returns the <see cref="IBlockDevice"/> whose file system could not be
        /// determined.
        /// </summary>
        /// <returns>the <see cref="IBlockDevice"/> with an unknown file system</returns>
        public IBlockDevice GetDevice() {
            return device;
        }
    }

}