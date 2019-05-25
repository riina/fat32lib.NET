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

namespace FAT32Lib.Fat {

    public sealed class Fat {

        /// <summary>
        /// The first cluster that really holds user data in a FAT.
        /// </summary>
        public const int FIRST_CLUSTER = 2;

        private readonly long[] entries;
        private readonly FatType fatType;
        private readonly int sectorCount;
        private readonly int sectorSize;
        private readonly IBlockDevice device;
        private readonly BootSector bs;
        private readonly long offset;
        private readonly int lastClusterIndex;

        private int lastAllocatedCluster;

        /// <summary>
        /// Reads a <see cref="Fat"/> as specified by a <see cref="BootSector"/>.
        /// </summary>
        /// <param name="bs">the boot sector specifying the <see cref="Fat"/> layout</param>
        /// <param name="fatNr">the number of the <see cref="Fat"/> to read</param>
        /// <returns>the <see cref="Fat"/> that was read</returns>
        /// <exception cref="IOException">IOException on read error</exception>
        /// <exception cref="ArgumentException">ArgumentException if fatNr is greater than
        ///     <see cref="BootSector.GetNrFats"/></exception>
        public static Fat Read(BootSector bs, int fatNr) {

            if (fatNr > bs.GetNrFats()) {
                throw new ArgumentException(
                        "boot sector says there are only " + bs.GetNrFats() +
                        " FATs when reading FAT #" + fatNr);
            }

            long fatOffset = FatUtils.GetFatOffset(bs, fatNr);
            Fat result = new Fat(bs, fatOffset);
            result.Read();
            return result;
        }

        /// Creates a new <see cref="Fat"/> as specified by a <see cref="BootSector"/>.
        /// </summary>
        /// <param name="bs">the boot sector specifying the <see cref="Fat"/> layout</param>
        /// <param name="fatNr">the number of the <see cref="Fat"/> to create</param>
        /// <returns>the <see cref="Fat"/> that was created</returns>
        /// <exception cref="IOException">IOException on write error</exception>
        /// <exception cref="ArgumentException">ArgumentException if fatNr is greater than
        ///     <see cref="BootSector.GetNrFats"/></exception>
        public static Fat Create(BootSector bs, int fatNr) {

            if (fatNr > bs.GetNrFats()) {
                throw new ArgumentException(
                        "boot sector says there are only " + bs.GetNrFats() +
                        " FATs when creating FAT #" + fatNr);
            }

            long fatOffset = FatUtils.GetFatOffset(bs, fatNr);
            Fat result = new Fat(bs, fatOffset);

            if (bs.GetDataClusterCount() > result.entries.Length)
                throw new IOException("FAT too small for device");

            result.Init(bs.GetMediumDescriptor());
            result.Write();
            return result;
        }

        private Fat(BootSector bs, long offset) {
            this.bs = bs;
            fatType = bs.GetFatType();
            if (bs.GetSectorsPerFat() > int.MaxValue)
                throw new ArgumentException("FAT too large");

            if (bs.GetSectorsPerFat() <= 0) throw new IOException(
                    "boot sector says there are " + bs.GetSectorsPerFat() +
                    " sectors per FAT");

            if (bs.GetBytesPerSector() <= 0) throw new IOException(
                    "boot sector says there are " + bs.GetBytesPerSector() +
                    " bytes per sector");

            sectorCount = (int)bs.GetSectorsPerFat();
            sectorSize = bs.GetBytesPerSector();
            device = bs.GetDevice();
            this.offset = offset;
            lastAllocatedCluster = FIRST_CLUSTER;

            if (bs.GetDataClusterCount() > int.MaxValue) throw
                    new IOException("too many data clusters");

            if (bs.GetDataClusterCount() == 0) throw
                    new IOException("no data clusters");

            lastClusterIndex = (int)bs.GetDataClusterCount() + FIRST_CLUSTER;

            entries = new long[(int)((sectorCount * sectorSize) /
                    fatType.GetEntrySize())];

            if (lastClusterIndex > entries.Length) throw new IOException(
                "file system has " + lastClusterIndex +
                "clusters but only " + entries.Length + " FAT entries");
        }

        public FatType GetFatType() {
            return fatType;
        }

