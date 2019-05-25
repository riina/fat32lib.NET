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
using System.Text;

namespace FAT32Lib.Fat {

    public sealed class FatDirectoryEntry : AbstractFsObject {

        /**
         * The size in bytes of an FAT directory entry.
         */
        public const int SIZE = 32;

        /**
         * The offset to the attributes byte.
         */
        private const int OFFSET_ATTRIBUTES = 0x0b;

        /**
         * The offset to the file size dword.
         */
        private const int OFFSET_FILE_SIZE = 0x1c;

        private const int F_READONLY = 0x01;
        private const int F_HIDDEN = 0x02;
        private const int F_SYSTEM = 0x04;
        private const int F_VOLUME_ID = 0x08;
        private const int F_DIRECTORY = 0x10;
        private const int F_ARCHIVE = 0x20;

        private const int MAX_CLUSTER = 0xFFFF;

        /**
         * The magic byte denoting that this entry was deleted and is free
         * for reuse.
         *
         * @see #isDeleted() 
         */
        public const int ENTRY_DELETED_MAGIC = 0xe5;

        private readonly byte[] data;
        private bool dirty;
        internal bool HasShortNameOnly { get; private set; }

        internal FatDirectoryEntry(byte[] data, bool readOnly) : base(readOnly) {
            this.data = data;
        }

        private FatDirectoryEntry() : this(new byte[SIZE], false) { }

        /**
         * Reads a {@code FatDirectoryEntry} from the specified {@code ByteBuffer}.
         * The buffer must have at least {@link #SIZE} bytes remaining. The entry
         * is read from the buffer's current position, and if this method returns
         * non-null the position will have advanced by {@link #SIZE} bytes,
         * otherwise the position will remain unchanged.
         *
         * @param buff the buffer to read the entry from
         * @param readOnly if the resulting {@code FatDirecoryEntry} should be
         *      read-only
         * @return the directory entry that was read from the buffer or {@code null}
         *      if there was no entry to read from the specified position (first
         *      byte was 0)
         */
        public static FatDirectoryEntry Read(MemoryStream buff, bool readOnly) {
            /* peek into the buffer to see if we're done with reading */
            long cPos = buff.Position;
            int v = buff.ReadByte();
            buff.Position = cPos;
            if (v == 0) return null;

            /* read the directory entry */

            byte[] data = new byte[SIZE];
            buff.Read(data, 0, data.Length);
            return new FatDirectoryEntry(data, readOnly);
        }

        public static void WriteNullEntry(MemoryStream buff) {
            for (int i = 0; i < SIZE; i++) {
                buff.WriteByte(0);
            }
        }

        /**
         * Decides if this entry is a "volume label" entry according to the FAT
         * specification.
         *
         * @return if this is a volume label entry
         */
        public bool IsVolumeLabel() {
            if (IsLfnEntry()) return false;
            else return ((GetFlags() & (F_DIRECTORY | F_VOLUME_ID)) == F_VOLUME_ID);
        }

        private void SetFlag(int mask, bool set) {
            int oldFlags = GetFlags();

            if (((oldFlags & mask) != 0) == set) return;

            if (set) {
                SetFlags(oldFlags | mask);
            }
            else {
                SetFlags(oldFlags & ~mask);
            }

            this.dirty = true;
        }

        public bool IsSystemFlag() {
            return ((GetFlags() & F_SYSTEM) != 0);
        }

        public void SetSystemFlag(bool isSystem) {
            SetFlag(F_SYSTEM, isSystem);
        }

        public bool IsArchiveFlag() {
            return ((GetFlags() & F_ARCHIVE) != 0);
        }

        public void SetArchiveFlag(bool isArchive) {
            SetFlag(F_ARCHIVE, isArchive);
        }

        public bool IsHiddenFlag() {
            return ((GetFlags() & F_HIDDEN) != 0);
        }

        public void SetHiddenFlag(bool isHidden) {
            SetFlag(F_HIDDEN, isHidden);
        }

