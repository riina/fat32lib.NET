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

    public class Sector {
        private readonly IBlockDevice device;
        private readonly long offset;

        /// <summary>
        /// The buffer holding the contents of this <see cref="Sector"/>.
        /// </summary>
        protected readonly MemoryStream Buffer;

        private bool dirty;

        protected Sector(IBlockDevice device, long offset, int size) {
            this.offset = offset;
            this.device = device;
            var buf = new byte[size];
            Buffer = new MemoryStream(buf);
            dirty = true;
        }

        /// <summary>
        /// Reads the contents of this <see cref="Sector"/> from the device into 
        /// internal buffer and resets the "dirty" state.
        /// </summary>
        /// <exception cref="IOException">IOException on read error</exception>
        /// <seealso cref="IsDirty"/>
        protected void Read() {
            Buffer.Position = 0;
            device.Read(offset, Buffer);
            dirty = false;
        }

        public bool IsDirty() {
            return dirty;
        }

        protected void MarkDirty() {
            dirty = true;
        }

        /// <summary>
        /// Returns the <see cref="IBlockDevice"/> where this <see cref="Sector"/> is stored.
        /// </summary>
        /// <returns>this <see cref="Sector"/>'s device</returns>
        public IBlockDevice GetDevice() {
            return device;
        }

        public void Write() {
            if (!IsDirty()) return;

            Buffer.Position = 0;
            device.Write(offset, Buffer);
            dirty = false;
        }

        protected int Get16(int offset) {
            var b = new byte[2];
            Buffer.Position = offset;
            Buffer.Read(b, 0, 2);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(b, 0, 2);
            return BitConverter.ToUInt16(b, 0);
        }

        protected long Get32(int offset) {
            var b = new byte[4];
            Buffer.Position = offset;
            Buffer.Read(b, 0, 4);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(b, 0, 4);
            return BitConverter.ToUInt32(b, 0);
        }

        protected int Get8(int offset) {
            Buffer.Position = offset;
            return Buffer.ReadByte();
        }

        protected void Set16(int offset, int value) {
            var b = BitConverter.GetBytes((short)value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(b, 0, 2);
            Buffer.Position = offset;
            Buffer.Write(b, 0, 2);
            dirty = true;
        }

        protected void Set32(int offset, long value) {
            var b = BitConverter.GetBytes((int)value);
            if (!BitConverter.IsLittleEndian)
                Array.Reverse(b, 0, 4);
            Buffer.Position = offset;
            Buffer.Write(b, 0, 4);
            dirty = true;
        }

        protected void Set8(int offset, int value) {
            if ((value & 0xff) != value) {
                throw new ArgumentException(
                        value + " too big to be stored in a single octet");
            }
            Buffer.Position = offset;
            Buffer.WriteByte((byte)value);
            dirty = true;
        }

        /// <summary>
        /// Returns the device offset to this <see cref="Sector"/>.
        /// </summary>
        /// <returns>the <see cref="Sector"/>'s device offset</returns>
        protected long GetOffset() {
            return offset;
        }
    }

}