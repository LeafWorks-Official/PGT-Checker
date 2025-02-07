using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.WebSocket;

namespace PGT_Anti_Cheat_Bot
{
    public class SlashCommandHandler
    {
        private readonly Dictionary<ulong, (string code, DateTime timestamp)> _userCodes = new();
        private readonly DirectMessageHandler _directMessageHandler;

        public SlashCommandHandler(DirectMessageHandler directMessageHandler)
        {
            _directMessageHandler = directMessageHandler;
        }

        public async Task HandleGetProcessCommand(SocketSlashCommand command)
        {
            ulong userId = command.User.Id;

            if (_userCodes.ContainsKey(userId))
            {
                var (storedCode, timestamp) = _userCodes[userId];
                if (DateTime.UtcNow - timestamp < TimeSpan.FromMinutes(15))
                {
                    await command.RespondAsync($"You already have a Process Code: {storedCode}. It is valid for another {15 - (DateTime.UtcNow - timestamp).Minutes} minutes.", ephemeral: true);
                    return;
                }
            }

            string newCode = GenerateRandomCode();

            _userCodes[userId] = (newCode, DateTime.UtcNow);

            await command.RespondAsync($"Here is your Process Code: {newCode}. It will expire in 15 minutes.", ephemeral: true);

            _directMessageHandler.LinkProcessCode(newCode, command.User.Username);

            _ = ResetUserCodeAfterTimeout(userId);
        }

        private string GenerateRandomCode()
        {
            Random rand = new Random();
            StringBuilder code = new StringBuilder();
            for (int i = 0; i < 15; i++)
            {
                code.Append(rand.Next(0, 10));
            }
            return code.ToString();
        }

        private async Task ResetUserCodeAfterTimeout(ulong userId)
        {
            await Task.Delay(TimeSpan.FromMinutes(15));

            if (_userCodes.ContainsKey(userId))
            {
                _userCodes.Remove(userId);
            }
        }
    }
}
