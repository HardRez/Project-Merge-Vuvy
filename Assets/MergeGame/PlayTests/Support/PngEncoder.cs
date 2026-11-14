using System.IO;
using System.IO.Compression;
using System.Text;

namespace MergeGame.PlayTests.Support
{
    /// <summary>
    /// Minimal PNG writer for 8-bit RGB images. The project does not include Unity's Image Conversion module (which
    /// provides <c>EncodeToPNG</c>), and the screenshots are a test-only concern, so this avoids adding it.
    /// </summary>
    internal static class PngEncoder
    {
        private static readonly byte[] Signature = { 137, 80, 78, 71, 13, 10, 26, 10 };
        private static readonly uint[] CrcTable = BuildCrcTable();

        /// <param name="rgb">Rows bottom to top, as Unity reads pixels; 3 bytes per pixel.</param>
        public static byte[] Encode(byte[] rgb, int width, int height)
        {
            using (var png = new MemoryStream())
            {
                png.Write(Signature, 0, Signature.Length);
                WriteChunk(png, "IHDR", Header(width, height));
                WriteChunk(png, "IDAT", Compress(rgb, width, height));
                WriteChunk(png, "IEND", new byte[0]);
                return png.ToArray();
            }
        }

        private static byte[] Header(int width, int height)
        {
            var header = new byte[13];
            WriteBigEndian(header, 0, (uint)width);
            WriteBigEndian(header, 4, (uint)height);
            header[8] = 8; // bit depth
            header[9] = 2; // colour type: RGB
            return header;
        }

        /// <summary>zlib stream of the scanlines, top row first, each prefixed with filter type 0.</summary>
        private static byte[] Compress(byte[] rgb, int width, int height)
        {
            var stride = width * 3;
            var raw = new byte[(stride + 1) * height];
            for (var row = 0; row < height; row++)
                System.Buffer.BlockCopy(rgb, (height - 1 - row) * stride, raw, row * (stride + 1) + 1, stride);

            using (var output = new MemoryStream())
            {
                output.WriteByte(0x78);
                output.WriteByte(0x9C);
                using (var deflate = new DeflateStream(output, CompressionLevel.Fastest, leaveOpen: true))
                    deflate.Write(raw, 0, raw.Length);

                var adler = Adler32(raw);
                output.Write(new[] { (byte)(adler >> 24), (byte)(adler >> 16), (byte)(adler >> 8), (byte)adler }, 0, 4);
                return output.ToArray();
            }
        }

        private static void WriteChunk(Stream stream, string type, byte[] data)
        {
            var length = new byte[4];
            WriteBigEndian(length, 0, (uint)data.Length);
            stream.Write(length, 0, 4);

            var typeAndData = new byte[4 + data.Length];
            Encoding.ASCII.GetBytes(type, 0, 4, typeAndData, 0);
            System.Buffer.BlockCopy(data, 0, typeAndData, 4, data.Length);
            stream.Write(typeAndData, 0, typeAndData.Length);

            var crc = new byte[4];
            WriteBigEndian(crc, 0, Crc32(typeAndData));
            stream.Write(crc, 0, 4);
        }

        private static uint Adler32(byte[] data)
        {
            uint a = 1, b = 0;
            foreach (var value in data)
            {
                a = (a + value) % 65521;
                b = (b + a) % 65521;
            }

            return (b << 16) | a;
        }

        private static uint Crc32(byte[] data)
        {
            var crc = 0xFFFFFFFFu;
            foreach (var value in data)
                crc = CrcTable[(crc ^ value) & 0xFF] ^ (crc >> 8);
            return crc ^ 0xFFFFFFFFu;
        }

        private static uint[] BuildCrcTable()
        {
            var table = new uint[256];
            for (uint n = 0; n < 256; n++)
            {
                var c = n;
                for (var k = 0; k < 8; k++)
                    c = (c & 1) != 0 ? 0xEDB88320u ^ (c >> 1) : c >> 1;
                table[n] = c;
            }

            return table;
        }

        private static void WriteBigEndian(byte[] buffer, int offset, uint value)
        {
            buffer[offset] = (byte)(value >> 24);
            buffer[offset + 1] = (byte)(value >> 16);
            buffer[offset + 2] = (byte)(value >> 8);
            buffer[offset + 3] = (byte)value;
        }
    }
}
