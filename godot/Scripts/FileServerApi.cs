using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Newtonsoft.Json.Linq;
using Godot;

namespace Janphe
{
    public class FileServerApi : Node, IWebResource
    {
        EmbeddedWebServerComponent server;

        Dictionary<string, Func<Request, string>> apiMap = new Dictionary<string, Func<Request, string>>();

        public override void _Ready()
        {
            var h5 = "api";

            server = GetParent<EmbeddedWebServerComponent>();
            server.AddResource($"/{h5}", this);
        }

        public void HandleRequest(Request request, Response response)
        {
            var path = request.uri.LocalPath.Substring(5);
            Debug.Log($"HandleRequest path:{path}");

            if (!apiMap.ContainsKey(path))
            {
                response.statusCode = 404;
                response.message = "Not Found";
                return;
            }

            var api = apiMap[path];
            var biz = api(request);
            response.statusCode = 200;
            response.message = "OK";
            response.headers.Add("Content-Type", "application/json");
            response.Write(biz);
        }

        public void AddPath(string path, Func<Request, string> func) => apiMap[path] = func;

    }
}
