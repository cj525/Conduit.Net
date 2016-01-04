using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Pipes.Example.Implementation
{
    class StockTickGenerator : Stream
    {

        private static readonly int[] _lengths = {sizeof (long), sizeof (byte) * 4, sizeof(decimal)};
        private int _position;
        private int _length;
        private int _width;
        private int _entries;
        private int _entry;
        private Random _rand;
        private Encoding _encoder;
        private string _lastChomp;

        public StockTickGenerator(int entries)
        {
            _position = 0;
            _entries = entries;

            // Width is each length, plus delimiter, minus last delimiter
            _width = _lengths.Select(x => x + 1).Sum() - 1;
            _length = _width*entries;
            _rand = new Random();
            _encoder = Encoding.ASCII;
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {

            StringBuilder sb = new StringBuilder();
            if (_lastChomp != null)
                sb.Append(_lastChomp);

            var chomp = count;
            while (chomp >=  _width && _entry++ <= _entries)
            {
                sb.Append(CreateEntry());
                chomp -= _width;
            }

            var overflow = chomp - _width;
            if (overflow > 0 && _entry < _entries)
            {
                var lastLine = CreateEntry();
                sb.Append(lastLine.Substring(0, overflow));
                _lastChomp = lastLine.Substring(overflow);
            }
            else
            {
                _lastChomp = null;
            }

            var data = sb.ToString();
            var length = data.Length;
            var bytes =_encoder.GetBytes(data);
            Array.Copy(bytes, buffer, length);
            _position += length;
            return length;
        }

        private string CreateEntry()
        {
            var change = Math.Round(_rand.NextDouble(), 2)*10d;
            var symbol = "XYZ" + (_entry++%10);
            if (_rand.NextDouble() > 0.9d)
            {
                symbol = "_ALL";
            }
            var data = String.Join(",", new object[] {_entry, symbol, change}) + Environment.NewLine;
            return data;
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override bool CanRead { get { return false; } }
        public override bool CanSeek { get { return false; } }
        public override bool CanWrite { get { return false; } }
        public override long Length { get { return _length; } }
        public override long Position { get { return _position; } set { _position = 0; } }
    }
}
