using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public class FileContent : HttpContent
    {
        private readonly Stream _fstream;
        public FileContent(string path, string mediaType = "application/octet-stream")
        {
            _fstream = new FileStream(path, FileMode.Open, FileAccess.Read);
            base.Headers.ContentType = new MediaTypeHeaderValue(mediaType);
        }
        protected override Task SerializeToStreamAsync(Stream stream, TransportContext ctx)
        {
            return _fstream.CopyToAsync(stream);
        }
        protected override bool TryComputeLength(out long length)
        {
            if (!_fstream.CanSeek) { length = 0; return false; }
            else { length = _fstream.Length; return true; }
        }
        protected override void Dispose(bool disposing)
        {
            _fstream.Dispose();
        }
    }
}
