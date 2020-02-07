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

    /// <summary>
    /// A directory that is stored in a cluster chain.
    /// </summary>
    internal sealed class ClusterChainDirectory : AbstractDirectory {

        /// <summary>
        /// According to the FAT specification, this is the maximum size a FAT
        /// directory may occupy on disk. The <see cref="ClusterChainDirectory"/> takes
        /// care not to grow beyond this limit.
        /// </summary>
        /// <seealso cref="ChangeSize(int)"/>
        public const int MAX_SIZE = 65536 * 32;

        /// <summary>
        /// The <see cref="ClusterChain"/> that stores this directory. Package-visible
        /// for testing.
        /// </summary>
        internal readonly ClusterChain Chain;

        internal ClusterChainDirectory(ClusterChain chain, bool isRoot)
                : base((int)(chain.GetLengthOnDisk() / FatDirectoryEntry.SIZE), chain.IsReadOnly(), isRoot) {
            this.Chain = chain;
        }

        public static ClusterChainDirectory ReadRoot(ClusterChain chain) {
            var result = new ClusterChainDirectory(chain, true);

            result.Read();
            return result;
        }

        public static ClusterChainDirectory CreateRoot(Fat fat) {

            if (fat.GetFatType() != FatType.BaseFat32) {
                throw new ArgumentException(
                        "only FAT32 stores root directory in a cluster chain");
            }

            var bs = (Fat32BootSector)fat.GetBootSector();
            var cc = new ClusterChain(fat, false);
            cc.SetChainLength(1);

            bs.SetRootDirFirstCluster(cc.GetStartCluster());

            var result =
                    new ClusterChainDirectory(cc, true);

            result.Flush();
            return result;
        }
        protected override void Read(MemoryStream data) {
            Chain.ReadData(0, data);
        }

        protected override void Write(MemoryStream data) {
            var toWrite = (int)(data.Length - data.Position);
            Chain.WriteData(0, data);
            var trueSize = Chain.GetLengthOnDisk();

            /* TODO: check if the code below is really needed */
            if (trueSize > toWrite) {
                var rest = (int)(trueSize - toWrite);
                var fill = new MemoryStream(rest);
                Chain.WriteData(toWrite, fill);
            }
        }

        /// <summary>
        /// Returns the first cluster of the chain that stores this directory for
        /// non-root instances or 0 if this is the root directory.
        /// </summary>
        /// <returns>the first storage cluster of this directory</returns>
        /// <seealso cref="AbstractDirectory.IsRoot"/>
        protected override long GetStorageCluster() {
            return IsRoot() ? 0 : Chain.GetStartCluster();
        }

        public void Delete() {
            Chain.SetChainLength(0);
        }

        internal override void ChangeSize(int entryCount) {
            if (entryCount == 0)
                throw new Exception();

            var size = entryCount * FatDirectoryEntry.SIZE;

            if (size > MAX_SIZE) throw new DirectoryFullException(
                    "directory would grow beyond " + MAX_SIZE + " bytes",
                    GetCapacity(), entryCount);

            SizeChanged(Chain.SetSize(Math.Max(size, Chain.GetClusterSize())));
        }

    }

}