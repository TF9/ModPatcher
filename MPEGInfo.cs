using System;
using System.IO;

public class MPEGInfo
{
	public class Video
	{
		public int GopHeaders;

		public int Height;

		public int Width;

		public double FrameRate;

		public int AspectRatioCode;

		public string AspectRatio;

		public int BitRate;

		public double Duration;

		public double MuxRate;

		public int ChromaFormat;

		public string ChromaFormatText;

		public int Format;

		public string FormatText;

		public int Frames;

		public int Version = 1;
	}

	public class Audio
	{
		public double Version;

		public int Layer;

		public bool Protected;

		public int BitRate;

		public float ByteRate;

		public int SamplingRate;

		public bool Padding;

		public int ModeCode;

		public int ModeXt;

		public bool Copyright;

		public int EmphasisIndex;

		public bool Original;

		public int FrameLength;

		public double Duration;

		public int Frames;
	}

	private int[,,] BitRateIndex = new int[2, 3, 16]
	{
		{
			{
				0,
				32,
				64,
				96,
				128,
				160,
				192,
				224,
				256,
				288,
				320,
				352,
				384,
				416,
				448,
				0
			},
			{
				0,
				32,
				48,
				56,
				64,
				80,
				96,
				112,
				128,
				160,
				192,
				224,
				256,
				320,
				384,
				0
			},
			{
				0,
				32,
				40,
				48,
				56,
				64,
				80,
				96,
				112,
				128,
				160,
				192,
				224,
				256,
				320,
				0
			}
		},
		{
			{
				0,
				32,
				48,
				56,
				64,
				80,
				96,
				112,
				128,
				144,
				160,
				176,
				192,
				224,
				256,
				0
			},
			{
				0,
				8,
				16,
				24,
				32,
				40,
				48,
				56,
				64,
				80,
				96,
				112,
				128,
				144,
				160,
				0
			},
			{
				0,
				8,
				16,
				24,
				32,
				40,
				48,
				56,
				64,
				80,
				96,
				112,
				128,
				144,
				160,
				0
			}
		}
	};

	private int[,] SamplingIndex = new int[3, 4]
	{
		{
			44100,
			48000,
			32000,
			0
		},
		{
			22050,
			24000,
			16000,
			0
		},
		{
			11025,
			12000,
			8000,
			0
		}
	};

	private double[] FrameRateIndex = new double[9]
	{
		0.0,
		23.976023976023978,
		24.0,
		25.0,
		29.970029970029969,
		30.0,
		50.0,
		59.940059940059939,
		60.0
	};

	private string[] AspectRatioIndex = new string[5]
	{
		"Invalid",
		"1/1 (VGA)",
		"4/3 (TV)",
		"16/9 (Large TV)",
		"2.21/1 (Cinema)"
	};

	public string[] ModeIndex = new string[4]
	{
		"Stereo",
		"Joint Stereo",
		"Dual Channel",
		"Mono"
	};

	public string[] EmphasisIndex = new string[4]
	{
		"No Emphasis",
		"50/15 Micro seconds",
		"Unknown",
		"CCITT J 17"
	};

	public Video VideoInfo;

	public Audio AudioInfo;

	public bool EnableTrace;

	private const byte PADDING_PACKET = 190;

	private const byte VIDEO_PACKET = 224;

	private const byte AUDIO_PACKET = 192;

	private const byte SYSTEM_PACKET = 187;

	private const double FLOAT_0x10000 = 65536.0;

	private const uint STD_SYSTEM_CLOCK_FREQ = 90000u;

	private const int BUFFER_SIZE = 8192;

	private int _mpegVersion = 1;

	private long _fileSize;

	private double _initialTS;

	private bool _mpeg2Found;

	private byte[] _backwardBuffer;

	private byte[] _forwardBuffer;

	private FileStream _fs;

	private BinaryReader _br;

	private BinaryWriter _bw;

	private int _backwardBufferStart;

	private int _backwardBufferEnd;

	private int _forwardBufferStart;

	private int _forwardBufferEnd;

	private bool hasAudio;

	public MPEGInfo(string file)
	{
		if (File.Exists(file))
		{
			_fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
			_br = new BinaryReader(_fs);
			_bw = new BinaryWriter(_fs);
		}
	}

	public void Parse()
	{
		if (_br != null)
		{
			_backwardBuffer = new byte[8192];
			_forwardBuffer = new byte[8192];
			_fileSize = _fs.Length;
			VideoInfo = new Video();
			AudioInfo = new Audio();
			ParseVideo(0);
			ParseAudio();
			ParseSystem(0);
		}
		_br.Close();
	}

