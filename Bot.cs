using System;
using System.Globalization;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace Radarsbutt.Client
{
    public class Bot
    {
        private readonly Tts _tts;
        private Bot(Tts tts)
        {
            _tts = tts;
            var twitchUsername = Environment.GetEnvironmentVariable("TWITCH_BOT_USERNAME");
            var twitchBotOauth = Environment.GetEnvironmentVariable("TWITCH_BOT_OAUTH");
            if (string.IsNullOrEmpty(twitchUsername) || string.IsNullOrEmpty(twitchBotOauth))
            {
                throw new ArgumentException("Twitch credentials are missing.");
            }

            var credentials = new ConnectionCredentials(twitchUsername, twitchBotOauth);
            var clientOptions = new ClientOptions
            {
                MessagesAllowedInPeriod = 750,
                ThrottlingPeriod = TimeSpan.FromSeconds(30)
            };

            var customClient = new WebSocketClient(clientOptions);
            var client = new TwitchClient(customClient);
            var channel = Environment.GetEnvironmentVariable("TWITCH_BOT_PRIMARY_CHANNEL");
            if (string.IsNullOrEmpty(channel))
            {
                throw new ArgumentException("Channel for bot to join is missing.");
            }
            client.Initialize(credentials, channel);

            client.OnLog += Client_OnLog;
            client.OnJoinedChannel += Client_OnJoinedChannel;
            client.OnMessageReceived += Client_OnMessageReceived;
            client.OnConnected += Client_OnConnected;
            client.OnChatCommandReceived += Client_OnChatCommandReceived;

            client.Connect();
        }

        public static async Task<Bot> Init()
        {
            return new Bot(await Tts.Init());
        }

        private static void Client_OnLog(object sender, OnLogArgs e)
        {
            Console.WriteLine($"{e.DateTime.ToString(CultureInfo.InvariantCulture)}: {e.BotUsername} - {e.Data}");
        }

        private static void Client_OnConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine($"Connected to {e.AutoJoinChannel}");
        }

        private static void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            Console.WriteLine($"Channel {e.Channel} joined");
        }

        private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs e)
        {
            // Console.WriteLine($"Received custom reward ID: {e.ChatMessage.CustomRewardId}");
        }

        private async void Client_OnChatCommandReceived(object sender, OnChatCommandReceivedArgs e)
        {
            var command = e.Command.CommandText;
            switch (command)
            {
                case "tts":
                    await _tts.ProcessTts(e.Command.ArgumentsAsList);
                    break;
            }
        }
    }
}
