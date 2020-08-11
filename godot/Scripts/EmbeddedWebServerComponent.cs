using System.Collections;
using System.Net.Sockets;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using System;
using Godot;

namespace Janphe
{


    public class EmbeddedWebServerComponent : Node
    {
        public bool startOnAwake = true;
        public int port = 8079;
        public int workerThreads = 2;
        public bool processRequestsInMainThread = true;
        public bool logRequests = true;

        WebServer server;
        Dictionary<string, IWebResource> resources = new Dictionary<string, IWebResource>();

        public override void _Ready()
        {
            //if (processRequestsInMainThread)
            //    Application.runInBackground = true;
            server = new WebServer(port, workerThreads, processRequestsInMainThread);
            server.logRequests = logRequests;
            server.HandleRequest += HandleRequest;
            server.OnLog(d => Debug.Log(d));
            if (startOnAwake)
            {
                server.Start();
            }
        }

        public override void _ExitTree()
        {
            server.Dispose();
        }

        public override void _Process(float delta)
        {
            if (server.processRequestsInMainThread)
            {
                server.ProcessRequests();
            }
        }



        void HandleRequest(Request request, Response response)
        {
            // get first part of the directory
            var folderRoot = Helper.GetFolderRoot(request.uri.LocalPath);
            folderRoot = folderRoot.replace('\\', '/');

            var keys = resources.Keys;

            var match = false;
            foreach (var k in keys)
            {
                if (folderRoot.StartsWith(k))
                {
                    try
                    {
                        resources[k].HandleRequest(request, response);
                    }
                    catch (Exception e)
                    {
                        response.statusCode = 500;
                        response.Write(e.Message);
                    }

                    match = true;
                    break;
                }
            }
            if (!match)
            {
                response.statusCode = 404;
                response.message = "Not Found.";
                response.Write($"LocalPath:{request.uri.LocalPath} not found.");
            }
        }

        public void AddResource(string path, IWebResource resource)
        {
            resources[path] = resource;
        }

    }



}