	public int Safe()
	{
		return 1;
	}

	private byte GetByteBackwards(int offset)
	{
		int num = -1;
		if (offset >= _backwardBufferEnd || offset < _backwardBufferStart)
		{
			num = offset - 8192 + 1;
			if (num < 0)
			{
				num = 0;
			}
			_backwardBufferStart = num;
			_backwardBufferEnd = offset;
			_br.BaseStream.Seek(num, SeekOrigin.Begin);
			_br.Read(_backwardBuffer, 0, 8192);
		}
		return _backwardBuffer[offset - _backwardBufferStart];
	}

	private byte GetByte(int offset)
	{
		int num = -1;
		if (offset >= _forwardBufferEnd || offset < _forwardBufferStart)
		{
			num = offset + 8192;
			if (num > _fileSize)
			{
				num = (int)_fileSize;
			}
			_forwardBufferStart = offset;
			_forwardBufferEnd = num;
			_br.BaseStream.Seek(offset, SeekOrigin.Begin);
			_br.Read(_forwardBuffer, 0, 8192);
		}
		return _forwardBuffer[offset - _forwardBufferStart];
	}

	private int GetSize(int offset)
	{
		return GetByte(offset) * 256 + GetByte(offset + 1);
	}

	private bool EnsureMPEG(out int offset, byte marker)
	{
		offset = 0;
		for (int i = 0; i < _fileSize - 4; i++)
		{
			if (GetByte(i) == 0 && GetByte(i + 1) == 0 && GetByte(i + 2) == 1 && GetByte(i + 3) == marker)
			{
				offset = i;
				return true;
			}
		}
		return false;
	}

	private int FindMarkerBackwards(int offset, byte marker)
	{
		for (int num = offset; num > 0; num--)
		{
			if (GetByteBackwards(num) == 0 && GetByteBackwards(num + 1) == 0 && GetByteBackwards(num + 2) == 1 && GetByteBackwards(num + 3) == marker)
			{
				return num;
			}
		}
		return -1;
	}

	private bool MarkerExistsAt(out int offset, byte marker, int from)
	{
		offset = -1;
		for (int i = from; i < _fileSize - 4; i++)
		{
			if (GetByte(i) == 0 && GetByte(i + 1) == 0 && GetByte(i + 2) == 1 && GetByte(i + 3) == marker)
			{
				offset = i;
				return true;
			}
		}
		return false;
	}

	private int FindNextMarker(int from, byte marker)
	{
		int num = from;
		while (from >= 0 && from < _fileSize - 4)
		{
			num = FindNextMarker(from);
			if (num == -1)
			{
				return -1;
			}
			if (MarkerExistsAt(out num, marker, from))
			{
				return num;
			}
			from++;
		}
		return -1;
	}

	private int FindNextMarker(int from, ref byte marker)
	{
		int num = FindNextMarker(from);
		if (num > -1)
		{
			marker = GetByte(num + 3);
			return num;
		}
		return -1;
	}

	private int FindNextMarker(int from)
	{
		for (int i = from; i < _fileSize - 4; i++)
		{
			if (GetByte(i) == 0 && GetByte(i + 1) == 0 && GetByte(i + 2) == 1)
			{
				return i;
			}
		}
		return -1;
	}

	private void ParseSystem(int offset)
	{
		bool flag = true;
		byte marker = 0;
		if (EnsureMPEG(out offset, 186))
		{
			int num = 0;
			byte b = 0;
			while (flag)
			{
				offset = FindNextMarker(offset, ref marker);
				if (offset == -1)
				{
					break;
				}
				switch (marker)
				{
				case 190:
					offset += GetSize(offset + 4);
					continue;
				case 186:
					FindMuxRate(offset + 4);
					offset += 12;
					continue;
				default:
					offset += 4;
					continue;
				case 187:
				{
					int num2 = FindNextMarker(offset, 186);
					if (num2 != -1)
					{
						num = (((GetByte(num2 + 4) & 0xF0) == 32) ? 12 : (((GetByte(num2 + 4) & 0xC0) != 64) ? 12 : (14 + (GetByte(num2 + 13) & 7))));
					}
					if (num2 == -1 || num2 + num != offset)
					{
						num2 = offset;
					}
					GetSize(offset + 4);
					b = GetByte(offset + 12);
					if (GetByte(offset + 15) == 192 || GetByte(offset + 15) == 224)
					{
						b = 224;
					}
					if (b != 192 && b == 224)
					{
						if (num == 12)
						{
							_initialTS = ReadTS(offset - num);
							_mpeg2Found = false;
						}
						else
						{
							_initialTS = ReadTSMpeg2(offset - num);
							_mpeg2Found = true;
						}
					}
					offset += 4;
					continue;
				}
				case 192:
				case 224:
					break;
				}
				break;
			}
		}
		int num3 = FindMarkerBackwards((int)_fileSize - 13, 186);
		double num4;
		if ((GetByte(num3 + 4) & 0xF0) == 32)
		{
			num4 = ReadTS(num3 + 4);
		}
		else
		{
			num3 = FindMarkerBackwards((int)_fileSize - 8, 184);
			if ((GetByte(num3 + 4) & 0xC0) == 64 || _mpeg2Found)
			{
				num3 = FindMarkerBackwards((int)_fileSize - 8, 186);
				num4 = ReadTSMpeg2(num3 + 4);
			}
			else
			{
				num4 = ReadTSMpeg2(num3 + 4);
			}
		}
		num4 -= _initialTS;
		if (VideoInfo.Duration > 0.0 && VideoInfo.Duration > num4)
		{
			VideoInfo.Duration = num4;
		}
	}

