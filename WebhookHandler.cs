using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PGT_Anti_Cheat_Bot
{
    public class WebhookHandler
    {
        private const string WebhookUrl = "https://discord.com/api/webhooks/1336447868550123580/CYYC9lHo0ulbmHTmtRfUQxpvfW-oxjNBgcxfuatKYF0a3CSaLHy9f_wqpWGeXLrvi-hz";

        public async Task SendWebhookAsync(string jsonPayload)
        {
            using HttpClient client = new HttpClient();
            StringContent content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");
            await client.PostAsync(WebhookUrl, content);
        }
    }
}
