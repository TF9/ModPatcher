using System;
using System.Collections.Generic;
using System.IO;

namespace BytePatternMatch
{
	public class BoyerMoore
	{
		private int[] m_badByteShift;
		private int[] m_goodSuffixShift;
		private int[] m_suffixes;
		private byte[] m_pattern;

		public BoyerMoore(byte[] pattern)
		{
			m_pattern = pattern;
			m_badByteShift = BuildBadByteShift(pattern);
			m_suffixes = FindSuffixes(pattern);
			m_goodSuffixShift = BuildGoodSuffixShift(pattern, m_suffixes);
		}

		private int[] BuildBadByteShift(byte[] pattern)
		{
			int[] array = new int[256];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = pattern.Length;
			}
			for (int j = 0; j < pattern.Length - 1; j++)
			{
				array[pattern[j]] = pattern.Length - j - 1;
			}
			return array;
		}

		private int[] FindSuffixes(byte[] pattern)
		{
			int num = 0;
			int num2 = pattern.Length;
			int[] array = new int[pattern.Length + 1];
			array[num2 - 1] = num2;
			int num3 = num2 - 1;
			for (int num4 = num2 - 2; num4 >= 0; num4--)
			{
				if (num4 > num3 && array[num4 + num2 - 1 - num] < num4 - num3)
				{
					array[num4] = array[num4 + num2 - 1 - num];
				}
				else
				{
					if (num4 < num3)
					{
						num3 = num4;
					}
					num = num4;
					while (num3 >= 0 && pattern[num3] == pattern[num3 + num2 - 1 - num])
					{
						num3--;
					}
					array[num4] = num - num3;
				}
			}
			return array;
		}

		private int[] BuildGoodSuffixShift(byte[] pattern, int[] suff)
		{
			int num = pattern.Length;
			int[] array = new int[pattern.Length + 1];
			for (int i = 0; i < num; i++)
			{
				array[i] = num;
			}
			int j = 0;
			for (int num2 = num - 1; num2 >= -1; num2--)
			{
				if (num2 == -1 || suff[num2] == num2 + 1)
				{
					for (; j < num - 1 - num2; j++)
					{
						if (array[j] == num)
						{
							array[j] = num - 1 - num2;
						}
					}
				}
			}
			for (int k = 0; k <= num - 2; k++)
			{
				array[num - 1 - suff[k]] = num - 1 - k;
			}
			return array;
		}

		public IEnumerable<int> BoyerMooreMatch(byte[] text, int startingIndex)
		{
			int patternLength = m_pattern.Length;
			int textLength = text.Length;
			int index = startingIndex;
			int tipps = 0;
			while (index <= textLength - patternLength)
			{
				int unmatched = patternLength - 1;
				while (unmatched >= 0 && m_pattern[unmatched] == text[unmatched + index])
				{
					int num = unmatched - 1;
					unmatched = num;
					++tipps;
				}
				if (unmatched < 0)
				{
					yield return index;
					index += m_goodSuffixShift[0];
				}
				else
				{
					index += Math.Max(m_goodSuffixShift[unmatched], m_badByteShift[text[unmatched + index]] - patternLength + 1 + unmatched);
					++tipps;
				}
			}
		}
	}
}
