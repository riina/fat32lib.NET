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

namespace FAT32Lib.Util {

    /// <summary>
    /// This is an <see cref="IBlockDevice"/> that uses a <see cref="FileStream"/> as it's backing store.
    /// </summary>
    public sealed class FileDisk : IBlockDevice {

        /// <summary>
        /// The number of bytes per sector for all <see cref="FileDisk"/> instances.
        /// </summary>
        public const int BYTES_PER_SECTOR = 512;

        private readonly FileStream fileStream;
        private readonly bool readOnly;
        private bool closed;

        /// <summary>
        /// Creates a new instance of <see cref="FileDisk"/> for the specified
        /// <see cref="FileStream"/>.
        /// </summary>
        /// <param name="file">the file that holds the disk contents</param>
        /// <param name="readOnly">if the file should be opened in read-only mode, which
        ///     will result in a read-only <see cref="FileDisk"/> instance</param>
        /// <exception cref="FileNotFoundException">FileNotFoundException if the specified file does not exist</exception>
        /// <seealso cref="IsReadOnly"/>
        public FileDisk(string file, bool readOnly) {
            if (!File.Exists(file)) throw new FileNotFoundException();

            this.readOnly = readOnly;
            closed = false;
            fileStream = new FileStream(file, FileMode.Open, readOnly ? FileAccess.Read : FileAccess.ReadWrite);
        }

        private FileDisk(FileStream fileStream, bool readOnly) {
            closed = false;
            this.fileStream = fileStream;
            this.readOnly = readOnly;
        }

        /// <summary>
        /// Creates a new <see cref="FileDisk"/> of the specified size. The
        /// <see cref="FileDisk"/> returned by this method will be writable.
        /// </summary>
        /// <param name="file">the file to hold the <see cref="FileDisk"/> contents</param>
        /// <param name="size">the size of the new <see cref="FileDisk"/></param>
        /// <returns>the created <see cref="FileDisk"/> instance</returns>
        /// <exception cref="IOException">IOException on error creating the <see cref="FileDisk"/></exception>
        public static FileDisk Create(string file, long size) {
            var fileStream =
                    new FileStream(file, FileMode.Create, FileAccess.ReadWrite);
            fileStream.SetLength(size);

            return new FileDisk(fileStream, false);
        }

        public long GetSize() {
            CheckClosed();

            return fileStream.Length;
        }

        public void Read(long devOffset, MemoryStream dest) {
            CheckClosed();

            var toRead = (int)(dest.Length - dest.Position);
            if ((devOffset + toRead) > GetSize()) throw new IOException(
                    "reading past end of device");

            // Kulikova: Conversion to explicit buffering
            var buf = new byte[4096];
            while (toRead > 0) {
                fileStream.Position = devOffset;
                var read = fileStream.Read(buf, 0, Math.Min(toRead, buf.Length));
                dest.Write(buf, 0, read);
                toRead -= read;
                devOffset += read;
            }
        }

        public void Write(long devOffset, MemoryStream src) {
            CheckClosed();

            if (this.readOnly) throw new ReadOnlyException();

            var toWrite = (int)(src.Length - src.Position);

            // Kulikova: Conversion to explicit buffering
            var buf = new byte[4096];
            while (toWrite > 0) {
                fileStream.Position = devOffset;
                var written = src.Read(buf, 0, Math.Min(toWrite, buf.Length));
                fileStream.Write(buf, 0, written);
                toWrite -= written;
                devOffset += written;
            }
        }

        public void Flush() {
            CheckClosed();
        }

        public int GetSectorSize() {
            CheckClosed();

            return BYTES_PER_SECTOR;
        }

        public void Close() {
            if (IsClosed()) return;

            closed = true;
            fileStream.Close();
        }

        public bool IsClosed() {
            return closed;
        }

        private void CheckClosed() {
            if (closed) throw new InvalidOperationException("device already closed");
        }

        public bool IsReadOnly() {
            CheckClosed();

            return readOnly;
        }

    }

}