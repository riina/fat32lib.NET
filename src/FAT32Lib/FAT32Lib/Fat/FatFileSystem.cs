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

using System.IO;

namespace FAT32Lib.Fat {

    /// <summary>
    /// <para>
    /// Implements the <see cref="IFileSystem"/> interface for the FAT family of file
    /// systems. This class always uses the "long file name" specification when
    /// writing directory entries.
    /// </para>
    /// <para>
    /// For creating (aka "formatting") FAT file systems please refer to the
    /// <see cref="SuperFloppyFormatter"/> class.
    /// </para>
    /// </summary>
    public sealed class FatFileSystem : AbstractFileSystem {

        private readonly Fat fat;
        private readonly FsInfoSector fsiSector;
        private readonly BootSector bs;
        private readonly FatLfnDirectory rootDir;
        private readonly AbstractDirectory rootDirStore;
        private readonly FatType fatType;
        private readonly long filesOffset;

        FatFileSystem(IBlockDevice api, bool readOnly) : this(api, readOnly, false) { }

        /// <summary>
        /// Constructor for FatFileSystem in specified readOnly mode
        /// </summary>
        /// <param name="device">the <see cref="IBlockDevice"/> holding the file system</param>
        /// <param name="readOnly"></param>
        /// <param name="ignoreFatDifferences"></param>
        /// <exception cref="IOException">IOException on read error</exception>
        private FatFileSystem(IBlockDevice device, bool readOnly,
                bool ignoreFatDifferences) : base(readOnly) {
            bs = BootSector.Read(device);

            if (bs.GetNrFats() <= 0) throw new IOException(
                    "boot sector says there are no FATs");

            filesOffset = FatUtils.GetFilesOffset(bs);
            fatType = bs.GetFatType();
            fat = Fat.Read(bs, 0);

            if (!ignoreFatDifferences) {
                for (var i = 1; i < bs.GetNrFats(); i++) {
                    var tmpFat = Fat.Read(bs, i);
                    if (!fat.Equals(tmpFat)) {
                        throw new IOException("FAT " + i + " differs from FAT 0");
                    }
                }
            }

            if (fatType == FatType.BaseFat32) {
                var f32Bs = (Fat32BootSector)bs;
                var rootDirFile = new ClusterChain(fat,
                        f32Bs.GetRootDirFirstCluster(), IsReadOnly());
                rootDirStore = ClusterChainDirectory.ReadRoot(rootDirFile);
                fsiSector = FsInfoSector.Read(f32Bs);

                if (fsiSector.GetFreeClusterCount() != fat.GetFreeClusterCount()) {
                    throw new IOException("free cluster count mismatch - fat: " +
                            fat.GetFreeClusterCount() + " - fsinfo: " +
                            fsiSector.GetFreeClusterCount());
                }
            }
            else {
                rootDirStore =
                        Fat16RootDirectory.Read((Fat16BootSector)bs, readOnly);
                fsiSector = null;
            }

            rootDir = new FatLfnDirectory(rootDirStore, fat, IsReadOnly());

        }

        /// <summary>
        /// Reads the file system structure from the specified <see cref="IBlockDevice"/>
        /// and returns a fresh <see cref="FatFileSystem"/> instance to read or modify it.
        /// </summary>
        /// <param name="device">the <see cref="IBlockDevice"/> holding the file system</param>
        /// <param name="readOnly">if the <see cref="FatFileSystem"/> should be in read-only mode</param>
        /// <returns>the <see cref="FatFileSystem"/> instance for the device</returns>
        /// <exception cref="IOException">IOException on read error or if the file system structure could
        ///     not be parsed</exception>
        public static FatFileSystem Read(IBlockDevice device, bool readOnly) {
            return new FatFileSystem(device, readOnly);
        }

        long GetFilesOffset() {
            CheckClosed();

            return filesOffset;
        }

        /// <summary>
        /// Returns the size of the FAT entries of this <see cref="FatFileSystem"/>.
        /// </summary>
        /// <returns>the exact type of the FAT used by this file system</returns>
        public FatType GetFatType() {
            CheckClosed();

            return fatType;
        }

        /// <summary>
        /// Returns the volume label of this file system.
        /// </summary>
        /// <returns>the volume label</returns>
        public string GetVolumeLabel() {
            CheckClosed();

            var fromDir = rootDirStore.GetLabel();

            if (fromDir == null && fatType != FatType.BaseFat32) {
                return ((Fat16BootSector)bs).GetVolumeLabel();
            }

            return fromDir;
        }

        /**
         * Sets the volume label for this file system.
         *
         * @param label the new volume label, may be {@code null}
         * @throws ReadOnlyException if the file system is read-only
         * @throws IOException on write error
         */
        /// <summary>
        /// Sets the volume label for this file system.
        /// </summary>
        /// <param name="label">the new volume label, may be null</param>
        /// <exception cref="ReadOnlyException">ReadOnlyException if the file system is read-only</exception>
        /// <exception cref="IOException">IOException on write error</exception>
        public void SetVolumeLabel(string label) {
            CheckClosed();
            CheckReadOnly();

            rootDirStore.SetLabel(label);

            if (fatType != FatType.BaseFat32) {
                ((Fat16BootSector)bs).SetVolumeLabel(label);
            }
        }

        AbstractDirectory GetRootDirStore() {
            CheckClosed();

            return rootDirStore;
        }

        /// <summary>
        /// Flush all changed structures to the device.
        /// </summary>
        /// <exception cref="IOException">IOException on write error</exception>
        public override void Flush() {
            CheckClosed();

            if (bs.IsDirty()) {
                bs.Write();
            }

            for (var i = 0; i < bs.GetNrFats(); i++) {
                fat.WriteCopy(FatUtils.GetFatOffset(bs, i));
            }

            rootDir.Flush();

            if (fsiSector != null) {
                fsiSector.SetFreeClusterCount(fat.GetFreeClusterCount());
                fsiSector.SetLastAllocatedCluster(fat.GetLastAllocatedCluster());
                fsiSector.Write();
            }
        }

        public override IFsDirectory GetRoot() {
            CheckClosed();

            return rootDir;
        }

        /// <summary>
        /// Returns the fat.
        /// </summary>
        /// <returns>Fat</returns>
        public Fat GetFat() {
            return fat;
        }

        /// <summary>
        /// Returns the bootsector.
        /// </summary>
        /// <returns>BootSector</returns>
        public BootSector GetBootSector() {
            CheckClosed();

            return bs;
        }

        /// <summary>
        /// This method shows the free space in terms of available clusters.
        /// </summary>
        /// <returns>Always -1</returns>
        public override long GetFreeSpace() {
            return fat.GetFreeClusterCount() * this.bs.GetBytesPerCluster();
        }

        /// <summary>
        /// This method is currently not implemented for <see cref="FatFileSystem"/> and
        /// always returns -1.
        /// </summary>
        /// <returns>always -1</returns>
        public override long GetTotalSpace() {
            return bs.GetDataClusterCount() * bs.GetBytesPerCluster();
        }

        /// <summary>
        /// This method is currently not implemented for <see cref="FatFileSystem"/> and
        /// always returns -1.
        /// </summary>
        /// <returns>always -1</returns>
        public override long GetUsableSpace() {
            // TODO implement me
            return -1;
        }
    }

}