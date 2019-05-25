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
using System.IO;
using System.Text;

namespace FAT32Lib.Fat {

    /// <summary>
    /// Represents an entry in a <see cref="FatLfnDirectory"/>. Besides implementing the
    /// <see cref="IFsDirectoryEntry"/> interface for FAT file systems, it allows access
    /// to the <see cref="SetArchiveFlag(bool)"/> archive,
    /// <see cref="SetHiddenFlag(bool)"/> hidden,
    /// <see cref="SetReadOnlyFlag(bool)"/> read-only and
    /// <see cref="SetSystemFlag(bool)"/> system flags specifed for the FAT file
    /// system.
    /// </summary>
    public sealed class FatLfnDirectoryEntry : AbstractFsObject, IFsDirectoryEntry {

        internal readonly FatDirectoryEntry realEntry;

        private FatLfnDirectory parent;
        private string fileName;

        internal FatLfnDirectoryEntry(string name, ShortName sn, FatLfnDirectory parent, bool directory) : base(false) {
            this.parent = parent;
            fileName = name;

            long now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            realEntry = FatDirectoryEntry.Create(directory);
            realEntry.SetShortName(sn);
            realEntry.SetCreated(now);
            realEntry.SetLastAccessed(now);
        }

        internal FatLfnDirectoryEntry(FatLfnDirectory parent,
                FatDirectoryEntry realEntry, string fileName) : base(parent.IsReadOnly()) {
            this.parent = parent;
            this.realEntry = realEntry;
            this.fileName = fileName;
        }

        internal static FatLfnDirectoryEntry Extract(
                FatLfnDirectory dir, int offset, int len) {

            FatDirectoryEntry realEntry = dir.dir.GetEntry(offset + len - 1);
            string fileName;

            if (len == 1) {
                /* this is just an old plain 8.3 entry */
                fileName = realEntry.GetShortName().AsSimpleString();
            }
            else {
                /* stored in reverse order */
                StringBuilder name = new StringBuilder(13 * (len - 1));

                for (int i = len - 2; i >= 0; i--) {
                    FatDirectoryEntry entry = dir.dir.GetEntry(i + offset);
                    name.Append(entry.GetLfnPart());
                }

                fileName = name.ToString().Trim();
            }

            return new FatLfnDirectoryEntry(dir, realEntry, fileName);
        }

        /// <summary>
        /// Returns if this directory entry has the FAT "hidden" flag set.
        /// </summary>
        /// <returns>if this is a hidden directory entry</returns>
        /// <seealso cref="SetHiddenFlag(bool)"/>
        public bool IsHiddenFlag() {
            return realEntry.IsHiddenFlag();
        }

        /// <summary>
        /// Sets the "hidden" flag on this <see cref="FatLfnDirectoryEntry"/> to the
        /// specified value.
        /// </summary>
        /// <param name="hidden">if this entry should have the hidden flag set</param>
        /// <exception cref="ReadOnlyException"></exception>
        /// <seealso cref="IsHiddenFlag"/>
        public void SetHiddenFlag(bool hidden) {
            CheckWritable();

            realEntry.SetHiddenFlag(hidden);
        }

        /// <summary>
        /// Returns if this directory entry has the FAT "system" flag set.
        /// </summary>
        /// <returns>if this is a "system" directory entry</returns>
        /// <seealso cref="SetSystemFlag(bool)"/>
        public bool IsSystemFlag() {
            return realEntry.IsSystemFlag();
        }

        /// <summary>
        /// Sets the "system" flag on this <see cref="FatLfnDirectoryEntry"/> to the
        /// specified value.
        /// </summary>
        /// <param name="systemEntry">if this entry should have the system flag set</param>
        /// <exception cref="ReadOnlyException">ReadOnlyException if this entry is read-only</exception>
        /// <seealso cref="IsSystemFlag"/>
        public void SetSystemFlag(bool systemEntry) {
            CheckWritable();

            realEntry.SetSystemFlag(systemEntry);
        }

        /// <summary>
        /// Returns if this directory entry has the FAT "read-only" flag set. This
        /// entry may still modified if <see cref="IsReadOnlyFlag"/> returns true.
        /// </summary>
        /// <returns>if this entry has the read-only flag set</returns>
        /// <seealso cref="SetReadOnlyFlag(bool)"/>
        public bool IsReadOnlyFlag() {
            return realEntry.IsReadonlyFlag();
        }

        /// <summary>
        ///  Sets the "read only" flag on this <see cref="FatLfnDirectoryEntry"/> to the
        /// specified value. This method only modifies the read-only flag as
        /// specified by the FAT file system, which is essentially ignored by the
        /// fat32-lib. The true indicator if it is possible to alter this
        /// </summary>
        /// <param name="readOnly">if this entry should be flagged as read only</param>
        /// <exception cref="ReadOnlyException">ReadOnlyException if this entry is read-only as given by
        ///     <see cref="IsReadOnlyFlag"/> method</exception>
        /// <seealso cref="IsReadOnlyFlag"/>
        public void SetReadOnlyFlag(bool readOnly) {
            CheckWritable();

            realEntry.SetReadonlyFlag(readOnly);
        }

        /// <summary>
        /// Returns if this directory entry has the FAT "archive" flag set.
        /// </summary>
        /// <returns>if this entry has the archive flag set</returns>
        public bool IsArchiveFlag() {
            return realEntry.IsArchiveFlag();
        }

