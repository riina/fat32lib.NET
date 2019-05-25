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
    /// A chain of clusters as stored in a <see cref="Fat"/>.
    /// </summary>
    public sealed class ClusterChain : AbstractFsObject {
        private readonly Fat fat;
        private readonly IBlockDevice device;
        private readonly int clusterSize;
        private readonly long dataOffset;

        private long startCluster;

        /// <summary>
        /// Creates a new <see cref="ClusterChain"/> that contains no clusters.
        /// </summary>
        /// <param name="fat">the <see cref="Fat"/> that holds the new chain</param>
        /// <param name="readOnly">if the chain should be created read-only</param>
        public ClusterChain(Fat fat, bool readOnly) : this(fat, 0, readOnly) {
        }

        public ClusterChain(Fat fat, long startCluster, bool readOnly) : base(readOnly) {

            this.fat = fat;

            if (startCluster != 0) {
                this.fat.TestCluster(startCluster);

                if (this.fat.IsFreeCluster(startCluster))
                    throw new ArgumentException(
                        "cluster " + startCluster + " is free");
            }

            device = fat.GetDevice();
            dataOffset = FatUtils.GetFilesOffset(fat.GetBootSector());
            this.startCluster = startCluster;
            clusterSize = fat.GetBootSector().GetBytesPerCluster();
        }

        public int GetClusterSize() {
            return clusterSize;
        }

        public Fat GetFat() {
            return fat;
        }

        public IBlockDevice GetDevice() {
            return device;
        }

        /// <summary>
        /// Returns the first cluster of this chain.
        /// </summary>
        /// <returns>the chain's first cluster, which may be 0 if this chain does
        /// not contain any clusters</returns>
        public long GetStartCluster() {
            return startCluster;
        }

        /// <summary>
        /// Calculates the device offset (0-based) for the given cluster and offset
        /// within the cluster.
        /// </summary>
        /// <param name="cluster"></param>
        /// <param name="clusterOffset"></param>
        /// <returns>long</returns>
        private long GetDevOffset(long cluster, int clusterOffset) {
            return dataOffset + clusterOffset +
                    ((cluster - Fat.FIRST_CLUSTER) * clusterSize);
        }

        /// <summary>
        /// Returns the size this <see cref="ClusterChain"/> occupies on the device.
        /// </summary>
        /// <returns>the size this chain occupies on the device in bytes</returns>
        public long GetLengthOnDisk() {
            if (GetStartCluster() == 0) return 0;

            return GetChainLength() * clusterSize;
        }

        /// <summary>
        /// Sets the length of this <see cref="ClusterChain"/> in bytes. Because a
        /// <see cref="ClusterChain"/> can only contain full clusters, the new size
        /// will always be a multiple of the cluster size.
        /// </summary>
        /// <param name="size">size the desired number of bytes the can be stored in
        ///     this <see cref="ClusterChain"/></param>
        /// <returns>the true number of bytes this <see cref="ClusterChain"/> can contain</returns>
        /// <exception cref="IOException">IOException on error setting the new size</exception>
        /// <seealso cref="SetChainLength(int)"/>
        public long SetSize(long size) {
            long nrClusters = ((size + clusterSize - 1) / clusterSize);
            if (nrClusters > int.MaxValue)
                throw new IOException("too many clusters");

            SetChainLength((int)nrClusters);

            return clusterSize * nrClusters;
        }

        /// <summary>
        /// Determines the length of this <see cref="ClusterChain"/> in clusters.
        /// </summary>
        /// <returns>the length of this chain</returns>
        public int GetChainLength() {
            if (GetStartCluster() == 0) return 0;

            long[] chain = GetFat().GetChain(GetStartCluster());
            return chain.Length;
        }

        /// <summary>
        /// Sets the length of this cluster chain in clusters.
        /// </summary>
        /// <param name="nrClusters">nrClusters the new number of clusters this chain should contain,
        ///     must be >= 0</param>
        /// <exception cref="IOException">IOException on error updating the chain length</exception>
        /// <seealso cref="SetSize(long)"/>
        public void SetChainLength(int nrClusters) {
            if (nrClusters < 0) throw new ArgumentException(
                    "negative cluster count");

            if ((this.startCluster == 0) && (nrClusters == 0)) {
                /* nothing to do */
            }
            else if ((this.startCluster == 0) && (nrClusters > 0)) {
                long[] chain = fat.AllocNew(nrClusters);
                startCluster = chain[0];
            }
            else {
                long[] chain = fat.GetChain(startCluster);

                if (nrClusters != chain.Length) {
                    if (nrClusters > chain.Length) {
                        /* grow the chain */
                        int count = nrClusters - chain.Length;

                        while (count > 0) {
                            fat.AllocAppend(GetStartCluster());
                            count--;
                        }
                    }
                    else {
                        /* shrink the chain */
                        if (nrClusters > 0) {
                            fat.SetEof(chain[nrClusters - 1]);
                            for (int i = nrClusters; i < chain.Length; i++) {
                                fat.SetFree(chain[i]);
                            }
                        }
                        else {
                            for (int i = 0; i < chain.Length; i++) {
                                fat.SetFree(chain[i]);
                            }

                            startCluster = 0;
                        }
                    }
                }
            }
        }

        public void ReadData(long offset, MemoryStream dest) {

            int len = (int)(dest.Length - dest.Position);

            if ((startCluster == 0 && len > 0)) throw new EndOfStreamException();

            long[] chain = GetFat().GetChain(startCluster);
            IBlockDevice dev = GetDevice();

            int chainIdx = (int)(offset / clusterSize);
            if (offset % clusterSize != 0) {
                int clusOfs = (int)(offset % clusterSize);
                int size = Math.Min(len,
                        (int)(clusterSize - (offset % clusterSize) - 1));
                dest.SetLength(dest.Position + size);

                dev.Read(GetDevOffset(chain[chainIdx], clusOfs), dest);

                offset += size;
                len -= size;
                chainIdx++;
            }

            while (len > 0) {
                int size = Math.Min(clusterSize, len);
                dest.SetLength(dest.Position + size);

                dev.Read(GetDevOffset(chain[chainIdx], 0), dest);

                len -= size;
                chainIdx++;
            }
        }

        /// <summary>
        /// Writes data to this cluster chain, possibly growing the chain so it
        /// can store the additional data. When this method returns without throwing
        /// an exception, the buffer's <see cref="MemoryStream.Position"/> will
        /// equal it's <see cref="MemoryStream.Length"/>, and the length will not
        /// have changed. This is not guaranteed if writing fails.
        /// </summary>
        /// <param name="offset">the offset where to write the first byte from the buffer</param>
        /// <param name="srcBuf">the buffer to write to this <see cref="ClusterChain"/></param>
        /// <exception cref="IOException">IOException on write error</exception>
        public void WriteData(long offset, MemoryStream srcBuf) {

            int len = (int)(srcBuf.Length - srcBuf.Position);

            if (len == 0) return;

            long minSize = offset + len;
            if (GetLengthOnDisk() < minSize) {
                SetSize(minSize);
            }

            long[] chain = fat.GetChain(GetStartCluster());

            int chainIdx = (int)(offset / clusterSize);
            if (offset % clusterSize != 0) {
                int clusOfs = (int)(offset % clusterSize);
                int size = Math.Min(len,
                        (int)(clusterSize - (offset % clusterSize)));
                srcBuf.SetLength(srcBuf.Position + size);

                device.Write(GetDevOffset(chain[chainIdx], clusOfs), srcBuf);

                offset += size;
                len -= size;
                chainIdx++;
            }

            while (len > 0) {
                int size = Math.Min(clusterSize, len);
                srcBuf.SetLength(srcBuf.Position + size);

                device.Write(GetDevOffset(chain[chainIdx], 0), srcBuf);

                len -= size;
                chainIdx++;
            }

        }

    }

}