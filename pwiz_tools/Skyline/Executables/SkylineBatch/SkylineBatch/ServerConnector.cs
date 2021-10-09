﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using FluentFTP;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharedBatch;
using SkylineBatch.Properties;

namespace SkylineBatch
{
    public class ServerConnector
    {
        private Dictionary<Server, List<ConnectedFileInfo>> _serverMap;
        private Dictionary<Server, Exception> _serverExceptions;

        public ServerConnector(params Server[] serverInfos)
        {
            _serverMap = new Dictionary<Server, List<ConnectedFileInfo>>();
            _serverExceptions = new Dictionary<Server, Exception>();
            foreach (var serverInfo in serverInfos)
            {
                if (!_serverMap.ContainsKey(serverInfo))
                {
                    _serverMap.Add(serverInfo, null);
                    _serverExceptions.Add(serverInfo, null);
                }
            }
        }

        public List<ConnectedFileInfo> GetFiles(DataServerInfo serverInfo, out Exception connectionException)
        {
            if (!_serverMap.ContainsKey(serverInfo) )
                throw new Exception("ServerConnector was not initialized with this server. No information for the server.");

            if (_serverMap[serverInfo] == null && _serverExceptions[serverInfo] == null)
                throw new Exception("ServerConnector was never started. No information for the server.");
            connectionException = _serverExceptions[serverInfo];
            var matchingFiles = new List<ConnectedFileInfo>();
            if (connectionException == null)
            {
                var namingRegex = new Regex(serverInfo.DataNamingPattern);
                foreach (var ftpFile in _serverMap[serverInfo])
                {
                    if (namingRegex.IsMatch(ftpFile.FileName))
                        matchingFiles.Add(ftpFile);
                }
                if (matchingFiles.Count == 0)
                {
                    connectionException = new ArgumentException(
                        string.Format(
                            Resources
                                .DataServerInfo_Validate_None_of_the_file_names_on_the_server_matched_the_regular_expression___0_,
                            serverInfo.DataNamingPattern) + Environment.NewLine +
                        Resources.DataServerInfo_Validate_Please_make_sure_your_regular_expression_is_correct_);
                }
            }

            if (connectionException != null)
                return null;
            return matchingFiles;
        }

        public void Add(Server server)
        {
            if (!_serverMap.ContainsKey(server))
            {
                _serverMap.Add(server, null);
                _serverExceptions.Add(server, null);
            }
        }

