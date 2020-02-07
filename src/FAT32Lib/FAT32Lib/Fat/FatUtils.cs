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

namespace FAT32Lib.Fat {

    public static class FatUtils {

        /// <summary>
        /// Gets the offset (in bytes) of the fat with the given index
        /// </summary>
        /// <param name="bs"></param>
        /// <param name="fatNr">(0..)</param>
        /// <returns>long</returns>
        /// <exception cref="System.IO.IOException"></exception>
        public static long GetFatOffset(BootSector bs, int fatNr) {
            long sectSize = bs.GetBytesPerSector();
            var sectsPerFat = bs.GetSectorsPerFat();
            long resSects = bs.GetNrReservedSectors();

            var offset = resSects * sectSize;
            var fatSize = sectsPerFat * sectSize;

            offset += fatNr * fatSize;

            return offset;
        }

        /// <summary>
        /// Gets the offset (in bytes) of the root directory with the given index
        /// </summary>
        /// <param name="bs"></param>
        /// <returns>long</returns>
        /// <exception cref="System.IO.IOException"></exception>
        public static long GetRootDirOffset(BootSector bs) {
            long sectSize = bs.GetBytesPerSector();
            var sectsPerFat = bs.GetSectorsPerFat();
            var fats = bs.GetNrFats();

            var offset = GetFatOffset(bs, 0);

            offset += fats * sectsPerFat * sectSize;

            return offset;
        }

        /// <summary>
        /// Gets the offset of the data (file) area
        /// </summary>
        /// <param name="bs"></param>
        /// <returns>long</returns>
        /// <exception cref="System.IO.IOException"></exception>
        public static long GetFilesOffset(BootSector bs) {
            var offset = GetRootDirOffset(bs);

            offset += bs.GetRootDirEntryCount() * 32;

            return offset;
        }

    }

}