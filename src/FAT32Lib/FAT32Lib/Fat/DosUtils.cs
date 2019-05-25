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

namespace FAT32Lib.Fat {

    /// <summary>
    /// This class contains some methods for date and time conversions between
    /// UNIX epoch milliseconds
    /// and the format known from DOS filesystems(e.g.fat)
    /// </summary>
    public static class DosUtils {

        /// <summary>
        /// Decode a 16-bit encoded DOS date/time into a UNIX epoch millisecond.
        /// </summary>
        /// <param name="dosDate"></param>
        /// <param name="dosTime"></param>
        /// <returns></returns>
        public static long DecodeDateTime(int dosDate, int dosTime) {
            DateTime dt = new DateTime(1980, 1, 1);

            dt.AddMilliseconds(0);
            dt.AddSeconds((dosTime & 0x1f) * 2);
            dt.AddMinutes((dosTime >> 5) & 0x3f);
            dt.AddHours(dosTime >> 11);

            dt.AddDays(dosDate & 0x1f);
            dt.AddMonths(((dosDate >> 5) & 0x0f)-1);
            dt.AddYears(dosDate >> 9);

            return (int)(dt - new DateTime(1970, 1, 1)).TotalMilliseconds;
        }

        /// <summary>
        /// Encode a UNIX epoch millisecond into a 16-bit encoded DOS time
        /// </summary>
        /// <param name="javaDateTime"></param>
        /// <returns></returns>
        public static int EncodeTime(long javaDateTime) {
            DateTime dt = new DateTime(1970, 1, 1);
            dt.AddMilliseconds(javaDateTime);
            return 2048 * dt.Hour + 32 * dt.Minute + dt.Second / 2;
        }

        /// <summary>
        /// Encode a UNIX epoch millisecond into a 16-bit encoded DOS date
        /// </summary>
        /// <param name="javaDateTime"></param>
        /// <returns></returns>
        public static int EncodeDate(long javaDateTime) {
            DateTime dt = new DateTime(1980, 1, 1);
            dt.AddMilliseconds(javaDateTime);
            return 512 * (dt.Year - 1980) + 32 * dt.Month +
                    dt.Day;
        }
    }

}