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
    /// An IFsFile is a representation of a single block of bytes on a filesystem. It
    /// is comparable to an inode in Unix.
    /// 
    /// An IFsFile does not have any knowledge of who is using this file. It is also
    /// possible that the system uses a single IFsFile instance to create two
    /// inputstream's for two different principals.
    /// </summary>
    public interface IFsFile : IFsObject {

        /// <summary>
        /// Gets the length (in bytes) of this file.
        /// </summary>
        /// <returns>the file size</returns>
        long GetLength();

        /// <summary>
        /// Sets the length of this file.
        /// </summary>
        /// <param name="length">the new length of this file</param>
        /// <exception cref="IOException">IOException on error updating the file size</exception>
        void SetLength(long length);

        /// <summary>
        /// Reads from this file into the specified <see cref="MemoryStream"/>. The
        /// first byte read will be put into the buffer at it's
        /// <see cref="MemoryStream.Position"/>, and the number of bytes read
        /// will equal <see cref="MemoryStream.Length"/> - <see cref="MemoryStream.Position"/> bytes.
        /// </summary>
        /// <param name="offset">the offset into the file where to start reading</param>
        /// <param name="dest">the destination buffer where to put the bytes that were read</param>
        /// <exception cref="IOException">IOException on read error</exception>
        void Read(long offset, MemoryStream dest);

        /**
         * Writes to this file taking the data to write from the specified
         * {@code ByteBuffer}. This method will read the buffer's
         * {@link ByteBuffer#remaining() remaining} bytes starting at it's
         * {@link ByteBuffer#position() position}.
         * 
         * @param offset the offset into the file where the first byte will be
         *      written
         * @param src the source buffer to read the data from
         * @throws ReadOnlyException if the file is read-only
         * @throws IOException on write error
         */

        /// <summary>
        /// Writes to this file taking the data to write from the specified
        /// <see cref="MemoryStream"/>. This method will read the buffer's
        /// <see cref="MemoryStream.Length"/> - <see cref="MemoryStream.Position"/> bytes starting at it's
        /// <see cref="MemoryStream.Position"/>.
        /// </summary>
        /// <param name="offset">the offset into the file where the first byte will be
        ///     written</param>
        /// <param name="src">the source buffer to read the data from</param>
        /// <exception cref="ReadOnlyException">if the file is read-only</exception>
        /// <exception cref="IOException">IOException on write error</exception>
        void Write(long offset, MemoryStream src);

        /// <summary>
        /// Flush any possibly cached data to the disk.
        /// </summary>
        /// <exception cref="IOException">IOException on error flushing</exception>
        void Flush();
    }

}