	private double ReadTS(int offset)
	{
		byte num = (byte)((GetByte(offset) >> 3) & 1);
		uint num2 = (uint)(((GetByte(offset) >> 1) & 3) << 30);
		num2 = (uint)((int)num2 | (GetByte(offset + 1) << 22));
		num2 = (uint)((int)num2 | (GetByte(offset + 2) >> 1 << 15));
		num2 = (uint)((int)num2 | (GetByte(offset + 3) << 7));
		num2 = (uint)((int)num2 | (GetByte(offset + 4) >> 1));
		return ((double)(int)num * 65536.0 * 65536.0 + (double)num2) / 90000.0;
	}

	private double ReadTSMpeg2(int offset)
	{
		byte b = (byte)((GetByte(offset) & 0x20) >> 5);
		uint num = (uint)((GetByte(offset) & 0x18) >> 3 << 30);
		num = (uint)((int)num | ((GetByte(offset) & 3) << 28));
		num = (uint)((int)num | (GetByte(offset + 1) << 20));
		num = (uint)((int)num | ((GetByte(offset + 2) & 0xF8) << 12));
		num = (uint)((int)num | ((GetByte(offset + 2) & 3) << 13));
		num = (uint)((int)num | (GetByte(offset + 3) << 5));
		num = (uint)((int)num | (GetByte(offset + 4) >> 3));
		int num2 = ((GetByte(offset + 4) & 3) << 7) | (GetByte(offset + 5) >> 1);
		double num3 = (double)(int)b * 65536.0 * 65536.0;
		num3 += (double)num;
		if (num2 == 0)
		{
			return num3 / 90000.0;
		}
		return num3 / 90000.0;
	}

	private void FindMuxRate(int offset)
	{
		int num = 0;
		if ((GetByte(offset) & 0xC0) == 64)
		{
			num = GetByte(offset + 6) << 14;
			num |= GetByte(offset + 7) << 6;
			num |= GetByte(offset + 8) >> 2;
		}
		else
		{
			_ = (GetByte(offset) & 0xF0);
			_ = 32;
			num = (GetByte(offset + 5) & 0x7F) << 15;
			num |= GetByte(offset + 6) << 7;
			num |= GetByte(offset + 7) >> 1;
		}
		num *= 50;
		VideoInfo.MuxRate = (double)num * 8.0 / 1000000.0;
	}

	private void ParseAudio()
	{
		int num = 0;
		hasAudio = false;
		num = FindNextMarker(0, 192);
		if (num <= -1)
		{
			return;
		}
		if (num > -1)
		{
			num += 13;
		}
		if (ParseAudio(num))
		{
			return;
		}
		for (; num < _fileSize - 10; num++)
		{
			if (hasAudio)
			{
				break;
			}
			if (GetByte(num) == byte.MaxValue && (GetByte(num + 1) & 0xF0) == 240 && ParseAudio(num))
			{
				hasAudio = true;
			}
		}
	}

