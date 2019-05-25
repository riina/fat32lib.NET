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
using System.Collections.Generic;
using System.IO;

namespace FAT32Lib.Fat {

    /// <summary>
    /// This is the abstract base class for all directory implementations.
    /// </summary>
    abstract class AbstractDirectory {

        /// <summary>
        /// The maximum length of the volume label.
        /// </summary>
        /// <seealso cref="SetLabel(string)"/>
        public const int MAX_LABEL_LENGTH = 11;

        private readonly List<FatDirectoryEntry> entries;
        private readonly bool readOnly;
        private readonly bool isRoot;

        private bool dirty;
        private int capacity;
        private string volumeLabel;

        /// <summary>
        /// Creates a new instance of <see cref="AbstractDirectory"/>.
        /// </summary>
        /// <param name="capacity">the initial capacity of the new instance</param>
        /// <param name="readOnly">if the instance should be read-only</param>
        /// <param name="isRoot">if the new <see cref="AbstractDirectory"/> represents a root
        ///     directory</param>
        protected AbstractDirectory(
                int capacity, bool readOnly, bool isRoot) {

            entries = new List<FatDirectoryEntry>();
            this.capacity = capacity;
            this.readOnly = readOnly;
            this.isRoot = isRoot;
        }

        /// <summary>
        /// Gets called when the <see cref="AbstractDirectory"/> must read it's content
        /// off the backing storage. This method must always fill the buffer's
        /// remaining space with the bytes making up this directory, beginning with
        /// the first byte.
        /// </summary>
        /// <param name="data">the <see cref="MemoryStream"/> to fill</param>
        /// <exception cref="IOException">IOException on read error</exception>
        protected abstract void Read(MemoryStream data);

        /// <summary>
        /// Gets called when the <see cref="AbstractDirectory"/> wants to write it's
        /// contents to the backing storage. This method is expected to write the
        /// buffer's remaining data to the storage, beginning with the first byte.
        /// </summary>
        /// <param name="data">the <see cref="MemoryStream"/> to write</param>
        /// <exception cref="IOException">IOException on read error</exception>
        protected abstract void Write(MemoryStream data);

        /// <summary>
        /// Returns the number of the cluster where this directory is stored. This
        /// is important when creating the ".." entry in a sub-directory, as this
        /// entry must poing to the storage cluster of it's parent.
        /// </summary>
        /// <returns>this directory's storage cluster</returns>
        protected abstract long GetStorageCluster();

        /// <summary>
        /// Gets called by the <see cref="AbstractDirectory"/> when it has determined that
        /// it should resize because the number of entries has changed.
        /// </summary>
        /// <param name="entryCount">the new number of entries this directory needs to store</param>
        /// <exception cref="IOException">IOException on write error</exception>
        /// <exception cref="DirectoryFullException">DirectoryFullException if the FAT12/16 root directory is full</exception>
        /// <seealso cref="SizeChanged(long)"/>
        /// <seealso cref="GetEntryCount"/>
        internal abstract void ChangeSize(int entryCount);

