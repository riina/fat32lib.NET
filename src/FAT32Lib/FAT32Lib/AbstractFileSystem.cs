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

using System;

namespace FAT32Lib {

    /// <summary>
    /// Abstract class with common things in different FileSystem implementations.
    /// </summary>
    public abstract class AbstractFileSystem : IFileSystem {
        private readonly bool readOnly;
        private bool closed;

        /// <summary>
        /// Creates a new <see cref="AbstractFileSystem"/>.
        /// </summary>
        /// <param name="readOnly">if the file system should be read-only</param>
        public AbstractFileSystem(bool readOnly) {
            closed = false;
            this.readOnly = readOnly;
        }

        public void Close() {
            if (!IsClosed()) {
                if (!IsReadOnly()) {
                    Flush();
                }

                closed = true;
            }
        }

        public bool IsClosed() {
            return closed;
        }

        public bool IsReadOnly() {
            return readOnly;
        }

        /// <summary>
        /// Checks if this <see cref="IFileSystem"/> was already closed, and throws an
        /// exception if it was.
        /// </summary>
        /// <exception cref="InvalidOperationException">InvalidOperationException if this <see cref="IFileSystem"/> was
        /// already closed</exception>
        /// <seealso cref="IsClosed"/>
        /// <seealso cref="Close"/>
        protected void CheckClosed() {
            if (IsClosed()) {
                throw new InvalidOperationException("file system was already closed");
            }
        }

        /// <summary>
        /// Checks if this <see cref="IFileSystem"/> is read-only, and throws an
        /// exception if it is.
        /// </summary>
        /// <exception cref="ReadOnlyException"> ReadOnlyException if this <see cref="IFileSystem"/>is read-only</exception>
        /// <seealso cref="IsReadOnly"/>
        protected void CheckReadOnly() {
            if (IsReadOnly()) {
                throw new ReadOnlyException();
            }
        }

        public abstract void Flush();

        public abstract IFsDirectory GetRoot();

        public abstract long GetTotalSpace();

        public abstract long GetFreeSpace();

        public abstract long GetUsableSpace();
    }
}