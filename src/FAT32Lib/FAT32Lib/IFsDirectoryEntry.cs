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
using System.Collections.Generic;

namespace FAT32Lib {

    /// <summary>
    /// Represents one entry in a <see cref="IFsDirectory"/>.
    /// </summary>
    public interface IFsDirectoryEntry : IFsObject {

        /// <summary>
        /// Gets the name of this entry.
        /// </summary>
        /// <returns>this entry's name</returns>
        string GetName();

        /// <summary>
        /// Gets the last modification time of this entry.
        /// </summary>
        /// <returns>the last modification time of the entry as milliseconds
        /// since 1970, or {@code 0} if this filesystem does not support
        /// getting the last modification time</returns>
        /// <exception cref="System.IO.IOException">IOException if an error occurs retrieving the time stamp</exception>
        long GetLastModified();

        /// <summary>
        /// Returns the time when this entry was created as ms since 1970.
        /// </summary>
        /// <returns>the creation time, or 0 if this feature is not supported</returns>
        /// <exception cref="System.IO.IOException">IOException if an error occurs retrieving the time stamp</exception>
        long GetCreated();

        /// <summary>
        /// Returns the time when this entry was last accessed as ms since 1970.
        /// </summary>
        /// <returns>the last access time, or 0 if this feature is not supported</returns>
        /// <exception cref="System.IO.IOException">IOException on error retrieving the last access time</exception>
        long GetLastAccessed();

        /// <summary>
        ///  Is this entry refering to a file?
        /// </summary>
        /// <returns>if this entry refers to a file</returns>
        bool IsFile();

        /// <summary>
        /// Is this entry refering to a (sub-)directory?
        /// </summary>
        /// <returns>if this entry refers to a directory</returns>
        bool IsDirectory();

        /// <summary>
        /// Sets the name of this entry.
        /// </summary>
        /// <param name="newName">the new name of this entry</param>
        /// <exception cref="System.IO.IOException">IOException on error setting the new name</exception>
        void SetName(string newName);

        /// <summary>
        /// Sets the last modification time of this entry.
        /// </summary>
        /// <param name="lastModified">the new last modification time of this entry</param>
        /// <exception cref="System.IO.IOException">IOException on write error</exception>
        void SetLastModified(long lastModified);

        /// <summary>
        /// Gets the file this entry refers to. This method can only be called if
        /// <see cref="IsFile"/> returns true.
        /// </summary>
        /// <returns>the file described by this entry</returns>
        /// <exception cref="System.IO.IOException">IOException on error accessing the file</exception>
        /// <exception cref="System.NotSupportedException">NotSupportedException if this entry is a directory</exception>
        IFsFile GetFile();

        /// <summary>
        /// Gets the directory this entry refers to. This method can only be called
        /// if <see cref="IsDirectory"/> returns true.
        /// </summary>
        /// <returns>The directory described by this entry</returns>
        /// <exception cref="System.IO.IOException">IOException on read error</exception>
        /// <exception cref="System.NotSupportedException">NotSupportedException if this entry is a file</exception>
        IFsDirectory GetDirectory();

        /// <summary>
        /// Indicate if the entry has been modified in memory (ie need to be saved)
        /// </summary>
        /// <returns>true if the entry needs to be saved</returns>
        bool IsDirty();
    }

}