        /// <summary>
        /// Replaces all entries in this directory.
        /// </summary>
        /// <param name="newEntries">the new directory entries</param>
        public void SetEntries(List<FatDirectoryEntry> newEntries) {
            if (newEntries.Count > capacity)
                throw new ArgumentException("too many entries");

            entries.Clear();
            entries.AddRange(newEntries);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="newSize">the new storage space for the directory in bytes</param>
        /// <seealso cref="ChangeSize(int)"/>
        protected void SizeChanged(long newSize) {
            long newCount = newSize / FatDirectoryEntry.SIZE;
            if (newCount > int.MaxValue)
                throw new IOException("directory too large");

            this.capacity = (int)newCount;
        }

        public FatDirectoryEntry GetEntry(int idx) {
            return entries[idx];
        }

        /// <summary>
        /// Returns the current capacity of this <see cref="AbstractDirectory"/>.
        /// </summary>
        /// <returns>the number of entries this directory can hold in its current
        ///     storage space</returns>
        /// <seealso cref="ChangeSize(int)"/>
        public int GetCapacity() {
            return capacity;
        }

        /// <summary>
        /// The number of entries that are currently stored in this
        /// <see cref="AbstractDirectory"/>. 
        /// </summary>
        /// <returns>the current number of directory entries</returns>
        public int GetEntryCount() {
            return entries.Count;
        }

        public bool IsReadOnly() {
            return readOnly;
        }

        public bool IsRoot() {
            return isRoot;
        }

        /// <summary>
        ///  Gets the number of directory entries in this directory. This is the
        /// number of "real" entries in this directory, possibly plus one if a
        /// volume label is set.
        /// </summary>
        /// <returns>the number of entries in this directory</returns>
        public int GetSize() {
            return entries.Count + ((volumeLabel != null) ? 1 : 0);
        }

        /// <summary>
        /// Mark this directory as dirty.
        /// </summary>
        internal void SetDirty() {
            dirty = true;
        }

        /// <summary>
        /// Checks if this <see cref="AbstractDirectory"/> is a root directory.
        /// </summary>
        /// <exception cref="NotSupportedException">NotSupportedException if this is not a root directory</exception>
        /// <seealso cref="IsRoot"/>
        private void CheckRoot() {
            if (!IsRoot()) {
                throw new NotSupportedException(
                        "only supported on root directories");
            }
        }

        /// <summary>
        /// Mark this directory as not dirty.
        /// </summary>
        private void ResetDirty() {
            dirty = false;
        }

        /// <summary>
        /// Flush the contents of this directory to the persistent storage
        /// </summary>
        public void Flush() {

            byte[] dataA = new byte[GetCapacity() * FatDirectoryEntry.SIZE];
            MemoryStream data = new MemoryStream(dataA);

            for (int i = 0; i < entries.Count; i++) {
                FatDirectoryEntry entry = entries[i];

                if (entry != null) {
                    entry.Write(data);
                }
            }

            /* TODO: the label could be placed directly the dot entries */

            if (volumeLabel != null) {
                FatDirectoryEntry labelEntry =
                        FatDirectoryEntry.CreateVolumeLabel(volumeLabel);

                labelEntry.Write(data);
            }

            if (data.Length - data.Position > 0) {
                FatDirectoryEntry.WriteNullEntry(data);
            }

            data.SetLength(data.Position);
            data.Position = 0;

            Write(data);
            ResetDirty();
        }

        internal void Read() {
            byte[] dataA = new byte[GetCapacity() * FatDirectoryEntry.SIZE];
            MemoryStream data = new MemoryStream(dataA);

            Read(data);
            data.SetLength(data.Position);
            data.Position = 0;

            for (int i = 0; i < GetCapacity(); i++) {
                FatDirectoryEntry e =
                        FatDirectoryEntry.Read(data, IsReadOnly());

                if (e == null) break;

                if (e.IsVolumeLabel()) {
                    if (!this.isRoot) throw new IOException(
                            "volume label in non-root directory");

                    this.volumeLabel = e.GetVolumeLabel();
                }
                else {
                    entries.Add(e);
                }
            }
        }

        public void AddEntry(FatDirectoryEntry e) {
            if (e == null)
                throw new Exception();

            if (GetSize() == GetCapacity()) {
                ChangeSize(GetCapacity() + 1);
            }

            entries.Add(e);
        }

        public void AddEntries(FatDirectoryEntry[] entries) {

            if (GetSize() + entries.Length > GetCapacity()) {
                ChangeSize(GetSize() + entries.Length);
            }

            this.entries.AddRange(entries);
        }

        public void RemoveEntry(FatDirectoryEntry entry) {
            if (entry == null)
                throw new Exception();

            entries.Remove(entry);
            ChangeSize(GetSize());
        }

        /// <summary>
        /// Returns the volume label that is stored in this directory. Reading the
        /// volume label is only supported for the root directory.
        /// </summary>
        /// <returns>the volume label stored in this directory, or null</returns>
        /// <exception cref="NotSupportedException">NotSupportedException if this is not a root directory</exception>
        /// <seealso cref="IsRoot"/>
        public string GetLabel() {
            CheckRoot();

            return volumeLabel;
        }

        public FatDirectoryEntry CreateSub(Fat fat) {
            ClusterChain chain = new ClusterChain(fat, false);
            chain.SetChainLength(1);

            FatDirectoryEntry entry = FatDirectoryEntry.Create(true);
            entry.SetStartCluster(chain.GetStartCluster());

            ClusterChainDirectory dir =
                    new ClusterChainDirectory(chain, false);

            /* add "." entry */

            FatDirectoryEntry dot = FatDirectoryEntry.Create(true);
            dot.SetShortName(ShortName.DOT);
            dot.SetStartCluster(dir.GetStorageCluster());
            CopyDateTimeFields(entry, dot);
            dir.AddEntry(dot);

            /* add ".." entry */

            FatDirectoryEntry dotDot = FatDirectoryEntry.Create(true);
            dotDot.SetShortName(ShortName.DOT_DOT);
            dotDot.SetStartCluster(GetStorageCluster());
            CopyDateTimeFields(entry, dotDot);
            dir.AddEntry(dotDot);

            dir.Flush();

            return entry;
        }

        private static void CopyDateTimeFields(
                FatDirectoryEntry src, FatDirectoryEntry dst) {

            dst.SetCreated(src.GetCreated());
            dst.SetLastAccessed(src.GetLastAccessed());
            dst.SetLastModified(src.GetLastModified());
        }

        /// <summary>
        /// Sets the volume label that is stored in this directory. Setting the
        /// volume label is supported on the root directory only.
        /// </summary>
        /// <param name="label">the new volume label</param>
        /// <exception cref="ArgumentException">ArgumentException if the label is too long</exception>
        /// <exception cref="NotSupportedException">NotSupportedException if this is not a root directory</exception>
        /// <seealso cref="IsRoot"/>
        public void SetLabel(string label) {

            CheckRoot();

            if (label.Length > MAX_LABEL_LENGTH) throw new
                     ArgumentException("label too long");

            if (volumeLabel != null) {
                if (label == null) {
                    ChangeSize(GetSize() - 1);
                    volumeLabel = null;
                }
                else {
                    ShortName.CheckValidChars(label.ToCharArray());
                    volumeLabel = label;
                }
            }
            else {
                if (label != null) {
                    ChangeSize(GetSize() + 1);
                    ShortName.CheckValidChars(label.ToCharArray());
                    volumeLabel = label;
                }
            }

            dirty = true;
        }

    }

}