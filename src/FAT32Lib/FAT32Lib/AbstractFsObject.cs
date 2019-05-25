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
    /// A base class that helps to implement the <see cref="IFsObject"/> interface.
    /// </summary>
    public class AbstractFsObject : IFsObject {


        /// <summary>
        /// Holds the read-only state of this object.
        /// </summary>
        private readonly bool readOnly;

        /// <summary>
        /// Remembers if this object still valid.
        /// </summary>
        private bool valid;

        /// <summary>
        /// Creates a new instance of <see cref="AbstractFsObject"/> which will be valid
        /// and have the specified read-only state.
        /// </summary>
        /// <param name="readOnly">if the new object will be read-only</param>
        protected AbstractFsObject(bool readOnly) {
            valid = true;
            this.readOnly = readOnly;
        }

        public bool IsValid() {
            return this.valid;
        }

        /// <summary>
        /// Marks this object as invalid.
        /// </summary>
        /// <seealso cref="IsValid"/>
        /// <seealso cref="CheckValid"/>
        protected void Invalidate() {
            valid = false;
        }

        /// <summary>
        /// Convience method to check if this object is still valid and throw an
        /// <see cref="InvalidOperationException"/> if it is not.
        /// </summary>
        /// <exception cref="InvalidOperationException">InvalidOperationException if this object was invalidated</exception>
        /// <seealso cref="IsValid"/>
        /// <seealso cref="Invalidate"/>
        protected void CheckValid() {
            if (!IsValid()) throw new InvalidOperationException(
                    this + " is not valid");
        }

        /// <summary>
        /// Convience method to check if this object is writable. An object is
        /// writable if it is both, valid and not read-only.
        /// </summary>
        /// <exception cref="InvalidOperationException">InvalidOperationException if this object was invalidated</exception>
        /// <exception cref="ReadOnlyException">ReadOnlyException if this object was created with the read-only
        ///     flag set</exception>
        protected void CheckWritable() {
            CheckValid();
            if (IsReadOnly()) {
                throw new ReadOnlyException();
            }
        }

        public bool IsReadOnly() {
            return readOnly;
        }

    }

}