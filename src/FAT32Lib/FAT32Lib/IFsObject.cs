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
    /// This interface is the base interface for objects that are part of a
    /// <see cref="IFileSystem"/>.
    /// </summary>
    public interface IFsObject {

        /// <summary>
        /// <para>
        /// Checks if this <see cref="IFsObject"/> is still valid.
        /// </para>
        /// <para>
        /// An object is not valid anymore if it has been removed from the
        /// filesystem.All invocations on methods (except this method and the
        /// methods inherited from <see cref="object"/>) of
        /// invalid objects must throw a <see cref="System.InvalidOperationException"/>.
        /// </para>
        /// </summary>
        /// <returns>if this <see cref="IFsObject"/> is still valid</returns>
        bool IsValid();

        /// <summary>
        /// Checks if this <see cref="IFsObject"/> is read-only. Any attempt to modify a
        /// read-only <see cref="IFsObject"/> must result in a <see cref="ReadOnlyException"/>
        /// being thrown, and the modification must not be performed.
        /// </summary>
        /// <returns>if this <see cref="IFsObject"/> is read-only</returns>
        bool IsReadOnly();

    }

}