        public bool IsVolumeIdFlag() {
            return ((GetFlags() & F_VOLUME_ID) != 0);
        }

        public bool IsLfnEntry() {
            return IsReadonlyFlag() && IsSystemFlag() &&
                    IsHiddenFlag() && IsVolumeIdFlag();
        }

        public bool IsDirty() {
            return dirty;
        }

        private int GetFlags() {
            return data[OFFSET_ATTRIBUTES];
        }

        private void SetFlags(int flags) {
            data[OFFSET_ATTRIBUTES] = (byte)flags;
        }

        public bool IsDirectory() {
            return ((GetFlags() & (F_DIRECTORY | F_VOLUME_ID)) == F_DIRECTORY);
        }

        public static FatDirectoryEntry Create(bool directory) {
            FatDirectoryEntry result = new FatDirectoryEntry();

            if (directory) {
                result.SetFlags(F_DIRECTORY);
            }

            /* initialize date and time fields */

            long now = (long)(DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc)).TotalMilliseconds;
            result.SetCreated(now);
            result.SetLastAccessed(now);
            result.SetLastModified(now);

            return result;
        }

        public static FatDirectoryEntry CreateVolumeLabel(string volumeLabel) {
            byte[] data = new byte[SIZE];

            Array.Copy(
                        Encoding.ASCII.GetBytes(volumeLabel), 0,
                        data, 0,
                        volumeLabel.Length);

            FatDirectoryEntry result = new FatDirectoryEntry(data, false);
            result.SetFlags(F_VOLUME_ID);
            return result;
        }

        public string GetVolumeLabel() {
            if (!IsVolumeLabel())
                throw new NotSupportedException("not a volume label");

            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < AbstractDirectory.MAX_LABEL_LENGTH; i++) {
                byte b = data[i];

                if (b != 0) {
                    sb.Append((char)b);
                }
                else {
                    break;
                }
            }

