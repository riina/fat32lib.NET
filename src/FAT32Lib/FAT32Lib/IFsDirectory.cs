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

using System.Collections;

namespace FAT32Lib {

    /// <summary>
    /// Base class for all <see cref="IFileSystem"/> directories.
    /// </summary>
    public interface IFsDirectory : IEnumerable, IFsObject {

        /// <summary>
        /// Gets the entry with the given name.
        /// </summary>
        /// <param name="name">the name of the entry to get</param>
        /// <returns>the entry, if it existed</returns>
        /// <exception cref="System.IO.IOException">IOException on error retrieving the entry</exception>
        IFsDirectoryEntry GetEntry(string name);

        /// <summary>
        /// Add a new file with a given name to this directory.
        /// </summary>
        /// <param name="name">the name of the file to add</param>
        /// <returns>the entry pointing to the new file</returns>
        /// <exception cref="System.IO.IOException">IOException on error creating the file</exception>
        IFsDirectoryEntry AddFile(string name);

        /// <summary>
        /// Add a new (sub-)directory with a given name to this directory.
        /// </summary>
        /// <param name="name">the name of the sub-directory to add</param>
        /// <returns>the entry pointing to the new directory</returns>
        /// <exception cref="System.IO.IOException">IOException on error creating the directory</exception>
        IFsDirectoryEntry AddDirectory(string name);

        /// <summary>
        /// Remove the entry with the given name from this directory.
        /// </summary>
        /// <param name="name">name of the entry to remove</param>
        /// <exception cref="System.IO.IOException">IOException on error deleting the entry</exception>
        void Remove(string name);

        /// <summary>
        /// Save all dirty (unsaved) data to the device.
        /// </summary>
        /// <exception cref="System.IO.IOException">IOException on write error</exception>
        void Flush();

    }

}