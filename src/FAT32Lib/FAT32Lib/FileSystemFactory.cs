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

using FAT32Lib.Fat;

namespace FAT32Lib {

    /// <summary>
    /// Factory for <see cref="IFileSystem"/> instances.
    /// </summary>
    public class FileSystemFactory {

        private FileSystemFactory() { }

        /// <summary>
        /// Creates a new <see cref="IFileSystem"/> for the specified device. When
        /// using this method, care must be taken that there is only one
        /// <see cref="IFileSystem"/> accessing the specified <see cref="IBlockDevice"/>.
        /// Otherwise severe file system corruption may occur.
        /// </summary>
        /// <param name="device">the device to create the file system for</param>
        /// <param name="readOnly">if the file system should be openend read-only</param>
        /// <returns>a new <see cref="IFileSystem"/> instance for the specified device</returns>
        /// <exception cref="UnknownFileSystemException">if the file system type could
        ///     not be determined</exception>
        /// <exception cref="System.IO.IOException">IOException on read error</exception>
        public static IFileSystem Create(IBlockDevice device, bool readOnly) {

            return FatFileSystem.Read(device, readOnly);
        }
    }
}