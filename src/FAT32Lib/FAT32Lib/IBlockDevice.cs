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

using System.IO;

namespace FAT32Lib {

    /// <summary>
    /// This is the abstraction used for a device that can hold a {@link FileSystem}.
    /// </summary>
    public interface IBlockDevice {

        /// <summary>
        /// Gets the total length of this device in bytes.
        /// </summary>
        /// <returns>the total number of bytes on this device</returns>
        /// <exception cref="IOException">IOException on error getting the size of this device</exception>
        long GetSize();

        /// <summary>
        /// Read a block of data from this device.
        /// </summary>
        /// <param name="devOffset">the byte offset where to read the data from</param>
        /// <param name="dest">the destination buffer where to store the data read</param>
        /// <exception cref="IOException">IOException on read error</exception>
        void Read(long devOffset, MemoryStream dest);

        /// <summary>
        /// Writes a block of data to this device.
        /// </summary>
        /// <param name="devOffset">the byte offset where to store the data</param>
        /// <param name="src">the source <see cref="MemoryStream"/> to write to the device</param>
        /// <exception cref="IOException">IOException on write error</exception>
        /// <exception cref="System.ArgumentException">ArgumentException if the devOffset is negative
        ///     or the write would go beyond the end of the device</exception>
        /// <seealso cref="IsReadOnly"/>
        void Write(long devOffset, MemoryStream src);

        /// <summary>
        /// Flushes data in caches to the actual storage.
        /// </summary>
        /// <exception cref="IOException">IOException on write error</exception>
        void Flush();

        /// <summary>
        /// Returns the size of a sector on this device.
        /// </summary>
        /// <returns>the sector size in bytes</returns>
        /// <exception cref="IOException">IOException on error determining the sector size</exception>
        int GetSectorSize();

        /// <summary>
        /// Closes this <see cref="IBlockDevice"/>. No methods of this device may be
        /// accesses after this method was called.
        /// </summary>
        /// <exception cref="IOException">IOException on error closing this device</exception>
        /// <seealso cref="IsClosed"/>
        void Close();

        /// <summary>
        /// Checks if this device was already closed. No methods may be called
        /// on a closed device (except this method).
        /// </summary>
        /// <returns>if this device is closed</returns>
        bool IsClosed();

        /// <summary>
        /// Checks if this <see cref="IBlockDevice"/> is read-only.
        /// </summary>
        /// <returns>if this <see cref="IBlockDevice"/> is read-only</returns>
        bool IsReadOnly();

    }

}