        public void Connect(OnPercentProgress doOnProgress, CancellationToken cancelToken, List<Server> servers = null)
        {
            if (servers == null)
                servers = _serverMap.Keys.ToList();
            if (servers.Count == 0) return;

            var serverCount = servers.Count;
            var downloadFinished = 0;
            var percentScale = 1.0 / serverCount * 100;
            foreach (var server in servers)
            {
                var percentDone = (int)(downloadFinished * percentScale);
                if (cancelToken.IsCancellationRequested)
                    break;
                if (_serverMap[server] != null || _serverExceptions[server] != null)
                    continue;
                var folder = ((DataServerInfo) server).Folder;
                if (server.URI.Host.Equals("panoramaweb.org"))
                {
                    Uri webdavUri;
                    var panoramaFolder = (Path.GetDirectoryName(server.URI.LocalPath) ?? string.Empty).Replace(@"\", "/");
                    JToken files;
                    Exception error;
                    if (panoramaFolder.StartsWith("/_webdav/"))
                    {
                        webdavUri = new Uri("https://panoramaweb.org" + panoramaFolder + "?method=json");
                        files = TryUri(webdavUri, server.Username, server.Password, cancelToken, out error);
                    }
                    else
                    {
                        panoramaFolder = "/_webdav" + panoramaFolder;
                        webdavUri = new Uri("https://panoramaweb.org" + panoramaFolder + "/%40files/RawFiles?method=json");
                        files = TryUri(webdavUri, server.Username, server.Password, cancelToken, out error);
                        if (files == null)
                        {
                            webdavUri = new Uri("https://panoramaweb.org" + panoramaFolder + "/%40files?method=json");
                            files = TryUri(webdavUri, server.Username, server.Password, cancelToken, out error);
                        }
                    }

                    var fileInfos = new List<ConnectedFileInfo>();
                    try
                    {
                        if (error != null) throw error;
                        var count = (double) (files.AsEnumerable().Count());
                        int i = 0;
                        foreach (var file in files)
                        {
                            doOnProgress((int) (i / count * percentScale) + percentDone,
                                (int) ((i + 1) / count * percentScale) + percentDone);
                            var pathOnServer = (string) file["id"];
                            var downloadUri = new Uri("https://panoramaweb.org" + pathOnServer);
                            var size = WebDownloadClient.GetSize(downloadUri, server.Username, server.Password,
                                cancelToken);
                            fileInfos.Add(new ConnectedFileInfo(Path.GetFileName(pathOnServer),
                                new Server(downloadUri, server.Username, server.Password, server.Encrypt), size,
                                folder));
                            i++;
                        }
                    }
                    catch (Exception e)
                    {
                        _serverExceptions[server] = e;
                    }

                    if (_serverExceptions[server] == null)
                        _serverMap[server] = fileInfos;
                }
                else
                {
                    doOnProgress(percentDone,
                        percentDone + (int)percentScale);
                    var serverFiles = new List<ConnectedFileInfo>();
                    var client = GetFtpClient(server);
                    var connectThread = new Thread(() =>
                    {
                        try
                        {
                            client.Connect();
                            foreach (FtpListItem item in client.GetListing(server.URI.LocalPath))
                            {
                                if (item.Type == FtpFileSystemObjectType.File)
                                {
                                    serverFiles.Add(new ConnectedFileInfo(item.Name, server, item.Size, folder));
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            _serverExceptions[server] = e;
                        }

                    });
                    connectThread.Start();
                    while (connectThread.IsAlive)
                    {
                        if (cancelToken.IsCancellationRequested)
                            connectThread.Abort();
                    }
                    client.Disconnect();

                    if (_serverExceptions[server] == null && serverFiles.Count == 0)
                    {
                        _serverExceptions[server] = new ArgumentException(string.Format(
                            Resources
                                .DataServerInfo_Validate_There_were_no_files_found_at__0___Make_sure_the_URL__username__and_password_are_correct_and_try_again_,
                            server));
                    }
                    else
                    {
                        _serverMap[server] = serverFiles;
                    }
                }
                downloadFinished++;
            }
            doOnProgress(100, 100);
        }

        private JToken TryUri(Uri uri, string username, string password, CancellationToken cancelToken, out Exception error)
        {
            error = null;
            if (cancelToken.IsCancellationRequested) return null;
            JToken files = null;
            try
            {
                var webClient = new WebPanoramaClient(new Uri("https://panoramaweb.org/"));
                var jsonAsString =
                    webClient.DownloadStringAsync(uri, username, password, cancelToken);
                if (cancelToken.IsCancellationRequested) return null;
                var panoramaJsonObject = JsonConvert.DeserializeObject<JObject>(jsonAsString);
                files = panoramaJsonObject["files"];
            }
            catch (Exception e)
            {
                error = e;
            }
            return files;
        }

        public void Reconnect(List<Server> servers, OnPercentProgress doOnProgress, CancellationToken cancelToken)
        {
            foreach (var serverInfo in servers)
            {
                _serverMap[serverInfo] = null;
                _serverExceptions[serverInfo] = null;
            }
            Connect(doOnProgress, cancelToken, servers);
        }

        public FtpClient GetFtpClient(Server serverInfo)
        {
            var client = new FtpClient(serverInfo.URI.Host);

            if (!string.IsNullOrEmpty(serverInfo.Password))
            {
                if (!string.IsNullOrEmpty(serverInfo.Username))
                    client.Credentials = new NetworkCredential(serverInfo.Username, serverInfo.Password);
                else
                    client.Credentials = new NetworkCredential("anonymous", serverInfo.Password);
            }

            return client;
        }

        public void Combine(ServerConnector other)
        {
            foreach (var server in other._serverMap.Keys)
            {
                if (!_serverMap.ContainsKey(server))
                {
                    _serverMap.Add(server, null);
                    _serverExceptions.Add(server, null);
                }
                _serverMap[server] = other._serverMap[server];
                _serverExceptions[server] = other._serverExceptions[server];
            }
        }

        public bool Contains(Server server)
        {
            return _serverMap.ContainsKey(server);
        }
    }
}
