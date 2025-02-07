using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord.WebSocket;
using Discord;
using Newtonsoft.Json;
using System.Text;

namespace PGT_Anti_Cheat_Bot
{
    public class DirectMessageHandler
    {
        private readonly DiscordSocketClient _client;
        private readonly WebhookHandler _webhookHandler;
        private readonly Dictionary<string, string> _linkedCodes;
        private readonly Dictionary<ulong, string> _activeProcesses;
        private readonly HashSet<string> _expiredCodes;
        private readonly List<string> legalSettings = new List<string>
        {
            "set by app", "2048", "60", "72", "90", "1024x1024", "2253", "brightness", "640x480", "24", "5mbps", "off", "Roomscale", "stationary", "2048x2048", "30", "48", "72hz", "60hz", "90hz", "default", "1280x720", "15mbps", "off", "low", "medium"
        };
        private readonly List<string> flaggedSettings = new List<string>
        {
            "set by user", "4096", "120", "144", "30", "10mbps", "2560x1440", "120hz", "144hz", "high", "unlimited", "off (by user)"
        };
        private readonly List<string> allowedAPKs = new List<string>
        {
            "com.oculus.browser", "com.android.settings", "com.meta.systemui", "com.AnotherAxiom.GorillaTag", "com.android.systemui", "com.google.android.gms", "com.oculus.vrshell", "com.meta.ovr.darwin", "com.oculus.updater", "com.google.android.apps.nexuslauncher", "com.android.contacts", "com.google.android.music", "com.google.android.dialer", "com.oculus.app", "com.meta.oass", "com.google.android.calendar", "com.android.messaging", "com.oculus.internal", "com.android.packageinstaller", "com.android.chrome", "com.android.camera", "com.google.android.youtube", "com.android.contacts", "com.google.android.search", "com.android.email", "com.android.sms", "com.oculus.gallery", "com.android.mms", "com.android.launcher3", "com.oculus.deviceinfo", "com.google.android.apps.photos", "com.android.documentsui", "com.android.inputmethod.latin", "com.android.settings.intelligence", "com.google.android.apps.maps", "com.android.phone", "com.android.calculator2", "com.android.bluetooth", "com.android.systemcore", "com.oculus.soundspace", "com.android.wallpaper.livepicker", "com.google.android.keep", "com.android.packageinstaller", "com.android.providers.contacts", "com.android.providers.telephony", "com.android.providers.downloads", "com.android.systemui.demo", "com.google.android.gms.unstable"
        };

