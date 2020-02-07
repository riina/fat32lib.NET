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
using System.IO;

namespace FAT32Lib.Fat {

    /// <summary>
    /// The in-memory representation of a single file (chain of clusters) on a
    /// FAT file system.
    /// </summary>
    public sealed class FatFile : AbstractFsObject, IFsFile {
        private readonly FatDirectoryEntry entry;
        private readonly ClusterChain chain;

        private FatFile(FatDirectoryEntry myEntry, ClusterChain chain) : base(myEntry.IsReadOnly()) {
            entry = myEntry;
            this.chain = chain;
        }

        internal static FatFile Get(Fat fat, FatDirectoryEntry entry) {

            if (entry.IsDirectory())
                throw new ArgumentException(entry + " is a directory");

            var cc = new ClusterChain(
                    fat, entry.GetStartCluster(), entry.IsReadonlyFlag());

            if (entry.GetLength() > cc.GetLengthOnDisk()) throw new IOException(
                    "entry is larger than associated cluster chain");

            return new FatFile(entry, cc);
        }

        /// <summary>
        /// Returns the length of this file in bytes. This is the length that
        /// is stored in the directory entry that is associated with this file.
        /// </summary>
        /// <returns>long the length that is recorded for this file</returns>
        public long GetLength() {
            CheckValid();

            return entry.GetLength();
        }

        /// <summary>
        /// Sets the size (in bytes) of this file. Because
        /// <see cref="Write(long, MemoryStream)"/> to the file will grow
        /// it automatically if needed, this method is mainly usefull for truncating
        /// a file.
        /// </summary>
        /// <param name="length">the new length of the file in bytes</param>
        /// <exception cref="ReadOnlyException">ReadOnlyException if this file is read-only</exception>
        /// <exception cref="IOException">IOException on error updating the file size</exception>
        public void SetLength(long length) {
            CheckWritable();

            if (GetLength() == length) return;

            UpdateTimeStamps(true);
            chain.SetSize(length);

            entry.SetStartCluster(chain.GetStartCluster());
            entry.SetLength(length);
        }

        /// <summary>
        /// Unless this file is <see cref="AbstractFsObject.IsReadOnly"/>, this method also
        /// updates the "last accessed" field in the directory entry that is
        /// associated with this file.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="dest"></param>
        /// <seealso cref="FatDirectoryEntry.SetLastAccessed(long)"/>
        public void Read(long offset, MemoryStream dest) {
            CheckValid();

            var len = (int)(dest.Length - dest.Position);

            if (len == 0) return;

            if (offset + len > GetLength()) {
                throw new EndOfStreamException();
            }

            if (!IsReadOnly()) {
                UpdateTimeStamps(false);
            }

            chain.ReadData(offset, dest);
        }

        /// <summary>
        /// If the data to be written extends beyond the current
        /// <see cref="GetLength"/> length of this file, an attempt is made to
        /// <see cref="SetLength(long)"/> grow the file so that the data will fit.
        /// Additionally, this method updates the "last accessed" and "last modified"
         /// fields on the directory entry that is associated with this file.
        /// </summary>
        /// <param name="offset"></param>
        /// <param name="srcBuf"></param>
        public void Write(long offset, MemoryStream srcBuf) {
            CheckWritable();

            UpdateTimeStamps(true);

            var lastByte = offset + (srcBuf.Length - srcBuf.Position);

            if (lastByte > GetLength()) {
                SetLength(lastByte);
            }

            chain.WriteData(offset, srcBuf);
        }

        private void UpdateTimeStamps(bool write) {
            var now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            entry.SetLastAccessed(now);

            if (write) {
                entry.SetLastModified(now);
            }
        }

        /// <summary>
        /// Has no effect besides possibly throwing an <see cref="ReadOnlyException"/>. To
        /// make sure that all data is written out to disk use the
        /// <see cref="FatFileSystem.Flush"/> method.
        /// </summary>
        /// <exception cref="ReadOnlyException">ReadOnlyException if this {@code FatFile} is read-only</exception>
        public void Flush() {
            CheckWritable();

            /* nothing else to do */
        }

        /// <summary>
        /// Returns the <see cref="ClusterChain"/> that holds the contents of
        /// this <see cref="FatFile"/>.
        /// </summary>
        /// <returns>the file's <see cref="ClusterChain"/></returns>
        public ClusterChain GetChain() {
            CheckValid();

            return chain;
        }

    }

}