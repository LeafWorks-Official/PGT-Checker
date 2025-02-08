using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Discord.Interactions;
using System.Net.Http;
using System.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using PGT_Anti_Cheat_Bot;
using System.IO;

class Program
{
    private readonly DiscordSocketClient _client = new DiscordSocketClient(new DiscordSocketConfig
    {
        GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent | GatewayIntents.DirectMessages | GatewayIntents.GuildMembers
    });

    private readonly SlashCommandHandler _slashCommandHandler;
    private readonly DirectMessageHandler _directMessageHandler;
    private readonly WebhookHandler _webhookHandler = new WebhookHandler();

    public Program()
    {
        _directMessageHandler = new DirectMessageHandler(_client, _webhookHandler);
        _slashCommandHandler = new SlashCommandHandler(_directMessageHandler);
    }

    static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();

    public async Task RunBotAsync()
    {
        try
        {
            Debug.WriteLine("Bot initialization started.");

            _client.Log += Log;
            _client.MessageReceived += async message => await _directMessageHandler.HandleMessageReceived(message);
            _client.InteractionCreated += InteractionCreated;

            Debug.WriteLine("Event handlers set.");

            var token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");

            if (string.IsNullOrEmpty(token))
            {
                Debug.WriteLine("Error: Bot token is missing. Set it as an environment variable.");
                return;
            }

            Debug.WriteLine("Token retrieved.");

            await _client.LoginAsync(TokenType.Bot, token);
            await _client.StartAsync();

            Debug.WriteLine("Bot started.");

            await Task.Delay(5000);

            var guild = _client.GetGuild(1298785518133448775);
            if (guild != null)
            {
                Debug.WriteLine("Downloading all users...");
                await guild.DownloadUsersAsync();
                Debug.WriteLine("User download complete.");
            }

            await RegisterSlashCommands();

            await Task.Delay(-1);
        }
        catch (Exception ex)
        {
            Debug.WriteLine("Error: " + ex.Message);
            Debug.WriteLine(ex.StackTrace);
            Environment.Exit(1);
        }
    }

    private async Task RegisterSlashCommands()
    {
        var guild = _client.GetGuild(1298785518133448775);
        if (guild != null)
        {
            var command = new SlashCommandBuilder()
                .WithName("getprocess")
                .WithDescription("Generates your process key.")
                .Build();

            await guild.CreateApplicationCommandAsync(command);
        }
    }

    private async Task InteractionCreated(SocketInteraction interaction)
    {
        if (interaction is not SocketSlashCommand slashCommand) return;

        if (slashCommand.Data.Name == "getprocess")
        {
            await _slashCommandHandler.HandleGetProcessCommand(slashCommand);
        }
    }

    private Task Log(LogMessage msg)
    {
        Console.WriteLine(msg);
        return Task.CompletedTask;
    }
}