        public string GetAuthorWithID(string authorName)
        {
            var guild = _client.GetGuild(1298785518133448775);
            if (guild == null)
            {
                Console.WriteLine("Guild not found.");
                return $"{authorName} ~ Null";
            }

            var foundMember = guild.Users.FirstOrDefault(m =>
                string.Equals(m.DisplayName, authorName, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(m.Username, authorName, StringComparison.OrdinalIgnoreCase));

            return foundMember != null ? $"{authorName} ~ {foundMember.Id}" : $"{authorName} ~ Null";
        }

        public DirectMessageHandler(DiscordSocketClient client, WebhookHandler webhookHandler)
        {
            _client = client;
            _webhookHandler = webhookHandler;
            _linkedCodes = new Dictionary<string, string>();
            _activeProcesses = new Dictionary<ulong, string>();
            _expiredCodes = new HashSet<string>();
        }

        public async Task HandleMessageReceived(SocketMessage message)
        {
            if (message.Author.IsBot) return;

            if (message.Channel is SocketDMChannel)
            {
                string authorName = message.Author.Username;
                ulong authorId = message.Author.Id;

                if (message.Content.StartsWith("Process.Start(") && message.Content.EndsWith(")"))
                {
                    var code = message.Content.Substring("Process.Start(".Length, message.Content.Length - "Process.Start()".Length);

                    if (_linkedCodes.ContainsKey(code) && !_expiredCodes.Contains(code) && _linkedCodes[code] == message.Author.Username)
                    {
                        _activeProcesses[authorId] = code;
                        await message.AddReactionAsync(new Emoji("✅"));
                    }
                    else
                    {
                        await message.AddReactionAsync(new Emoji("❎"));
                    }
                    return;
                }

                if (!_activeProcesses.ContainsKey(authorId))
                {
                    return;
                }

                var guild = _client.GetGuild(1298785518133448775);
                if (guild == null)
                {
                    Console.WriteLine("Guild not found.");
                    return;
                }

                var foundMember = guild.Users.FirstOrDefault(m =>
                    string.Equals(m.DisplayName, authorName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(m.Username, authorName, StringComparison.OrdinalIgnoreCase));

                string authorWithID = GetAuthorWithID(authorName);

                if (message.Attachments.Count > 0)
                {
                    foreach (var attachment in message.Attachments)
                    {
                        if (attachment.Filename.EndsWith(".txt"))
                        {
                            string content = await DownloadFileAsync(attachment.Url);
                            var (settings, flagged, illegal, apks, apps) = ProcessAdbOutput(content);

                            string formattedResults = FormatResults(settings, flagged, illegal, apks, apps);

                            _ = SendResultsToDiscord(settings, flagged, illegal, apks, apps, authorName);
                        }
                    }
                }

                if (message.Content.Contains("✅"))
                {
                    await _webhookHandler.SendWebhookAsync($@"
                    {{
                        ""content"": null,
                        ""embeds"": [
                            {{
                                ""title"": ""Rooted Check"",
                                ""color"": 15664132,
                                ""fields"": [
                                    {{
                                        ""name"": ""User failed the rooted check"",
                                        ""value"": ""Output:\nRooted outcome: ✅""
                                    }}
                                ],
                                ""author"": {{
                                    ""name"": ""{authorWithID}""
                                }}
                            }}
                        ],
                        ""attachments"": []
                    }}");
                }
                else if (message.Content.Contains("❎"))
                {
                    await _webhookHandler.SendWebhookAsync($@"
                    {{
                        ""content"": null,
                        ""embeds"": [
                            {{
                                ""title"": ""Rooted Check"",
                                ""color"": 5238532,
                                ""fields"": [
                                    {{
                                        ""name"": ""User passed the rooted check"",
                                        ""value"": ""Output:\nRooted outcome: ❎""
                                    }}
                                ],
                                ""author"": {{
                                    ""name"": ""{authorWithID}""
                                }}
                            }}
                        ],
                        ""attachments"": []
                    }}");
                }
            }
        }

        private (List<string> settings, List<string> flagged, List<string> illegal, List<string> apks, List<string> apps) ProcessAdbOutput(string adbOutput)
        {
            List<string> settings = new List<string>();
            List<string> flagged = new List<string>();
            List<string> illegal = new List<string>();
            List<string> apks = new List<string>();
            List<string> apps = new List<string>();

            string[] lines = adbOutput.Split('\n');
            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                if (line.Contains("package:"))
                {
                    string packageName = line.Replace("package:", "").Trim();
                    if (allowedAPKs.Contains(packageName))
                        apps.Add(packageName);
                    else
                        apks.Add(packageName);
                }
                else
                {
                    string[] parts = line.Split(':');
                    if (parts.Length < 2) continue;

                    string settingValue = parts[1].Trim().Trim('[', ']');
                    string settingName = parts[0].Trim();

                    if (settingName.Contains("debug.oculus.capture"))
                    {
                        if (settingName.Contains("height") || settingName.Contains("width"))
                        {
                            int captureSize;
                            if (int.TryParse(settingValue, out captureSize) && captureSize > 1024)
                                illegal.Add($"{settingName}: {settingValue} | ❌ (Capture size exceeds limit)");
                            else
                                settings.Add($"{settingName}: {settingValue} | ✅");
                        }
                        else if (settingName.Contains("fps"))
                        {
                            int captureFps;
                            if (int.TryParse(settingValue, out captureFps) && captureFps > 24)
                                illegal.Add($"{settingName}: {settingValue} | ❌ (Capture FPS exceeds limit)");
                            else
                                settings.Add($"{settingName}: {settingValue} | ✅");
                        }
                        else if (settingName.Contains("bitrate"))
                        {
                            int bitrate;
                            if (int.TryParse(settingValue, out bitrate) && bitrate > 5000000)
                                illegal.Add($"{settingName}: {settingValue} | ❌ (Bitrate exceeds limit)");
                            else
                                settings.Add($"{settingName}: {settingValue} | ✅");
                        }
                    }
                    else if (settingName.Contains("debug.oculus.experimentalEnabled") && settingValue != "0")
                        illegal.Add($"{settingName}: {settingValue} | ❌ (Experimental mode should be off)");
                    else if (settingName.Contains("debug.oculus.mtp") && settingValue != "0")
                        illegal.Add($"{settingName}: {settingValue} | ❌ (MTP should be off)");
                    else if (settingName.Contains("debug.oculus.fullRateCapture") && settingValue != "0")
                        illegal.Add($"{settingName}: {settingValue} | ❌ (Full rate capture should be off)");
                    else if (settingName.Contains("debug.oculus.cpuLevel") || settingName.Contains("debug.oculus.gpuLevel"))
                        illegal.Add($"{settingName}: {settingValue} | ❌ (CPU/GPU levels cannot be manually adjusted)");
                    else if (settingName.Contains("debug.oculus.texture"))
                    {
                        int textureSize;
                        if (int.TryParse(settingValue, out textureSize) && textureSize > 2048)
                            illegal.Add($"{settingName}: {settingValue} | ❌ (Texture size exceeds 2048)");
                        else
                            settings.Add($"{settingName}: {settingValue} | ✅");
                    }
                    else if (settingName.Contains("debug.oculus.refreshRate"))
                    {
                        int refreshRate;
                        if (int.TryParse(settingValue, out refreshRate) && (refreshRate < 60 || refreshRate > 90))
                            illegal.Add($"{settingName}: {settingValue} | ❌ (Refresh rate must be between 60Hz and 90Hz)");
                        else
                            settings.Add($"{settingName}: {settingValue} | ✅");
                    }
                    else if (settingName.Contains("debug.oculus.ffr") || settingName.Contains("debug.oculus.forceChroma"))
                        settings.Add($"{settingName}: {settingValue} | ✅");
                    else if (settingName.Contains("debug"))
                        flagged.Add($"{settingName}: {settingValue} | ⚠️ (Debug setting flagged)");
                }
            }
            return (settings, flagged, illegal, apks, apps);
        }

        private string FormatResults(List<string> settings, List<string> flagged, List<string> illegal, List<string> apks, List<string> apps)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("ADB Check Results");
            sb.AppendLine("\nSettings:");
            foreach (var setting in settings) sb.AppendLine("- " + setting);
            sb.AppendLine("\nFlagged:");
            foreach (var f in flagged) sb.AppendLine("- " + f);
            sb.AppendLine("\nIllegal:");
            foreach (var i in illegal) sb.AppendLine("- " + i);
            sb.AppendLine("\nAPKs:");
            foreach (var apk in apks) sb.AppendLine("- " + apk);
            sb.AppendLine("\nApps:");
            foreach (var app in apps) sb.AppendLine("- " + app);
            return sb.ToString();
        }

        private async Task SendResultsToDiscord(List<string> settings, List<string> flagged, List<string> illegal, List<string> apks, List<string> apps, string authName)
        {
            using (var client = new HttpClient())
            {
                var settingsChunks = SplitIntoChunks(settings, 10);
                var flaggedChunks = SplitIntoChunks(flagged, 10);
                var illegalChunks = SplitIntoChunks(illegal, 10);
                var apksChunks = SplitIntoChunks(apks, 10);
                var appsChunks = SplitIntoChunks(apps, 10);

                await SendChunkedEmbed(client, "✅ Legal Settings", authName, settingsChunks);
                await SendChunkedEmbed(client, "⚠️ Flagged Settings", authName, flaggedChunks);
                await SendChunkedEmbed(client, "❌ Illegal Settings", authName, illegalChunks);
                await SendChunkedEmbed(client, "📦 Unauthorized APKs", authName, apksChunks);
                await SendChunkedEmbed(client, "📱 Authorized Apps", authName, appsChunks);
            }
        }

        private List<List<string>> SplitIntoChunks(List<string> list, int chunkSize)
        {
            var chunks = new List<List<string>>();
            for (int i = 0; i < list.Count; i += chunkSize)
            {
                chunks.Add(list.Skip(i).Take(chunkSize).ToList());
            }
            return chunks;
        }

        private async Task SendChunkedEmbed(HttpClient client, string fieldName, string username, List<List<string>> chunks)
        {
            bool firstEmbedSent = false;

            for (int i = 0; i < chunks.Count; i++)
            {
                var chunk = chunks[i];

                var embed = new
                {
                    embeds = new[]
                    {
                new
                {
                    title = firstEmbedSent ? null : "🚨 ADB Check Results",
                    description = firstEmbedSent ? null : $"Here are the results of the ADB settings check for **{fieldName}**:",
                    color = 0xFF0000,
                    author = firstEmbedSent ? null : new
                    {
                        name = $"{username}'s Check Results"
                    },
                    fields = new[]
                    {
                        new
                        {
                            name = firstEmbedSent ? string.Empty : fieldName,
                            value = chunk.Any() ? string.Join("\n", chunk) : "None",
                            inline = false
                        }
                    }
                }
            }
                };

                var jsonPayload = JsonConvert.SerializeObject(embed);
                var discordWebhookUrl = "https://discord.com/api/webhooks/1336447868550123580/CYYC9lHo0ulbmHTmtRfUQxpvfW-oxjNBgcxfuatKYF0a3CSaLHy9f_wqpWGeXLrvi-hz";
                var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
                var response = await client.PostAsync(discordWebhookUrl, content);

                while (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    var retryAfter = int.Parse(response.Headers.RetryAfter?.Delta?.TotalSeconds.ToString() ?? "5");
                    await Task.Delay(retryAfter * 1500);
                    response = await client.PostAsync(discordWebhookUrl, content);
                }

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("Results successfully sent to Discord.");
                }
                else
                {
                    Console.WriteLine("Error sending results to Discord.");
                }

                if (!firstEmbedSent)
                {
                    firstEmbedSent = true;
                }
            }
        }

        private static async Task<string> DownloadFileAsync(string url)
        {
            using (HttpClient client = new HttpClient())
            {
                return await client.GetStringAsync(url);
            }
        }

        public void LinkProcessCode(string code, string username)
        {
            _linkedCodes[code] = username;
        }

        public void ExpireProcessCode(string code)
        {
            if (_linkedCodes.ContainsKey(code))
            {
                _expiredCodes.Add(code);

                var usersToRemove = _activeProcesses.Where(kvp => kvp.Value == code).Select(kvp => kvp.Key).ToList();
                foreach (var userId in usersToRemove)
                {
                    _activeProcesses.Remove(userId);
                }
            }
        }
    }
}