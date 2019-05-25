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

using System.Collections.Generic;

namespace FAT32Lib {

    /// <summary>
    /// Compares directory entries alphabetically, with all directories coming
    /// before all files.
    /// </summary>
    public class DirectoryEntryComparator : IComparer<IFsDirectoryEntry> {

        public static readonly DirectoryEntryComparator DIRECTORY_ENTRY_COMPARATOR =
            new DirectoryEntryComparator();

        public int Compare(IFsDirectoryEntry e1, IFsDirectoryEntry e2) {
            if (e2.IsDirectory() == e1.IsDirectory()) {
                /* compare names */
                return e1.GetName().CompareTo(e2.GetName());
            }
            else {
                if (e2.IsDirectory()) return 1;
                else return -1;
            }
        }
    }
}