	private bool ParseAudio(int offset)
	{
		hasAudio = false;
		bool flag = false;
		if (GetByte(offset) != byte.MaxValue || (GetByte(offset + 1) & 0xF0) != 240)
		{
			if (GetByte(offset) != byte.MaxValue || (GetByte(offset + 1) & 0xE0) != 224)
			{
				return false;
			}
			flag = true;
		}
		if (Convert.ToBoolean(GetByte(offset + 1) & 8))
		{
			if (flag)
			{
				return false;
			}
			AudioInfo.Version = 1.0;
		}
		else if (!flag)
		{
			AudioInfo.Version = 2.0;
		}
		else
		{
			AudioInfo.Version = 3.0;
		}
		AudioInfo.Layer = (GetByte(offset + 1) & 6) >> 1;
		switch (AudioInfo.Layer)
		{
		case 0:
			AudioInfo.Layer = -1;
			return false;
		case 1:
			AudioInfo.Layer = 3;
			break;
		case 2:
			AudioInfo.Layer = 2;
			break;
		case 3:
			AudioInfo.Layer = 1;
			break;
		default:
			AudioInfo.Layer = -1;
			return false;
		}
		AudioInfo.Protected = Convert.ToBoolean(GetByte(offset + 1) & 1);
		int num = GetByte(offset + 2) >> 4;
		int num2 = (GetByte(offset + 2) & 0xF) >> 2;
		if (num2 >= 3)
		{
			return false;
		}
		if (num == 15)
		{
			return false;
		}
		AudioInfo.BitRate = BitRateIndex[(int)AudioInfo.Version - 1, AudioInfo.Layer - 1, num];
		AudioInfo.ByteRate = (float)((double)(AudioInfo.BitRate * 1000) / 8.0);
		AudioInfo.SamplingRate = SamplingIndex[(int)AudioInfo.Version - 1, num2];
		if (AudioInfo.BitRate <= 0 || AudioInfo.ByteRate <= 0f || AudioInfo.SamplingRate <= 0)
		{
			return false;
		}
		if (Convert.ToBoolean(GetByte(offset + 2) & 2))
		{
			AudioInfo.Padding = true;
		}
		else
		{
			AudioInfo.Padding = false;
		}
		AudioInfo.ModeCode = GetByte(offset + 3) >> 6;
		GetByte(offset + 3);
		if (Convert.ToBoolean(GetByte(offset + 3) & 8))
		{
			AudioInfo.Copyright = true;
		}
		else
		{
			AudioInfo.Copyright = false;
		}
		if (Convert.ToBoolean(GetByte(offset + 3) & 4))
		{
			AudioInfo.Original = true;
		}
		else
		{
			AudioInfo.Original = false;
		}
		AudioInfo.EmphasisIndex = (GetByte(offset + 3) & 3);
		if (AudioInfo.Version == 1.0)
		{
			if (AudioInfo.Layer == 1)
			{
				AudioInfo.FrameLength = 48000 * AudioInfo.BitRate / AudioInfo.SamplingRate + 4 * Convert.ToInt32(AudioInfo.Padding);
			}
			else
			{
				AudioInfo.FrameLength = 144000 * AudioInfo.BitRate / AudioInfo.SamplingRate + Convert.ToInt32(AudioInfo.Padding);
			}
		}
		else
		{
			if (AudioInfo.Version != 2.0)
			{
				return false;
			}
			if (AudioInfo.Layer == 1)
			{
				AudioInfo.FrameLength = 24000 * AudioInfo.BitRate / AudioInfo.SamplingRate + 4 * Convert.ToInt32(AudioInfo.Padding);
			}
			else
			{
				AudioInfo.FrameLength = 72000 * AudioInfo.BitRate / AudioInfo.SamplingRate + Convert.ToInt32(AudioInfo.Padding);
			}
		}
		if (AudioInfo.Protected)
		{
			AudioInfo.FrameLength += 2;
		}
		AudioInfo.Duration = (double)_fileSize * 1.0 / (double)AudioInfo.BitRate * 0.008;
		hasAudio = true;
		return hasAudio;
	}

