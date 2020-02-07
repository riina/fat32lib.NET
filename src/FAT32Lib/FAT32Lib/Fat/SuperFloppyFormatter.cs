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

namespace FAT32Lib.Fat {

    /// <summary>
    /// <para>
    /// Allows to create FAT file systems on <see cref="IBlockDevice"/>s which follow the
    /// "super floppy" standard. This means that the device will be formatted so
    /// that it does not contain a partition table. Instead, the entire device holds
    /// a single FAT file system.
    /// </para>
    /// <para>
    /// This class follows the "builder" pattern, which means it's methods always
    /// returns the <see cref="SuperFloppyFormatter"/> instance they're called on. This
    /// allows to chain the method calls like this:
    /// <para>
    ///  BlockDevice dev = new RamDisk(16700000);
    ///  FatFileSystem fs = SuperFloppyFormatter.get(dev).
    ///  setFatType(FatType.FAT12).format();
    /// </para>
    /// </para>
    /// </summary>
    public sealed class SuperFloppyFormatter {

        /// <summary>
        /// The media descriptor used (hard disk).
        /// </summary>
        public const int MEDIUM_DESCRIPTOR_HD = 0xf8;

        /// <summary>
        /// The default number of FATs.
        /// </summary>
        public const int DEFAULT_FAT_COUNT = 2;

        /// <summary>
        /// The default number of sectors per track.
        /// </summary>
        public const int DEFAULT_SECTORS_PER_TRACK = 32;

        /// <summary>
        /// The default number of heads.
        /// </summary>
        public const int DEFAULT_HEADS = 64;

        /// <summary>
        /// The default OEM name for file systems created by this class.
        /// </summary>
        public const string DEFAULT_OEM_NAME = "fat32lib";

        private const int MAX_DIRECTORY = 512;

        private readonly IBlockDevice device;

        private string label;
        private string oemName;
        private FatType fatType;
        private int sectorsPerCluster;
        private int reservedSectors;
        private int fatCount;

        /// <summary>
        /// Creates a new <see cref="SuperFloppyFormatter"/> for the specified
        /// <see cref="IBlockDevice"/>.
        /// </summary>
        /// <param name="device"></param>
        /// <exception cref="System.IO.IOException">IOException on error accessing the specified device</exception>
        private SuperFloppyFormatter(IBlockDevice device) {
            this.device = device;
            oemName = DEFAULT_OEM_NAME;
            fatCount = DEFAULT_FAT_COUNT;
            SetFatType(FatTypeFromDevice());
        }

        /// <summary>
        /// Retruns a <see cref="SuperFloppyFormatter"/> instance suitable for formatting
        /// the specified device.
        /// </summary>
        /// <param name="dev">the device that should be formatted</param>
        /// <returns>the formatter for the device</returns>
        /// <exception cref="System.IO.IOException">IOException on error creating the formatter</exception>
        public static SuperFloppyFormatter Get(IBlockDevice dev) {
            return new SuperFloppyFormatter(dev);
        }

        /// <summary>
        /// Returns the OEM name that will be written to the <see cref="BootSector"/>.
        /// </summary>
        /// <returns>the OEM name of the new file system</returns>
        public string GetOemName() {
            return oemName;
        }
        
        /// <summary>
        /// Sets the OEM name of the boot sector.
        /// TODO: throw an exception early if name is invalid(too long, ...)
        /// </summary>
        /// <param name="oemName">the new OEM name</param>
        /// <returns>this <see cref="SuperFloppyFormatter"/></returns>
        /// <seealso cref="BootSector.SetOemName(string)"/>
        public SuperFloppyFormatter SetOemName(string oemName) {
            this.oemName = oemName;
            return this;
        }

        /// <summary>
        /// Sets the volume label of the file system to create.
        /// TODO: throw an exception early if label is invalid (too long, ...)
        /// </summary>
        /// <param name="label">label the new file system label, may be null</param>
        /// <returns>this <see cref="SuperFloppyFormatter"/></returns>
        /// <seealso cref="FatFileSystem.SetVolumeLabel(string)"/>
        public SuperFloppyFormatter SetVolumeLabel(string label) {
            this.label = label;
            return this;
        }

        /// <summary>
        /// Returns the volume label that will be given to the new file system.
        /// </summary>
        /// <returns>the file system label, may be null</returns>
        /// <seealso cref="FatFileSystem.GetVolumeLabel"/>
        public string GetVolumeLabel() {
            return label;
        }

