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
using System.IO.Compression;
using System.Text;

namespace FAT32Lib.Util {

    /// <summary>
    /// A <see cref="IBlockDevice"/> that lives entirely in heap memory. This is basically
    /// a RAM disk. A <see cref="RamDisk"/> is always writable.
    /// </summary>
    public sealed class RamDisk : IBlockDevice {

        /// <summary>
        /// The default sector size for <see cref="RamDisk"/>s.
        /// </summary>
        public const int DEFAULT_SECTOR_SIZE = 512;

        private readonly int sectorSize;
        private readonly MemoryStream data;
        private readonly int size;
        private bool closed;

        /// <summary>
        /// Reads a GZIP compressed disk image from the specified input stream and
        /// returns a <see cref="RamDisk"/> holding the decompressed image.
        /// </summary>
        /// <param name="inStream">the stream to read the disk image from</param>
        /// <returns>the decompressed <see cref="RamDisk"/></returns>
        /// <exception cref="IOException">IOException on read or decompression error</exception>
        public static RamDisk ReadGzipped(Stream inStream) {
            var zis = new GZipStream(inStream, CompressionMode.Decompress);
            var decompressedStream = new MemoryStream();

            var buffer = new byte[4096];

            var read = zis.Read(buffer, 0, buffer.Length);
            var total = 0;

            while (read >= 0) {
                total += read;
                decompressedStream.Write(buffer, 0, read);
                read = zis.Read(buffer, 0, buffer.Length);
            }

            if (total < DEFAULT_SECTOR_SIZE) throw new IOException(
                    "read only " + total + " bytes");

            var buf = new byte[total];
            var targetStream = new MemoryStream(buf);
            decompressedStream.Position = 0;
            decompressedStream.CopyTo(targetStream);
            return new RamDisk(targetStream, DEFAULT_SECTOR_SIZE);
        }

        private RamDisk(MemoryStream buffer, int sectorSize) {
            size = (int)buffer.Length;
            this.sectorSize = sectorSize;
            data = buffer;
            closed = false;
        }

        /// <summary>
        /// Creates a new instance of <see cref="RamDisk"/> of this specified
        /// size and using the <see cref="DEFAULT_SECTOR_SIZE"/>.
        /// </summary>
        /// <param name="size">the size of the new block device</param>
        public RamDisk(int size) : this(size, DEFAULT_SECTOR_SIZE) {
        }

        /// <summary>
        /// Creates a new instance of <see cref="RamDisk"/> of this specified
        /// size and sector size
        /// </summary>
        /// <param name="size">the size of the new block device</param>
        /// <param name="sectorSize">the sector size of the new block device</param>
        public RamDisk(int size, int sectorSize) {
            if (sectorSize < 1) throw new ArgumentException(
                    "invalid sector size");

            this.sectorSize = sectorSize;
            this.size = size;
            var buf = new byte[size];
            data = new MemoryStream(buf);
        }

        public long GetSize() {
            CheckClosed();
            return this.size;
        }

        public void Read(long devOffset, MemoryStream dest) {
            CheckClosed();


            if (devOffset > GetSize()) {
                var sb = new StringBuilder();
                sb.Append("read at ").Append(devOffset);
                sb.Append(" is off size (").Append(GetSize()).Append(")");

                throw new ArgumentException(sb.ToString());
            }

            data.SetLength(devOffset + (dest.Length - dest.Position));
            data.Position = devOffset;

            data.CopyTo(dest);
        }

        public void Write(long devOffset, MemoryStream src) {
            CheckClosed();

            if (devOffset + (src.Length - src.Position) > GetSize()) throw new
                    ArgumentException(
                    "offset=" + devOffset +
                    ", length=" + (src.Length - src.Position) +
                    ", size=" + GetSize());

            data.SetLength(devOffset + (src.Length - src.Position));
            data.Position = devOffset;

            src.CopyTo(data);
        }

        /// <summary>
        /// Returns a slice of the <see cref="MemoryStream"/> that is used by this
        /// <see cref="RamDisk"/> as it's backing store. The returned buffer will be
        /// live(reflecting any changes made through the
        /// <see cref="Write(long, MemoryStream)"/> method, but read-only.
        /// </summary>
        /// <returns>a buffer holding the contents of this <see cref="RamDisk"/></returns>
        public MemoryStream GetBuffer() {
            // Kulikova: current guarantee that memorystream was made with backing array
            return new MemoryStream(data.GetBuffer(), false);
        }

        public void Flush() {
            CheckClosed();
        }

        public int GetSectorSize() {
            CheckClosed();
            return this.sectorSize;
        }

        public void Close() {
            this.closed = true;
        }

        public bool IsClosed() {
            return this.closed;
        }

        private void CheckClosed() {
            if (closed) throw new InvalidOperationException("device already closed");
        }

        /// <summary>
        /// Returns always false, as a <see cref="RamDisk"/> is always writable.
        /// </summary>
        /// <returns>always false</returns>
        public bool IsReadOnly() {
            CheckClosed();

            return false;
        }

    }

}