	private bool ParseVideo(int offset)
	{
		bool flag = false;
		if (EnsureMPEG(out offset, 179))
		{
			flag = true;
		}
		if (flag)
		{
			offset += 4;
			VideoInfo.Width = GetSize(offset) >> 4;
			VideoInfo.Height = (GetSize(offset + 1) & 0xFFF);
			offset += 3;
			int num = GetByte(offset) & 0xF;
			if (num > 8)
			{
				VideoInfo.FrameRate = 0.0;
			}
			else
			{
				VideoInfo.FrameRate = FrameRateIndex[num];
			}
			VideoInfo.AspectRatioCode = (GetByte(offset) & 0xF0) >> 4;
			if (VideoInfo.AspectRatioCode <= 4)
			{
				VideoInfo.AspectRatio = AspectRatioIndex[VideoInfo.AspectRatioCode];
			}
			else
			{
				VideoInfo.AspectRatio = "Unknown";
			}
			offset++;
			VideoInfo.BitRate = GetSize(offset);
			VideoInfo.BitRate <<= 2;
			byte @byte = GetByte(offset + 2);
			@byte = (byte)(@byte >> 6);
			VideoInfo.BitRate |= @byte;
			VideoInfo.Duration = (double)_fileSize / ((double)(VideoInfo.BitRate * 400) / 8.0);
			byte marker = 0;
			while (true)
			{
				offset = FindNextMarker(offset, ref marker);
				if (offset <= -1 || marker == 184)
				{
					break;
				}
				byte byte2 = GetByte(offset + 3);
				if (byte2 == 181)
				{
					ParseExtension(offset);
				}
				offset++;
			}
			switch (VideoInfo.ChromaFormat)
			{
			case 1:
				VideoInfo.ChromaFormatText = "4:2:0";
				break;
			case 2:
				VideoInfo.ChromaFormatText = "4:2:2";
				break;
			case 3:
				VideoInfo.ChromaFormatText = "4:4:4";
				break;
			default:
				VideoInfo.ChromaFormatText = "Unknown";
				break;
			}
			switch (VideoInfo.Format)
			{
			case 0:
				VideoInfo.FormatText = "Component";
				break;
			case 1:
				VideoInfo.FormatText = "PAL";
				break;
			case 2:
				VideoInfo.FormatText = "NTSC";
				break;
			case 3:
				VideoInfo.FormatText = "SECAM";
				break;
			case 4:
				VideoInfo.FormatText = "MAC";
				break;
			case 5:
				VideoInfo.FormatText = "Unspecified";
				break;
			default:
				VideoInfo.FormatText = "Unknown";
				break;
			}
		}
		return true;
	}

	private string SecondsToHMS(double duration)
	{
		int num = (int)(duration / 3600.0);
		int num2 = (int)(duration / 60.0 - (double)(num * 60));
		double num3 = duration - (double)(60 * num2) - (double)(3600 * num);
		if (num != 0)
		{
			return $"{num:n0}h {num2:n0}m {num3:n2}s";
		}
		if (num2 != 0)
		{
			return $"{num2:n0}m {num3:n2}s";
		}
		return $"{num3:n2}s";
	}

	private void CountVideoFrames()
	{
		byte[] array = new byte[4];
		_br.BaseStream.Seek(0L, SeekOrigin.Begin);
		while (_br.Read(array, 0, 4) != 0)
		{
			if (array[0] == 0 && array[1] == 0 && array[2] == 1 && array[3] == 0)
			{
				VideoInfo.Frames++;
			}
		}
	}

	private void CountVideoGopHeaders()
	{
		byte[] array = new byte[4];
		_br.BaseStream.Seek(0L, SeekOrigin.Begin);
		while (_br.Read(array, 0, 4) != 0)
		{
			if (array[0] == 0 && array[1] == 0 && array[2] == 1 && array[3] == 184)
			{
				VideoInfo.GopHeaders++;
			}
		}
	}

	private void ParseExtension(int offset)
	{
		offset += 4;
		switch (GetByte(offset) >> 4)
		{
		case 1:
			ParseSequenceExt(offset);
			break;
		case 2:
			ParseSequenceDisplayExt(offset);
			break;
		}
	}

	private void ParseSequenceExt(int offset)
	{
		_mpegVersion = 2;
		VideoInfo.Version = 2;
		VideoInfo.ChromaFormat = (GetByte(offset + 1) & 6) >> 1;
	}

	private void ParseSequenceDisplayExt(int offset)
	{
		VideoInfo.Format = (GetByte(offset) & 0xE) >> 1;
	}

	private int SkipPacketHeader(int offset)
	{
		byte b = 0;
		if (_mpegVersion == 1)
		{
			offset += 6;
			b = GetByte(offset);
			while (Convert.ToBoolean(b & 0x80))
			{
				b = GetByte(++offset);
			}
			if ((b & 0xC0) == 64)
			{
				offset += 2;
			}
			b = GetByte(offset);
			offset = (((b & 0xF0) == 32) ? (offset + 5) : (((b & 0xF0) != 48) ? (offset + 1) : (offset + 10)));
			return offset;
		}
		if (_mpegVersion == 2)
		{
			return offset + 9 + GetByte(offset + 8);
		}
		return offset + 10;
	}

	private void Trace(string msg)
	{
		if (EnableTrace)
		{
			Console.WriteLine(msg);
		}
	}
}
