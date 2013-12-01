using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http.Headers;
using System.Speech.Synthesis;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;

namespace Common
{
    public class WaveContent : HttpContent
    {
        private readonly Lazy<Stream> _stream;

        public WaveContent(string text)
        {
            _stream = new Lazy<Stream>(() =>
            {
                using (var synth = new SpeechSynthesizer())
                {
                    var ms = new MemoryStream();
                    synth.SetOutputToWaveStream(ms);
                    synth.Speak(text);
                    ms.Seek(0, SeekOrigin.Begin);
                    return ms;
                }
            });

            Headers.ContentType = new MediaTypeHeaderValue("audio/x-wav");
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext ctx)
        {
            return _stream.Value.CopyToAsync(stream);
        }
        protected override bool TryComputeLength(out long length)
        {
            length = _stream.Value.Length;
            return true;
        }
        protected override void Dispose(bool disposing)
        {
            
        }
    }
}
