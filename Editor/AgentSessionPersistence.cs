using System;
using System.Collections.Generic;
using System.IO;
using AgentClientProtocol;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace UnityAgentClient
{
    /// <summary>
    /// Persists the last ACP session id and UI history across Unity domain reloads.
    /// This does NOT attempt to persist the live JSON-RPC connection; it enables reconnect + session/load.
    /// </summary>
    internal static class AgentSessionPersistence
    {
        const int CurrentVersion = 1;

        [Serializable]
        sealed class AgentSessionStateFile
        {
            public int Version = CurrentVersion;
            public string SessionId;
            public string MessagesJson;
            public string LastUpdatedUtc;
        }

        static readonly object fileLock = new();
        static AgentSessionStateFile cached;

        public static string GetSessionId()
        {
            return LoadState().SessionId;
        }

        public static void SetSessionId(string sessionId)
        {
            if (string.IsNullOrWhiteSpace(sessionId)) return;

            var s = LoadState();
            s.Version = CurrentVersion;
            s.SessionId = sessionId;
            s.LastUpdatedUtc = DateTime.UtcNow.ToString("o");
            SaveState(s);
        }

        public static void SaveSnapshot(string sessionId, IReadOnlyList<SessionUpdate> messages)
        {
            var s = LoadState();
            s.Version = CurrentVersion;
            if (!string.IsNullOrWhiteSpace(sessionId))
            {
                s.SessionId = sessionId;
            }
            s.MessagesJson = SerializeMessages(messages);
            s.LastUpdatedUtc = DateTime.UtcNow.ToString("o");
            SaveState(s);
        }

        public static List<SessionUpdate> LoadMessages()
        {
            var json = LoadState().MessagesJson;
            if (string.IsNullOrWhiteSpace(json))
            {
                return new List<SessionUpdate>();
            }

            try
            {
                var token = JToken.Parse(json);
                var list = token.ToObject<List<SessionUpdate>>(AcpJson.Serializer);
                return list ?? new List<SessionUpdate>();
            }
            catch
            {
                // If parsing fails (schema change/corruption), fail soft by returning empty history.
                return new List<SessionUpdate>();
            }
        }

        public static void ClearSessionId()
        {
            var s = LoadState();
            s.SessionId = null;
            s.LastUpdatedUtc = DateTime.UtcNow.ToString("o");
            SaveState(s);
        }

        static string SerializeMessages(IReadOnlyList<SessionUpdate> messages)
        {
            try
            {
                return JToken.FromObject(messages ?? Array.Empty<SessionUpdate>(), AcpJson.Serializer)
                    .ToString(Newtonsoft.Json.Formatting.None);
            }
            catch
            {
                return "[]";
            }
        }

        static string GetStatePath()
        {
            var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? ".";
            return Path.Combine(projectRoot, "Library", "UnityAgentClient", "AgentSessionState.json");
        }

        static AgentSessionStateFile LoadState()
        {
            lock (fileLock)
            {
                if (cached != null) return cached;

                var path = GetStatePath();
                try
                {
                    if (!File.Exists(path))
                    {
                        cached = new AgentSessionStateFile { Version = CurrentVersion };
                        return cached;
                    }

                    var json = File.ReadAllText(path);
                    if (string.IsNullOrWhiteSpace(json))
                    {
                        cached = new AgentSessionStateFile { Version = CurrentVersion };
                        return cached;
                    }

                    var token = JToken.Parse(json);
                    cached = token.ToObject<AgentSessionStateFile>() ?? new AgentSessionStateFile { Version = CurrentVersion };
                    if (cached.Version <= 0) cached.Version = CurrentVersion;
                    return cached;
                }
                catch
                {
                    cached = new AgentSessionStateFile { Version = CurrentVersion };
                    return cached;
                }
            }
        }

        static void SaveState(AgentSessionStateFile state)
        {
            lock (fileLock)
            {
                cached = state ?? new AgentSessionStateFile { Version = CurrentVersion };

                try
                {
                    var path = GetStatePath();
                    Directory.CreateDirectory(Path.GetDirectoryName(path));
                    var json = JToken.FromObject(cached).ToString(Newtonsoft.Json.Formatting.Indented);
                    File.WriteAllText(path, json);
                }
                catch
                {
                    // ignore - persistence should never block editor actions or domain reload
                }
            }
        }
    }
}