        private void InitBootSector(BootSector bs) {

            bs.Init();
            bs.SetFileSystemTypeLabel(fatType.GetLabel());
            bs.SetNrReservedSectors(reservedSectors);
            bs.SetNrFats(fatCount);
            bs.SetSectorsPerCluster(sectorsPerCluster);
            bs.SetMediumDescriptor(MEDIUM_DESCRIPTOR_HD);
            bs.SetSectorsPerTrack(DEFAULT_SECTORS_PER_TRACK);
            bs.SetNrHeads(DEFAULT_HEADS);
            bs.SetOemName(oemName);
        }

        /// <summary>
        /// Initializes the boot sector and file system for the device. The file
        /// system created by this method will always be in read-write mode.
        /// </summary>
        /// <returns>the file system that was created</returns>
        /// <exception cref="System.IO.IOException">IOException on write error</exception>
        public FatFileSystem Format() {
            var sectorSize = device.GetSectorSize();
            var totalSectors = (int)(device.GetSize() / sectorSize);

            FsInfoSector fsi;
            BootSector bs;
            if (sectorsPerCluster == 0) throw new Exception();

            if (fatType == FatType.BaseFat32) {
                bs = new Fat32BootSector(device);
                InitBootSector(bs);

                var f32Bs = (Fat32BootSector)bs;

                f32Bs.SetFsInfoSectorNr(1);

                f32Bs.SetSectorsPerFat(SectorsPerFat(0, totalSectors));
                var rnd = new Random();
                f32Bs.SetFileSystemId(rnd.Next());

                f32Bs.SetVolumeLabel(label);

                /* create FS info sector */
                fsi = FsInfoSector.Create(f32Bs);
            }
            else {
                bs = new Fat16BootSector(device);
                InitBootSector(bs);

                var f16Bs = (Fat16BootSector)bs;

                var rootDirEntries = RootDirectorySize(
                        device.GetSectorSize(), totalSectors);

                f16Bs.SetRootDirEntryCount(rootDirEntries);
                f16Bs.SetSectorsPerFat(SectorsPerFat(rootDirEntries, totalSectors));
                if (label != null) f16Bs.SetVolumeLabel(label);
                fsi = null;
            }

            //        bs.write();

            if (fatType == FatType.BaseFat32) {
                var f32Bs = (Fat32BootSector)bs;
                /* possibly writes the boot sector copy */
                f32Bs.WriteCopy(device);
            }

            var fat = Fat.Create(bs, 0);

            AbstractDirectory rootDirStore;
            if (fatType == FatType.BaseFat32) {
                rootDirStore = ClusterChainDirectory.CreateRoot(fat);
                fsi.SetFreeClusterCount(fat.GetFreeClusterCount());
                fsi.SetLastAllocatedCluster(fat.GetLastAllocatedCluster());
                fsi.Write();
            }
            else {
                rootDirStore = Fat16RootDirectory.Create((Fat16BootSector)bs);
            }

            var rootDir =
                    new FatLfnDirectory(rootDirStore, fat, false);

            rootDir.Flush();

            for (var i = 0; i < bs.GetNrFats(); i++) {
                fat.WriteCopy(FatUtils.GetFatOffset(bs, i));
            }

            bs.Write();

            var fs = FatFileSystem.Read(device, false);

            if (label != null) {
                fs.SetVolumeLabel(label);
            }

            fs.Flush();
            return fs;
        }

        private int SectorsPerFat(int rootDirEntries, int totalSectors) {
            var bps = device.GetSectorSize();
            var rootDirSectors =
                    ((rootDirEntries * 32) + (bps - 1)) / bps;
            long tmp1 =
                    totalSectors - (this.reservedSectors + rootDirSectors);
            var tmp2 = (256 * this.sectorsPerCluster) + this.fatCount;

            if (fatType == FatType.BaseFat32)
                tmp2 /= 2;

            var result = (int)((tmp1 + (tmp2 - 1)) / tmp2);

            return result;
        }

        /// <summary>
        /// Determines a usable FAT type from the device by looking at the
        /// <see cref="IBlockDevice.GetSize"/> device size only.
        /// </summary>
        /// <returns>the suggested FAT type</returns>
        /// <exception cref="System.IO.IOException">IOException on error determining the device's size</exception>
        private FatType FatTypeFromDevice() {
            return FatTypeFromSize(device.GetSize());
        }

        /// <summary>
        /// Determines a usable FAT type from the device by looking at the
        /// <see cref="IBlockDevice.GetSize"/> device size only.
        /// </summary>
        /// <param name="sizeInBytes"></param>
        /// <returns>the suggested FAT type</returns>
        /// <exception cref="System.IO.IOException">IOException on error determining the device's size</exception>
        public static FatType FatTypeFromSize(long sizeInBytes) {
            var sizeInMb = sizeInBytes / (1024 * 1024);
            if (sizeInMb < 4) return FatType.BaseFat12;
            if (sizeInMb < 512) return FatType.BaseFat16;
            return FatType.BaseFat32;
        }

