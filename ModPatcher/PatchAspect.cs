using BytePatternMatch;
using System;
using System.Collections.Generic;
using System.IO;

namespace ModPatcher
{
	public class PatchAspect
	{
		public int PatchAspectRatio16_9(FileStream fs)
		{
			int num = 0;
			using (BinaryReader binaryReader = new BinaryReader(fs))
			{
				List<int> m_dist = new List<int>();
				int last = 0;
				byte[] pattern = new byte[7]{ 0x00, 0x00, 0x01, 0xB3, 0x2C, 0x02, 0x40 };
				byte[] text = binaryReader.ReadBytes((int)fs.Length);
				BoyerMoore bm = new BoyerMoore(pattern);
				IEnumerable<int> ret = bm.BoyerMooreMatch(text, 0);

				foreach (int item in ret)
				{
					m_dist.Add(item - last);
					last = item;
					fs.Seek(item + 7, SeekOrigin.Begin);
					byte b = (byte)fs.ReadByte();
					b = (byte)(b & 0xF);
					b = (byte)(b | 0x30);
					fs.Seek(-1L, SeekOrigin.Current);
					//fs.WriteByte(b);
					num++;
				}
				return num;
			}
		}
	}
}
