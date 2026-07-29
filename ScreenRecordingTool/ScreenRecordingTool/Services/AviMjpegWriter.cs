using System.IO;
using System.Text;

namespace ScreenRecordingTool.Services
{
    /// <summary>
    /// 手写 AVI(MJPEG) 容器写入器：将连续的 JPEG 帧封装为标准 AVI 文件，
    /// 不依赖任何第三方编码库，主流播放器均可播放。
    /// </summary>
    internal sealed class AviMjpegWriter : IDisposable
    {
        private const uint AvifHasIndex = 0x10;   // AVIF_HASINDEX
        private const uint AviifKeyframe = 0x10;  // AVIIF_KEYFRAME（MJPEG 每帧都是关键帧）

        private readonly FileStream _stream;
        private readonly BinaryWriter _writer;
        private readonly int _width;
        private readonly int _height;
        private readonly List<(uint Offset, uint Size)> _index = new();

        private readonly long _riffSizePos;
        private readonly long _totalFramesPos;
        private readonly long _streamLengthPos;
        private readonly long _moviListSizePos;
        private readonly long _moviFourccPos;

        private bool _finished;

        public AviMjpegWriter(string filePath, int width, int height, int fps)
        {
            _width = width;
            _height = height;
            _stream = new FileStream(filePath, FileMode.Create, FileAccess.ReadWrite, FileShare.Read);
            _writer = new BinaryWriter(_stream);

            uint suggestedBuf = (uint)(width * height * 3);

            // ---- RIFF 头 ----
            WriteFourcc("RIFF");
            _riffSizePos = _stream.Position;
            _writer.Write(0u);                     // 占位：文件总长-8，Finish 时回填
            WriteFourcc("AVI ");

            // ---- LIST hdrl（内容固定 192 字节）----
            WriteFourcc("LIST");
            _writer.Write(192u);
            WriteFourcc("hdrl");

            // avih 主头（56 字节）
            WriteFourcc("avih");
            _writer.Write(56u);
            _writer.Write((uint)(1_000_000 / fps)); // 每帧微秒数
            _writer.Write(0u);                      // dwMaxBytesPerSec
            _writer.Write(0u);                      // dwPaddingGranularity
            _writer.Write(AvifHasIndex);            // dwFlags
            _totalFramesPos = _stream.Position;
            _writer.Write(0u);                      // 占位：总帧数
            _writer.Write(0u);                      // dwInitialFrames
            _writer.Write(1u);                      // dwStreams
            _writer.Write(suggestedBuf);            // dwSuggestedBufferSize
            _writer.Write((uint)width);
            _writer.Write((uint)height);
            _writer.Write(0u); _writer.Write(0u); _writer.Write(0u); _writer.Write(0u); // 保留

            // LIST strl（内容固定 116 字节）
            WriteFourcc("LIST");
            _writer.Write(116u);
            WriteFourcc("strl");

            // strh 流头（56 字节）
            WriteFourcc("strh");
            _writer.Write(56u);
            WriteFourcc("vids");                    // 视频流
            WriteFourcc("MJPG");                    // MJPEG 编码
            _writer.Write(0u);                      // dwFlags
            _writer.Write(0u);                      // wPriority + wLanguage
            _writer.Write(0u);                      // dwInitialFrames
            _writer.Write(1u);                      // dwScale
            _writer.Write((uint)fps);               // dwRate => fps = rate/scale
            _writer.Write(0u);                      // dwStart
            _streamLengthPos = _stream.Position;
            _writer.Write(0u);                      // 占位：帧数
            _writer.Write(suggestedBuf);            // dwSuggestedBufferSize
            _writer.Write(0xFFFFFFFFu);             // dwQuality = -1（默认）
            _writer.Write(0u);                      // dwSampleSize
            _writer.Write((short)0); _writer.Write((short)0);
            _writer.Write((short)width); _writer.Write((short)height); // rcFrame

            // strf：BITMAPINFOHEADER（40 字节）
            WriteFourcc("strf");
            _writer.Write(40u);
            _writer.Write(40u);                     // biSize
            _writer.Write(width);
            _writer.Write(height);
            _writer.Write((ushort)1);               // biPlanes
            _writer.Write((ushort)24);              // biBitCount
            WriteFourcc("MJPG");                    // biCompression
            _writer.Write(suggestedBuf);            // biSizeImage
            _writer.Write(0); _writer.Write(0);
            _writer.Write(0u); _writer.Write(0u);

            // ---- LIST movi ----
            WriteFourcc("LIST");
            _moviListSizePos = _stream.Position;
            _writer.Write(0u);                      // 占位：movi 列表大小
            _moviFourccPos = _stream.Position;
            WriteFourcc("movi");
        }

        /// <summary>追加一帧 JPEG 数据。</summary>
        public void AddFrame(byte[] jpegData, int length)
        {
            ObjectDisposedException.ThrowIf(_finished, this);

            uint offset = (uint)(_stream.Position - _moviFourccPos); // 索引偏移以 'movi' 起算
            WriteFourcc("00dc");
            _writer.Write((uint)length);
            _writer.Write(jpegData, 0, length);
            if ((length & 1) == 1)
            {
                _writer.Write((byte)0);             // RIFF 块须 2 字节对齐
            }

            _index.Add((offset, (uint)length));
        }

        /// <summary>写入索引块并回填所有占位长度，使文件成为合法 AVI。</summary>
        public void Finish()
        {
            if (_finished)
            {
                return;
            }
            _finished = true;

            // 回填 movi 列表大小（从 'movi' fourcc 起到最后一帧末尾）
            long moviEnd = _stream.Position;
            PatchUInt32(_moviListSizePos, (uint)(moviEnd - _moviFourccPos));

            // idx1 索引块
            WriteFourcc("idx1");
            _writer.Write((uint)(_index.Count * 16));
            foreach (var (offset, size) in _index)
            {
                WriteFourcc("00dc");
                _writer.Write(AviifKeyframe);
                _writer.Write(offset);
                _writer.Write(size);
            }

            // 回填 RIFF 总长与帧数
            PatchUInt32(_riffSizePos, (uint)(_stream.Length - 8));
            PatchUInt32(_totalFramesPos, (uint)_index.Count);
            PatchUInt32(_streamLengthPos, (uint)_index.Count);
            _writer.Flush();
        }

        public void Dispose()
        {
            try
            {
                Finish();
            }
            finally
            {
                _writer.Dispose();
                _stream.Dispose();
            }
        }

        private void WriteFourcc(string fourcc) => _writer.Write(Encoding.ASCII.GetBytes(fourcc));

        private void PatchUInt32(long position, uint value)
        {
            long current = _stream.Position;
            _stream.Position = position;
            _writer.Write(value);
            _stream.Position = current;
        }
    }
}
