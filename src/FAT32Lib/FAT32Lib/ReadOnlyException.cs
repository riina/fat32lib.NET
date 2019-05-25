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

namespace FAT32Lib {

    /// <summary>
    /// This exception is thrown when an attempt is made to write to a read-only
    /// <see cref="IBlockDevice"/>, <see cref="IFileSystem"/> or other file system object. This is
    /// an unchecked exception, as it should always be possible to query the object
    /// about it's read-only state using it's isReadOnly() method.
    /// </summary>
    /// <seealso cref="IFileSystem.IsReadOnly"/>
    /// <seealso cref="IBlockDevice.IsReadOnly"/>
    public sealed class ReadOnlyException : SystemException {

        /// <summary>
        ///  Creates a new instance of <see cref="ReadOnlyException"/>.
        /// </summary>
        public ReadOnlyException() : base("read-only") {
        }
    }

}