        public static int ClusterSizeFromSize(long sizeInBytes, int sectorSize) {
            var ft = FatTypeFromSize(sizeInBytes);
            if (ft == FatType.BaseFat12)
                return SectorsPerCluster12(sizeInBytes, sectorSize);
            if (ft == FatType.BaseFat16)
                return SectorsPerCluster16FromSize(sizeInBytes, sectorSize);
            if (ft == FatType.BaseFat32)
                return SectorsPerCluster32FromSize(sizeInBytes, sectorSize);
            throw new Exception();
        }

        /// <summary>
        /// Returns the exact type of FAT the will be created by this formatter.
        /// </summary>
        /// <returns>the FAT type</returns>
        public FatType GetFatType() {
            return fatType;
        }

        /// <summary>
        /// Sets the type of FAT that will be created by this
        /// <see cref="SuperFloppyFormatter"/>.
        /// </summary>
        /// <param name="fatType">the desired <see cref="FatType"/></param>
        /// <returns>this <see cref="SuperFloppyFormatter"/></returns>
        /// <exception cref="System.IO.IOException">IOException on error setting the fatType</exception>
        /// <exception cref="InvalidOperationException">InvalidOperationException if fatType does not support the
        ///     size of the device</exception>
        public SuperFloppyFormatter SetFatType(FatType fatType) {

            if (fatType == null) throw new NullReferenceException();

            if (fatType == FatType.BaseFat12 || fatType == FatType.BaseFat16)
                reservedSectors = 1;
            else if (fatType == FatType.BaseFat32)
                reservedSectors = 32;
            sectorsPerCluster = DefaultSectorsPerCluster(fatType);
            this.fatType = fatType;

            return this;
        }

        private static int RootDirectorySize(int bps, int nbTotalSectors) {
            var totalSize = bps * nbTotalSectors;
            if (totalSize >= MAX_DIRECTORY * 5 * 32) {
                return MAX_DIRECTORY;
            }

            return totalSize / (5 * 32);
        }

        static private int _maxFat32Clusters = 0x0FFFFFF5;

        static private int SectorsPerCluster32FromSize(long size, int sectorSize) {
            var sectors = size / sectorSize;

            if (sectors <= 66600) throw new ArgumentException(
                    "disk too small for FAT32");

            return
                    sectors > 67108864 ? 64 :
                    sectors > 33554432 ? 32 :
                    sectors > 16777216 ? 16 :
                    sectors > 532480 ? 8 : 1;
        }

        private int SectorsPerCluster32() {
            if (reservedSectors != 32) throw new InvalidOperationException(
                    "number of reserved sectors must be 32");

            if (fatCount != 2) throw new InvalidOperationException(
                    "number of FATs must be 2");

            var sectors = device.GetSize() / device.GetSectorSize();

            if (sectors <= 66600) throw new ArgumentException(
                    "disk too small for FAT32");

            return SectorsPerCluster32FromSize(device.GetSize(), device.GetSectorSize());
        }

        static private int _maxFat16Clusters = 65524;

        static private int SectorsPerCluster16FromSize(long size, int sectorSize) {
            var sectors = size / sectorSize;

            if (sectors <= 8400) throw new ArgumentException(
                    "disk too small for FAT16");

            if (sectors > 4194304) throw new ArgumentException(
                    "disk too large for FAT16");

            return
            sectors > 2097152 ? 64 :
            sectors > 1048576 ? 32 :
            sectors > 524288 ? 16 :
            sectors > 262144 ? 8 :
            sectors > 32680 ? 4 : 2;
        }

        private int SectorsPerCluster16() {
            if (reservedSectors != 1) throw new InvalidOperationException(
                    "number of reserved sectors must be 1");

            if (fatCount != 2) throw new InvalidOperationException(
                    "number of FATs must be 2");

            var size = device.GetSize();
            var sectorSize = device.GetSectorSize();
            return SectorsPerCluster16FromSize(size, sectorSize);
        }

        private int DefaultSectorsPerCluster(FatType fatType) {
            var size = device.GetSize();
            var sectorSize = device.GetSectorSize();

            if (fatType == FatType.BaseFat12)
                return SectorsPerCluster12(size, sectorSize);
            if (fatType == FatType.BaseFat16)
                return SectorsPerCluster16();
            if (fatType == FatType.BaseFat32)
                return SectorsPerCluster32();
            throw new Exception();
        }

        static private int SectorsPerCluster12(long size, int sectorSize) {
            var result = 1;

            var sectors = size / sectorSize;

            while (sectors / result > Fat16BootSector.MAX_FAT12_CLUSTERS) {
                result *= 2;
                if (result * size > 4096) throw new
                        ArgumentException("disk too large for FAT12");
            }

            return result;
        }

    }

}