            return sb.ToString();
        }

        public long GetCreated() {
            return DosUtils.DecodeDateTime(LittleEndian.GetUInt16(data, 0x10), LittleEndian.GetUInt16(data, 0x0E));
        }

        public void SetCreated(long created) {
            LittleEndian.SetInt16(data, 0x0e,
                DosUtils.EncodeTime(created));
            LittleEndian.SetInt16(data, 0x10,
                    DosUtils.EncodeDate(created));

            dirty = true;
        }

        public long GetLastModified() {
            return DosUtils.DecodeDateTime(
                LittleEndian.GetUInt16(data, 0x18),
                LittleEndian.GetUInt16(data, 0x16));
        }

        public void SetLastModified(long lastModified) {
            LittleEndian.SetInt16(data, 0x16,
               DosUtils.EncodeTime(lastModified));
            LittleEndian.SetInt16(data, 0x18,
                    DosUtils.EncodeDate(lastModified));

            dirty = true;
        }

        public long GetLastAccessed() {
            return DosUtils.DecodeDateTime(
                LittleEndian.GetUInt16(data, 0x12),
                0); /* time is not recorded */
        }

        public void SetLastAccessed(long lastAccessed) {
            LittleEndian.SetInt16(data, 0x12,
                DosUtils.EncodeDate(lastAccessed));

            dirty = true;
        }

        /**
         * Returns if this entry has been marked as deleted. A deleted entry has
         * its first byte set to the magic {@link #ENTRY_DELETED_MAGIC} value.
         * 
         * @return if this entry is marked as deleted
         */
        public bool IsDeleted() {
            return data[0] == ENTRY_DELETED_MAGIC;
        }

        /**
         * Returns the size of this entry as stored at {@link #OFFSET_FILE_SIZE}.
         * 
         * @return the size of the file represented by this entry
         */
        public long GetLength() {
            return LittleEndian.GetUInt32(data, OFFSET_FILE_SIZE);
        }

        /**
         * Sets the size of this entry stored at {@link #OFFSET_FILE_SIZE}.
         * 
         * @param length the new size of the file represented by this entry
         * @throws IllegalArgumentException if {@code length} is out of range
         */
        public void SetLength(long length) {
            LittleEndian.SetInt32(data, OFFSET_FILE_SIZE, length);
        }

        /**
         * Returns the {@code ShortName} that is stored in this directory entry or
         * {@code null} if this entry has not been initialized.
         * 
         * @return the {@code ShortName} stored in this entry or {@code null}
         */
        public ShortName GetShortName() {
            if (data[0] == 0) {
                return null;
            }
            else {
                return ShortName.Parse(data);
            }
        }

        /**
         * Does this entry refer to a file?
         *
         * @return
         * @see org.jnode.fs.FSDirectoryEntry#isFile()
         */
        public bool IsFile() {
            return ((GetFlags() & (F_DIRECTORY | F_VOLUME_ID)) == 0);
        }

        public void SetShortName(ShortName sn) {
            if (sn.Equals(GetShortName())) return;

            sn.Write(data);
            HasShortNameOnly = sn.HasShortNameOnly();
            dirty = true;
        }

        /**
         * Returns the startCluster.
         * 
         * @return int
         */
        public long GetStartCluster() {
            long lowBytes = LittleEndian.GetUInt16(data, 0x1a);
            long highBytes = LittleEndian.GetUInt16(data, 0x14);
            return (highBytes << 16 | lowBytes);
        }

        /**
         * Sets the startCluster.
         *
         * @param startCluster The startCluster to set
         */
        internal void SetStartCluster(long startCluster) {
            if (startCluster > int.MaxValue) throw new Exception();

            LittleEndian.SetInt16(data, 0x1a, (int)startCluster);
            LittleEndian.SetInt16(data, 0x14, (int)(startCluster >> 16));
        }

        /**
         * Writes this directory entry into the specified buffer.
         *
         * @param buff the buffer to write this entry to
         */
        internal void Write(MemoryStream buff) {
            buff.Write(data, 0, data.Length);
            dirty = false;
        }

        /**
         * Returns if the read-only flag is set for this entry. Do not confuse
         * this with {@link #isReadOnly()}.
         *
         * @return if the read only file system flag is set on this entry
         * @see #F_READONLY
         * @see #setReadonlyFlag(boolean) 
         */
        public bool IsReadonlyFlag() {
            return ((GetFlags() & F_READONLY) != 0);
        }

        /**
         * Updates the read-only file system flag for this entry.
         *
         * @param isReadonly the new value for the read-only flag
         * @see #F_READONLY
         * @see #isReadonlyFlag() 
         */
        public void SetReadonlyFlag(bool isReadonly) {
            SetFlag(F_READONLY, isReadonly);
        }

        internal string GetLfnPart() {
            char[] unicodechar = new char[13];

            unicodechar[0] = (char)LittleEndian.GetUInt16(data, 1);
            unicodechar[1] = (char)LittleEndian.GetUInt16(data, 3);
            unicodechar[2] = (char)LittleEndian.GetUInt16(data, 5);
            unicodechar[3] = (char)LittleEndian.GetUInt16(data, 7);
            unicodechar[4] = (char)LittleEndian.GetUInt16(data, 9);
            unicodechar[5] = (char)LittleEndian.GetUInt16(data, 14);
            unicodechar[6] = (char)LittleEndian.GetUInt16(data, 16);
            unicodechar[7] = (char)LittleEndian.GetUInt16(data, 18);
            unicodechar[8] = (char)LittleEndian.GetUInt16(data, 20);
            unicodechar[9] = (char)LittleEndian.GetUInt16(data, 22);
            unicodechar[10] = (char)LittleEndian.GetUInt16(data, 24);
            unicodechar[11] = (char)LittleEndian.GetUInt16(data, 28);
            unicodechar[12] = (char)LittleEndian.GetUInt16(data, 30);

            int end = 0;

            while ((end < 13) && (unicodechar[end] != '\0')) {
                end++;
            }

            return new string(unicodechar).Substring(0, end);
        }

    }

}