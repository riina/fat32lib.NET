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

using System.IO;

namespace FAT32Lib.Fat {

    /// <summary>
    /// Gets thrown when either
    /// a <see cref="Fat16RootDirectory"/> becomes full or
    /// a <see cref="ClusterChainDirectory"/> grows beyond it's
    /// <see cref="ClusterChainDirectory.MAX_SIZE"/>
    /// </summary>
    public sealed class DirectoryFullException : IOException {
        private readonly int currentCapacity;
        private readonly int requestedCapacity;

        internal DirectoryFullException(int currentCapacity, int requestedCapacity)
            : this("directory is full", currentCapacity, requestedCapacity) {
        }

        internal DirectoryFullException(string message,
                int currentCapacity, int requestedCapacity) : base(message) {

            this.currentCapacity = currentCapacity;
            this.requestedCapacity = requestedCapacity;
        }

        /// <summary>
        /// Returns the current capacity of the directory.
        /// </summary>
        /// <returns>the current capacity</returns>
        public int getCurrentCapacity() {
            return currentCapacity;
        }

        /// <summary>
        /// Returns the capacity the directory tried to grow, which did not succeed.
        /// </summary>
        /// <returns>the requested capacity</returns>
        public int getRequestedCapacity() {
            return requestedCapacity;
        }

    }

}