        /// <summary>
        /// Sets the "archive" flag on this <see cref="FatLfnDirectoryEntry"/> to the
        /// specified value.
        /// </summary>
        /// <param name="archive">if this entry should have the archive flag set</param>
        /// <exception cref="ReadOnlyException">ReadOnlyException if this entry is
        ///     <see cref="IsReadOnlyFlag"/> read-only</exception>
        public void SetArchiveFlag(bool archive) {
            CheckWritable();

            realEntry.SetArchiveFlag(archive);
        }

        private int TotalEntrySize() {
            int result = (fileName.Length / 13) + 1;

            if ((fileName.Length % 13) != 0) {
                result++;
            }

            return result;
        }

        internal FatDirectoryEntry[] CompactForm() {
            if (realEntry.GetShortName().Equals(ShortName.DOT) ||
                    realEntry.GetShortName().Equals(ShortName.DOT_DOT) ||
                    realEntry.HasShortNameOnly) {
                /* the dot entries must not have a LFN */
                return new FatDirectoryEntry[] { realEntry };
            }

            int totalEntrySize = TotalEntrySize();

            FatDirectoryEntry[] entries =
                    new FatDirectoryEntry[totalEntrySize];

            byte checkSum = realEntry.GetShortName().CheckSum();
            int j = 0;

            for (int i = totalEntrySize - 2; i > 0; i--) {
                entries[i] = CreatePart(fileName.Substring(j * 13, j * 13 + 13),
                        j + 1, checkSum, false);
                j++;
            }

            entries[0] = CreatePart(fileName.Substring(j * 13),
                    j + 1, checkSum, true);

            entries[totalEntrySize - 1] = realEntry;

            return entries;
        }

        public string GetName() {
            CheckValid();

            return fileName;
        }

        public void SetName(string newName) {
            CheckWritable();

            if (!parent.IsFreeName(newName)) {
                throw new IOException(
                        "the name \"" + newName + "\" is already in use");
            }

            parent.UnlinkEntry(this);
            fileName = newName;
            parent.LinkEntry(this);
        }

        /// <summary>
        /// Moves this entry to a new directory under the specified name.
        /// </summary>
        /// <param name="target">the direcrory where this entry should be moved to</param>
        /// <param name="newName">the new name under which this entry will be accessible
        ///     in the target directory</param>
        /// <exception cref="IOException">IOException on error moving this entry</exception>
        /// <exception cref="ReadOnlyException">ReadOnlyException if this directory is read-only</exception>
        public void MoveTo(FatLfnDirectory target, string newName) {
            CheckWritable();

            if (!target.IsFreeName(newName)) {
                throw new IOException(
                        "the name \"" + newName + "\" is already in use");
            }

            parent.UnlinkEntry(this);
            parent = target;
            fileName = newName;
            parent.LinkEntry(this);
        }

        public void SetLastModified(long lastModified) {
            CheckWritable();
            realEntry.SetLastModified(lastModified);
        }

        public IFsFile GetFile() {
            return parent.GetFile(realEntry);
        }

        public IFsDirectory GetDirectory() {
            return parent.GetDirectory(realEntry);
        }

        private static FatDirectoryEntry CreatePart(string subName,
                int ordinal, byte checkSum, bool isLast) {

            char[] unicodechar = new char[13];
            char[] c2 = subName.ToCharArray();
            Array.Copy(c2, unicodechar, subName.Length);

            for (int i = subName.Length; i < 13; i++) {
                if (i == subName.Length) {
                    unicodechar[i] = (char)0x0000;
                }
                else {
                    unicodechar[i] = (char)0xffff;
                }
            }

            byte[] rawData = new byte[FatDirectoryEntry.SIZE];

            if (isLast) {
                LittleEndian.SetInt8(rawData, 0, ordinal + (1 << 6));
            }
            else {
                LittleEndian.SetInt8(rawData, 0, ordinal);
            }

            LittleEndian.SetInt16(rawData, 1, unicodechar[0]);
            LittleEndian.SetInt16(rawData, 3, unicodechar[1]);
            LittleEndian.SetInt16(rawData, 5, unicodechar[2]);
            LittleEndian.SetInt16(rawData, 7, unicodechar[3]);
            LittleEndian.SetInt16(rawData, 9, unicodechar[4]);
            LittleEndian.SetInt8(rawData, 11, 0x0f); // this is the hidden
                                                     // attribute tag for
                                                     // lfn
            LittleEndian.SetInt8(rawData, 12, 0); // reserved
            LittleEndian.SetInt8(rawData, 13, checkSum); // checksum
            LittleEndian.SetInt16(rawData, 14, unicodechar[5]);
            LittleEndian.SetInt16(rawData, 16, unicodechar[6]);
            LittleEndian.SetInt16(rawData, 18, unicodechar[7]);
            LittleEndian.SetInt16(rawData, 20, unicodechar[8]);
            LittleEndian.SetInt16(rawData, 22, unicodechar[9]);
            LittleEndian.SetInt16(rawData, 24, unicodechar[10]);
            LittleEndian.SetInt16(rawData, 26, 0); // sector... unused
            LittleEndian.SetInt16(rawData, 28, unicodechar[11]);
            LittleEndian.SetInt16(rawData, 30, unicodechar[12]);

            return new FatDirectoryEntry(rawData, false);
        }

        public long GetLastModified() {
            return realEntry.GetLastModified();
        }

        public long GetCreated() {
            return realEntry.GetCreated();
        }

        public long GetLastAccessed() {
            return realEntry.GetLastAccessed();
        }

        public bool IsFile() {
            return realEntry.IsFile();
        }

        public bool IsDirectory() {
            return realEntry.IsDirectory();
        }

        public bool IsDirty() {
            return realEntry.IsDirty();
        }

    }

}