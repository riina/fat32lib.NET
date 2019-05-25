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

namespace FAT32Lib {

    /// <summary>
    /// The interface common to all file system implementations.
    /// </summary>
    public interface IFileSystem {
        /// <summary>
        /// Gets the root entry of this filesystem. This is usually a directory, but
        /// this is not required.
        /// </summary>
        /// <returns>the file system's root entry</returns>
        /// <exception cref="System.IO.IOException">IOException on read error</exception>
        IFsDirectory GetRoot();

        /// <summary>
        /// Returns if this <see cref="IFileSystem"/> is in read-only mode.
        /// </summary>
        /// <returns>if this <see cref="IFileSystem"/> is read-only</returns>
        bool IsReadOnly();

        /// <summary>
        /// Close this file system. After a close, all invocations of methods of
        /// this file system or objects created by this file system will throw an
        /// <see cref="System.InvalidOperationException"/>.
        /// </summary>
        /// <exception cref="System.IO.IOException">IOException on error closing the file system</exception>
        void Close();

        /// <summary>
        /// Returns true if this file system is closed. If the file system
        /// is closed, no more operations may be performed on it.
        /// </summary>
        /// <returns>if this file system is closed</returns>
        bool IsClosed();

        /// <summary>
        /// The total size of this file system.
        /// </summary>
        /// <returns>if -1 this feature is unsupported</returns>
        /// <exception cref="System.IO.IOException">IOException if an I/O error occurs</exception>
        long GetTotalSpace();

        /// <summary>
        /// The free space of this file system.
        /// </summary>
        /// <returns>if -1 this feature is unsupported</returns>
        /// <exception cref="System.IO.IOException">IOException if an I/O error occurs</exception>
        long GetFreeSpace();

        /// <summary>
        /// The usable space of this file system.
        /// </summary>
        /// <returns>if -1 this feature is unsupported</returns>
        /// <exception cref="System.IO.IOException">IOException if an I/O error occurs</exception>
        long GetUsableSpace();

        /// <summary>
        /// Flushes any modified file system structures to the underlying storage.
        /// </summary>
        /// <exception cref="System.IO.IOException">IOException</exception>
        void Flush();
    }

}