        /// <summary>
        /// Returns the <see cref="BootSector"/> that specifies this <see cref="Fat"/>.
        /// </summary>
        /// <returns>this <see cref="Fat"/>'s <see cref="BootSector"/></returns>
        public BootSector GetBootSector() {
            return this.bs;
        }

        /// <summary>
        /// Returns the <see cref="IBlockDevice"/> where this <see cref="Fat"/> is stored.
        /// </summary>
        /// <returns>the device holding this FAT</returns>
        public IBlockDevice GetDevice() {
            return device;
        }

        private void Init(int mediumDescriptor) {
            entries[0] =
                    (mediumDescriptor & 0xFFL) |
                    (0xFFFFF00L & fatType.GetBitMask());
            entries[1] = fatType.GetEofMarker();
        }

        /// <summary>
        /// Read the contents of this FAT from the given device at the given offset.
        /// </summary>
        /// <exception cref="IOException">IOException on read error</exception>
        private void Read() {
            byte[] data = new byte[sectorCount * sectorSize];
            device.Read(offset, new MemoryStream(data));

            for (int i = 0; i < entries.Length; i++)
                entries[i] = fatType.ReadEntry(data, i);
        }

        public void Write() {
            WriteCopy(offset);
        }

        /// <summary>
        /// Write the contents of this FAT to the given device at the given offset.
        /// </summary>
        /// <param name="offset">the device offset where to write the FAT copy</param>
        /// <exception cref="IOException">IOException on write error</exception>
        public void WriteCopy(long offset) {
            byte[] data = new byte[sectorCount * sectorSize];

            for (int index = 0; index < entries.Length; index++) {
                fatType.WriteEntry(data, index, entries[index]);
            }

            device.Write(offset, new MemoryStream(data));
        }

        /// <summary>
        /// Gets the medium descriptor byte
        /// </summary>
        /// <returns>int</returns>
        public int GetMediumDescriptor() {
            return (int)(entries[0] & 0xFF);
        }

        /// <summary>
        /// Gets the entry at a given offset
        /// </summary>
        /// <param name="index"></param>
        /// <returns>long</returns>
        public long GetEntry(int index) {
            return entries[index];
        }

        /// <summary>
        /// Returns the last free cluster that was accessed in this FAT.
        /// </summary>
        /// <returns>the last seen free cluster</returns>
        public int GetLastFreeCluster() {
            return lastAllocatedCluster;
        }

        public long[] GetChain(long startCluster) {
            TestCluster(startCluster);
            // Count the chain first
            int count = 1;
            long cluster = startCluster;
            while (!IsEofCluster(entries[(int)cluster])) {
                count++;
                cluster = entries[(int)cluster];
            }
            // Now create the chain
            long[] chain = new long[count];
            chain[0] = startCluster;
            cluster = startCluster;
            int i = 0;
            while (!IsEofCluster(entries[(int)cluster])) {
                cluster = entries[(int)cluster];
                chain[++i] = cluster;
            }
            return chain;
        }

        /// <summary>
        /// Gets the cluster after the given cluster
        /// </summary>
        /// <param name="cluster"></param>
        /// <returns>long The next cluster number or -1 which means eof.</returns>
        public long GetNextCluster(long cluster) {
            TestCluster(cluster);
            long entry = entries[(int)cluster];
            if (IsEofCluster(entry)) {
                return -1;
            }
            else {
                return entry;
            }
        }

        /// <summary>
        /// Allocate a cluster for a new file
        /// </summary>
        /// <returns>long the number of the newly allocated cluster</returns>
        /// <exception cref="IOException">IOException if there are no free clusters</exception>
        public long AllocNew() {

            int i;
            int entryIndex = -1;

            for (i = lastAllocatedCluster; i < lastClusterIndex; i++) {
                if (IsFreeCluster(i)) {
                    entryIndex = i;
                    break;
                }
            }

            if (entryIndex < 0) {
                for (i = FIRST_CLUSTER; i < lastAllocatedCluster; i++) {
                    if (IsFreeCluster(i)) {
                        entryIndex = i;
                        break;
                    }
                }
            }

            if (entryIndex < 0) {
                throw new IOException(
                        "FAT Full (" + (lastClusterIndex - FIRST_CLUSTER)
                        + ", " + i + ")");
            }

            entries[entryIndex] = fatType.GetEofMarker();
            lastAllocatedCluster = entryIndex % lastClusterIndex;
            if (lastAllocatedCluster < FIRST_CLUSTER)
                lastAllocatedCluster = FIRST_CLUSTER;

            return entryIndex;
        }

