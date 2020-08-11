using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Godot;

namespace Janphe
{
    public class FileServer : Node, IWebResource
    {
        static string GetLocalIp()
        {
            var hostname = Dns.GetHostName();
            var localhost = Dns.GetHostEntry(hostname);

            //localhost.AddressList.forEach((d, i) =>
            //{
            //    /**
            //    dphe-i7 0 fe80::d43e:583b:86b7:6391 False True False False False
            //    dphe-i7 1 192.168.0.96 False False False False False
            //     */
            //    Debug.Log($"{hostname} {i} {d.ToString()} {d.IsIPv6SiteLocal} {d.IsIPv6LinkLocal} {d.IsIPv6Multicast} {d.IsIPv4MappedToIPv6} {d.IsIPv6Teredo}");
            //});

            var i = localhost.AddressList.findIndex(d => !d.IsIPv6LinkLocal);
            return i < 0 ?
                IPAddress.Loopback.ToString() :
                localhost.AddressList[i].ToString();
        }

        EmbeddedWebServerComponent server;

        public override void _Ready()
        {
            server = GetParent<EmbeddedWebServerComponent>();
            server.AddResource("/.h5", this);


            OS.ShellOpen($"http://{GetLocalIp()}:8079/.h5/index.html");
        }

        public void HandleRequest(Request request, Response response)
        {
            // check if file exist at folder (need to assume a base local root)
            var fullPath = "res://public" + Uri.UnescapeDataString(request.uri.LocalPath);
            // get file extension to add to header
            var fileExt = System.IO.Path.GetExtension(fullPath);
            //Debug.Log($"fullPath:{fullPath} fileExt:{fileExt}");

            var f = new Godot.File();
            // not found
            if (!f.FileExists(fullPath))
            {
                response.statusCode = 404;
                response.message = "Not Found";
                return;
            }

            // serve the file
            response.statusCode = 200;
            response.message = "OK";
            response.headers.Add("Content-Type", MimeTypeMap.GetMimeType(fileExt));

            var ret = f.Open(fullPath, Godot.File.ModeFlags.Read);
            // read file and set bytes
            if (ret == Error.Ok)
            {
                var length = (int)f.GetLen();
                // add content length
                response.headers.Add("Content-Length", length.ToString());
                response.SetBytes(f.GetBuffer(length));
            }
            f.Close();
        }

    }
}
