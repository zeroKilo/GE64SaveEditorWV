using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GE64SaveEditor
{
    public class GE64SaveGame
    {
        public bool isValid = false;
        public byte[] raw;
        public string myPath;
        public GE64SaveGame(string path)
        {
            myPath = path;
            raw = File.ReadAllBytes(path);
            uint m1 = BitConverter.ToUInt32(raw, 0);
            uint m2 = BitConverter.ToUInt32(raw, 0);
            if (m1 == 0x33382489 && m2 == 0x33382489 && raw.Length == 0x200)
            {
                MemoryStream m = new MemoryStream();
                m.Write(raw, 0, 0x20);
                if (!CheckHash(m.ToArray())) return;
                for (int i = 0; i < 5; i++)
                    if (!CheckHash(getSlotDataRaw(i))) return;
                isValid = true;
            }
        }

        public void Save()
        {
            if (!isValid) return;
            File.WriteAllBytes(myPath, raw);
        }


        public byte[] getSlotDataRaw(int slot)
        {
            MemoryStream m = new MemoryStream();
            m.Write(raw, 0x20 + slot * 0x60, 0x60);
            return m.ToArray();
        }

        public List<ushort> getSlotTimes(int slot, int diff)
        {
            byte[] data = getSlotDataRaw(slot);
            List<ushort> result = new List<ushort>();
            for (int i = 0; i < 20; i++)
            {
                Level l = (Level)i;
                switch (diff)
                {
                    case 0:
                        result.Add(ReadBits(data, 0x90 + i * 10, 0xA));
                        break;
                    case 1:
                        result.Add(ReadBits(data, 0x158 + i * 10, 0xA));
                        break;
                    case 2:
                        result.Add(ReadBits(data, 0x220 + i * 10, 0xA));
                        break;
                }
            }
            return result;
        }

        public string getTimeString(ushort time, int level)
        {
            Level l = (Level)level;
            return l.ToString().PadRight(10) + " : " + toTime(time);
        }

        public List<uint> getOther(int slot)
        {
            MemoryStream m = new MemoryStream(getSlotDataRaw(slot));
            m.Seek(8, 0);
            List<uint> result = new List<uint>();
            result.Add(ReadU16(m));
            result.Add(ReadU16(m));
            result.Add(ReadU16(m));
            result.Add(ReadU32(m));
            return result;
        }

        public void makeSlot(int slot, List<ushort> tA, List<ushort> tSA, List<ushort> t00A, List<uint> other)
        {
            MemoryStream m = new MemoryStream();
            WriteU16((ushort)other[0], m);
            WriteU16((ushort)other[1], m);
            WriteU16((ushort)other[2], m);
            WriteU32(other[3], m);
            m.Write(new byte[0x4E], 0, 0x4E);
            byte[] buff = m.ToArray();
            for (int i = 0; i < 20; i++)
            {
                WriteBits(buff, 0x50 + i * 10, 0xA, tA[i]);
                WriteBits(buff, 0x118 + i * 10, 0xA, tSA[i]);
                WriteBits(buff, 0x1E0 + i * 10, 0xA, t00A[i]);
            }
            UInt64 hash = Hash(buff);
            m = new MemoryStream();
            WriteU64(hash, m);
            m.Write(buff, 0, 0x58);
            buff = m.ToArray();
            int offset = 0x20 + slot * 0x60;
            for (int i = 0; i < 0x60; i++)
                raw[offset + i] = buff[i];
        }


        public bool CheckHash(byte[] data)
        {
            MemoryStream m = new MemoryStream(data);
            m.Seek(0, 0);
            UInt64 hash = ReadU64(m);
            m = new MemoryStream();
            m.Write(data, 8, data.Length - 8);
            UInt64 hash2 = Hash(m.ToArray());
            return hash == hash2;
        }

        public UInt64 ReadU64(Stream s)
        {
            UInt64 result = 0;
            for (int i = 0; i < 8; i++)
            {
                result <<= 8;
                result |= (byte)s.ReadByte();
            }
            return result;
        }

        public uint ReadU32(Stream s)
        {
            uint result = 0;
            for (int i = 0; i < 4; i++)
            {
                result <<= 8;
                result |= (byte)s.ReadByte();
            }
            return result;
        }

        public ushort ReadU16(Stream s)
        {
            return (ushort)((byte)s.ReadByte() << 8 | (byte)s.ReadByte());
        }

        public void WriteU64(UInt64 u, Stream s)
        {
            byte[] buff = BitConverter.GetBytes(u);
            for (int i = 0; i < 8; i++)
                s.WriteByte(buff[7 - i]);
        }

        public void WriteU32(uint u, Stream s)
        {
            byte[] buff = BitConverter.GetBytes(u);
            for (int i = 0; i < 4; i++)
                s.WriteByte(buff[3 - i]);
        }

        public void WriteU16(ushort u, Stream s)
        {
            s.WriteByte((byte)(u >> 8));
            s.WriteByte((byte)(u & 0xFF));
        }

        public ushort ReadBits(byte[] buff, int pos, int len)
        {
            ushort result = 0;
            for (int i = 0; i < len; i++)
            {
                result = (ushort)(result << 1);
                if (getBit(buff, pos + i))
                    result |= 1;
            }
            return result;
        }

        public bool getBit(byte[] buff, int pos)
        {
            byte b = buff[pos / 8];
            b = (byte)(b >> (7 - (pos % 8)));
            return (b & 1) == 1;
        }

        public void WriteBits(byte[] buff, int pos, int len, ushort u)
        {
            for (int i = 0; i < len; i++)
            {
                uint bit = (uint)(u >> ((len - 1) - i)) & 1;
                setBit(buff, pos + i, bit);
            }
        }

        public void setBit(byte[] buff, int pos, uint bit)
        {
            int byt = pos / 8;
            int subbit = pos % 8;
            byte b = buff[byt];
            byte mask = (byte)(1 << (7 - subbit));
            mask ^= 0xFF;
            b = (byte)(b & mask);
            b |= (byte)(bit << (7 - subbit));
            buff[byt] = b;
        }

        private string toTime(ushort sec)
        {
            if (sec == 0x3ff)
                return "--:--";
            else
                return (sec / 60) + ":" + (sec % 60).ToString("D2");
        }

        private UInt64 Hash(byte[] data)
        {
            uint seedLo = 0x3108B3C1;
            uint seedHi = 0x8F809F47;
            uint s1 = 0;
            UInt64 resultHi = 0;
            UInt64 resultLo = 0;
            for (int i = 0; i < data.Length; i++)
            {
                byte t8 = data[i];
                uint t9 = s1 & 0xF;
                uint t0 = (uint)(t8 << (byte)t9);
                uint t7 = t0 + seedLo;
                uint t6 = t0 >> 31;
                if (t7 < seedLo)
                    t6++;
                t6 += seedHi;
                seedHi = t6;
                seedLo = t7;
                UInt64 newSeed = SubHash(seedHi, seedLo);
                s1 += 7;
                resultHi ^= (uint)newSeed;
                seedLo = (uint)newSeed;
                seedHi = (uint)(newSeed >> 32);
            }
            for (int i = data.Length - 1; i >= 0; i--)
            {
                byte t8 = data[i];
                uint t9 = s1 & 0xF;
                uint t0 = (uint)(t8 << (byte)t9);
                uint t7 = t0 + seedLo;
                uint t6 = t0 >> 31;
                if (t7 < seedLo)
                    t6++;
                t6 += seedHi;
                seedHi = t6;
                seedLo = t7;
                UInt64 newSeed = SubHash(seedHi, seedLo);
                s1 += 3;
                resultLo ^= (uint)newSeed;
                seedLo = (uint)newSeed;
                seedHi = (uint)(newSeed >> 32);
            }
            return resultHi << 32 | resultLo;
        }

        private UInt64 SubHash(uint hi, uint lo)
        {
            UInt64 a3 = (UInt64)hi << 32 | lo;
            UInt64 a2 = a3 << 63;
            UInt64 a1 = a3 << 31;
            a2 = a2 >> 31;
            a1 = a1 >> 32;
            a3 = a3 << 44;
            a2 |= a1;
            a3 = a3 >> 32;
            a2 ^= a3;
            a3 = a2 >> 20;
            a3 &= 0xFFF;
            a3 ^= a2;
            return a3;
        }

        private enum Level
        {
            Dam,
            Facility,
            Runway,
            Surface1,
            Bunker1,
            Silo,
            Frigate,
            Surface2,
            Bunker2,
            Statue,
            Archives,
            Streets,
            Depot,
            Train,
            Jungle,
            Control,
            Caverns,
            Cradle,
            Aztec,
            Egypt
        }
    }
}
