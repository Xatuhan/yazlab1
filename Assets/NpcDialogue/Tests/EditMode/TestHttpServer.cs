using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace YazLab.NpcDialogue.Tests
{
    // Yalnızca test: 127.0.0.1'de tek isteğe cevap veren küçük sunucu. Ollama'nın hata hâllerini
    // (cevapsız, 404, 500, bozuk, çok büyük) gerçek ağ üzerinden denemek için. Build'e girmez.
    internal sealed class TestHttpServer : IDisposable
    {
        private readonly TcpListener _listener;

        public TestHttpServer()
        {
            _listener = new TcpListener(IPAddress.Loopback, 0); // 0: işletim sistemi boş port verir
            _listener.Start();
        }

        public string Url => "http://127.0.0.1:" + ((IPEndPoint)_listener.LocalEndpoint).Port;

        // Çağrılmazsa bağlantı kurulur ama cevap hiç gelmez (zaman aşımı testi).
        public void RespondOnce(int status, string body)
        {
            Task.Run(() =>
            {
                try
                {
                    using (TcpClient client = _listener.AcceptTcpClient())
                    using (NetworkStream stream = client.GetStream())
                    {
                        ReadRequest(stream);
                        byte[] payload = Encoding.UTF8.GetBytes(body);
                        byte[] head = Encoding.ASCII.GetBytes(
                            $"HTTP/1.1 {status} Test\r\nContent-Type: application/json\r\n" +
                            $"Content-Length: {payload.Length}\r\nConnection: close\r\n\r\n");
                        stream.Write(head, 0, head.Length);
                        stream.Write(payload, 0, payload.Length);
                    }
                }
                catch (Exception)
                {
                    // İstemci erken kapatırsa (ör. 64 KB sınırı) yazma hata verir; test için önemsiz.
                }
            });
        }

        // İstek başlığını ve Content-Length kadar gövdeyi okur ki istemci bağlantı sıfırlanması görmesin.
        private static void ReadRequest(NetworkStream stream)
        {
            var data = new MemoryStream();
            var buf = new byte[4096];
            int headerEnd;
            while ((headerEnd = Encoding.ASCII.GetString(data.ToArray()).IndexOf("\r\n\r\n", StringComparison.Ordinal)) < 0)
            {
                int n = stream.Read(buf, 0, buf.Length);
                if (n <= 0)
                    return;
                data.Write(buf, 0, n);
            }
            string header = Encoding.ASCII.GetString(data.ToArray(), 0, headerEnd);
            Match m = Regex.Match(header, @"Content-Length:\s*(\d+)", RegexOptions.IgnoreCase);
            long remaining = (m.Success ? long.Parse(m.Groups[1].Value) : 0) - (data.Length - headerEnd - 4);
            while (remaining > 0)
            {
                int n = stream.Read(buf, 0, buf.Length);
                if (n <= 0)
                    return;
                remaining -= n;
            }
        }

        public void Dispose() => _listener.Stop();
    }
}
