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
    /// Little endian (LSB first) conversion methods.
    /// </summary>
    public static class LittleEndian {

        /// <summary>
        /// Gets an 8-bit unsigned integer from the given byte array at
        /// the given offset.
        /// </summary>
        /// <param name="src">the byte offset where to read the value from</param>
        /// <param name="offset">the byte array to extract the value from</param>
        /// <returns>the integer that was read</returns>
        public static int GetUInt8(byte[] src, int offset) {
            return src[offset] & 0xFF;
        }

        /// <summary>
        /// Gets a 16-bit unsigned integer from the given byte array at the given offset.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static int GetUInt16(byte[] src, int offset) {
            int v0 = src[offset + 0] & 0xFF;
            int v1 = src[offset + 1] & 0xFF;
            return ((v1 << 8) | v0);
        }

        /// <summary>
        /// Gets a 32-bit unsigned integer from the given byte array at the given offset.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="offset"></param>
        /// <returns></returns>
        public static long GetUInt32(byte[] src, int offset) {
            long v0 = src[offset + 0] & 0xFF;
            long v1 = src[offset + 1] & 0xFF;
            long v2 = src[offset + 2] & 0xFF;
            long v3 = src[offset + 3] & 0xFF;
            return ((v3 << 24) | (v2 << 16) | (v1 << 8) | v0);
        }

        /// <summary>
        /// Sets an 8-bit integer in the given byte array at the given offset.
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="offset"></param>
        /// <param name="value"></param>
        public static void SetInt8(byte[] dst, int offset, int value) {
            dst[offset] = (byte)value;
        }

        /// <summary>
        /// Sets a 16-bit integer in the given byte array at the given offset.
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="offset"></param>
        /// <param name="value"></param>
        public static void SetInt16(byte[] dst, int offset, int value) {
            dst[offset + 0] = (byte)(value & 0xFF);
            dst[offset + 1] = (byte)((value >> 8) & 0xFF);
        }

        /// <summary>
        /// Sets a 32-bit integer in the given byte array at the given offset.
        /// </summary>
        /// <param name="dst"></param>
        /// <param name="offset"></param>
        /// <param name="value"></param>
        public static void SetInt32(byte[] dst, int offset, long value) {

            if (value > int.MaxValue) {
                throw new ArgumentException(
                        value + " can not be represented in a 32bit dword");
            }

            dst[offset + 0] = (byte)(value & 0xFF);
            dst[offset + 1] = (byte)((value >> 8) & 0xFF);
            dst[offset + 2] = (byte)((value >> 16) & 0xFF);
            dst[offset + 3] = (byte)((value >> 24) & 0xFF);
        }

    }

}