        /// <summary>
        /// Returns the number of clusters that are currently not in use by this FAT.
        /// This estimate does only account for clusters that are really available in
        /// the data portion of the file system, not for clusters that might only
        /// theoretically be stored in the <see cref="Fat"/>.
        /// </summary>
        /// <returns>the free cluster count</returns>
        /// <seealso cref="FsInfoSector.SetFreeClusterCount(long)"/>
        /// <seealso cref="FsInfoSector.GetFreeClusterCount"/>
        /// <seealso cref="BootSector.GetDataClusterCount"/>
        public int GetFreeClusterCount() {
            int result = 0;

            for (int i = FIRST_CLUSTER; i < lastClusterIndex; i++) {
                if (IsFreeCluster(i)) result++;
            }

            return result;
        }

        /// <summary>
        /// Returns the cluster number that was last allocated in this fat.
        /// </summary>
        /// <returns></returns>
        public int GetLastAllocatedCluster() {
            return this.lastAllocatedCluster;
        }

        /// <summary>
        /// Allocate a series of clusters for a new file.
        /// </summary>
        /// <param name="nrClusters">the number of clusters to allocate</param>
        /// <returns>long</returns>
        /// <exception cref="IOException">IOException if there are no free clusters</exception>
        public long[] AllocNew(int nrClusters) {
            long[] rc = new long[nrClusters];

            rc[0] = AllocNew();
            for (int i = 1; i < nrClusters; i++) {
                rc[i] = AllocAppend(rc[i - 1]);
            }

            return rc;
        }

        /// <summary>
        /// Allocate a cluster to append to a new file
        /// </summary>
        /// <param name="cluster">a cluster from a chain where the new cluster should be
        ///     appended</param>
        /// <returns>long the newly allocated and appended cluster number</returns>
        /// <exception cref="IOException">IOException if there are no free clusters</exception>
        public long AllocAppend(long cluster) {
            TestCluster(cluster);

            while (!IsEofCluster(entries[(int)cluster])) {
                cluster = entries[(int)cluster];
            }

            long newCluster = AllocNew();
            entries[(int)cluster] = newCluster;

            return newCluster;
        }

        public void SetEof(long cluster) {
            TestCluster(cluster);
            entries[(int)cluster] = fatType.GetEofMarker();
        }

        public void SetFree(long cluster) {
            TestCluster(cluster);
            entries[(int)cluster] = 0;
        }

        /// <summary>
        /// Is the given entry a free cluster?
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>bool</returns>
        public bool IsFreeCluster(long entry) {
            if (entry > int.MaxValue) throw new ArgumentException();
            return (entries[(int)entry] == 0);
        }

        /// <summary>
        /// Is the given entry a reserved cluster?
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>bool</returns>
        public bool IsReservedCluster(long entry) {
            return fatType.IsReservedCluster(entry);
        }

        /// <summary>
        /// Is the given entry an EOF marker
        /// </summary>
        /// <param name="entry"></param>
        /// <returns>bool</returns>
        public bool IsEofCluster(long entry) {
            return fatType.IsEofCluster(entry);
        }

        public void TestCluster(long cluster) {
            if ((cluster < FIRST_CLUSTER) || (cluster >= entries.Length)) {
                throw new ArgumentException(
                        "invalid cluster value " + cluster);
            }
        }

        public override bool Equals(object obj) {
            if (obj is Fat other) {
                if (fatType != other.fatType) return false;
                if (sectorCount != other.sectorCount) return false;
                if (sectorSize != other.sectorSize) return false;
                if (lastClusterIndex != other.lastClusterIndex) return false;
                if (entries.Length != other.entries.Length)
                    return false;
                int len = Math.Min(entries.Length, other.entries.Length);
                for (int i = 0; i < len; i++)
                    if (entries[i] != other.entries[i])
                        return false;
                if (GetMediumDescriptor() != other.GetMediumDescriptor())
                    return false;
                return true;
            }
            return false;
        }
    }
}