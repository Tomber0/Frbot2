using System;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Extensions.Polling;
using Telegram.Bot.Args;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.IO;
using FileIO = System.IO.File;
using static System.Net.Mime.MediaTypeNames;
using System.Reflection;
using System.Threading.Tasks;
using System.Reflection.Metadata;
using System.Diagnostics;
using System.Linq;
using static Frbot2.Program;

namespace Frbot2
{
    static class Command
    {
        public enum Commands
        {
            help,
            start
        }
        public static string GetCommand(Commands command)
        {
            return $"/{command.ToString()}";
        }
    }

    public class ChatLibrary
    {
        public List<CustomChat> Chats = new List<CustomChat>();

        public void AddNewChat(Chat chat)
        {
            if (!Chats.Any(e => e.Chat == chat))
            {
                Chats.Add(new CustomChat(chat));
            }
        }
    }
    public class CustomChat 
    {

        public Chat Chat;
        public DateTime LastTime;

        public CustomChat(Chat chat) 
        {
            Chat = chat;
        }
        public void UpdatelastTime() 
        {
            LastTime = DateTime.UtcNow;
        } 

    }
    public class TelegramBot
    {
        private TelegramBotClient _telegramBotClient;
        public TelegramBotClient TelegramBotClient { get { return _telegramBotClient; } }
        public string VideoLinkId = "BAACAgIAAxkBAAMQYbvnAAE6XE1BLyrzmcgkD6-twnklAAIwEgACfHNZSahmykClLfwCIwQ";
        private string _tokenFileName;
        public CancellationTokenSource Cancellation;
        private ChatLibrary _chats;
        private string _token;
        private MessageHandler _messageHandler;
        public TelegramBot(string tokenFileName, ChatLibrary chatLibrary, MessageHandler messageHandler)
        {
            _messageHandler = messageHandler;
            _tokenFileName = tokenFileName;
            _chats = chatLibrary;
        }
        public void StartBot()
        {
            var cts = new CancellationTokenSource();
            var token = cts.Token;
            Cancellation = cts;
            var reciveOptions = new ReceiverOptions() { AllowedUpdates = { } };
            TokensHandler tokens = new TokensHandler();
            _token = tokens.GetToken(_tokenFileName);
            _telegramBotClient = new TelegramBotClient(_token);
            _telegramBotClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, reciveOptions, token);
        }
        public void AddChatToLib(Chat chat)
        {
            _chats.AddNewChat(chat);
        }
        async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
        {
            if (update.Message is Message message)
            {
                await Task.Run(() => AddChatToLib(message.Chat));
                string response = await Task.Run(() => _messageHandler.OnMessage(message));
                await botClient.SendTextMessageAsync(message.Chat, $"{response}");
            }
        }
        async Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            if (exception is ApiRequestException apiRequestException)
            {
                await botClient.SendTextMessageAsync(123, apiRequestException.ToString());
            }
        }
        public async void SendFriday()
        {
            await SendFridayMessage(TelegramBotClient, VideoLinkId);

        }
        public async Task SendFridayMessage(ITelegramBotClient botClient, string videoId)
        {
            foreach (var chat in _chats.Chats)
            {
                if (chat.LastTime.Day != DateTime.UtcNow.Day)
                {
                    chat.LastTime = DateTime.UtcNow;
                    await botClient.SendVideoAsync(chat.Chat, video: $"{videoId}");
                }
            }
        }
    }

    public class MessageHandler
    {
        private string _logger;
        public MessageHandler()
        {

        }
        private string _response = "";
        public string OnMessage(Message message)
        {

            Console.WriteLine("Текстовое сообщение боту");

            Console.WriteLine($"Текст сообщения:{message.Text}");
            switch (message.Text)
            {
                case "/help":
                    _response = $"response, help";
                    break;

                case "/start":
                    _response = $"response, start";
                    break;

                default:
                    _response = $"";

                    break;
            }
            return _response;
        }
    }
    class Program
    {
        static void Main(string[] args)
        {
            ChatLibrary chLib = new ChatLibrary();
            MessageHandler mH = new MessageHandler();
            Friday friday = new Friday();
            TelegramBot bot = new TelegramBot("Token2", chLib, mH);
            friday.OnSendMessage += bot.SendFriday;
            bot.StartBot();
            Console.WriteLine("Бот запущен!");
            friday.Start(1);
            Console.ReadLine();
            bot.Cancellation.Cancel();
            Console.WriteLine("Бот остановлен!");



        }

        public class TokensHandler
        {
            public string Token;
            public string GetToken(string fileName)
            {
                string result;
                var assembly = Assembly.GetExecutingAssembly();
                var className = typeof(TokensHandler);
                var nameOfNamespace = className.Namespace;
                var resourseName = $"{nameOfNamespace}.{fileName}.json";
                using (Stream stream = assembly.GetManifestResourceStream(resourseName))
                using (StreamReader reader = new StreamReader(stream))
                {
                    result = reader.ReadToEnd();
                }
                //string jsonFile = FileIO.ReadAllText(result);
                var token = JsonConvert.DeserializeObject<TokensHandler>(result);
                return token.Token.ToString();
            }
        }
    }

    public class Friday
    {
        public delegate void SendMessageHandler();
        public event SendMessageHandler OnSendMessage;
        Timer _timer;
        private int _timeD = 1000;
        private bool _isFriday; //is friday - today?
        private int _lastDay; //last day when friday was
        public void Process(object obj)
        {
            Console.WriteLine("fridayT");
            Stopwatch watch = new Stopwatch();

            watch.Start();

            if (IsTodayFriday()
                )
            {
                Console.WriteLine("friday");
                SendMessage();
                ChangeLastDay();
            }
            _timer.Change(Math.Max(0, _timeD - watch.ElapsedMilliseconds), Timeout.Infinite);

        }
        public void Start(int hoursDelta)
        {
            //delta = hours
            Console.WriteLine("friday start");

            int timeMs = hoursDelta * 1 * 1 * 1000;
            _timeD = timeMs;
            TimerCallback tm = new TimerCallback(Process);
            _timer = new Timer(tm, null, 1000 * 5, Timeout.Infinite);

        }
        private void ChangeLastDay()
        {
            _lastDay = DateTime.UtcNow.Day;
            Console.WriteLine("friday day");

        }
        public void SendMessage()
        {
            OnSendMessage.Invoke();
        }

        public bool IsTodayFriday()
        {
            return (DateTime.UtcNow.DayOfWeek == DayOfWeek.Friday);
        }
    }

    class Counter
    {
        private DateTime _currentTime = new DateTime();
        private DateTime _targetTime = new DateTime();

        public TimeSpan Time { get; private set; }


        public Counter()
        {
            UpdateTime();
        }
        private void SetCurrentTime()
        {
            _currentTime = DateTime.Now;
        }
        public void SetDefaultTargetTime()
        {
            _targetTime = new DateTime(2021, 10, 19);
        }
        public TimeSpan GetTimeToHome()
        {
            return _targetTime.Subtract(_currentTime);
        }
        public void UpdateTime()
        {
            SetCurrentTime();
            Time = GetTimeToHome();
        }
        public int GetSecondsLeft()
        {
            int hoursLeft = (Time.Days * 24) + Time.Hours;
            int minutesLeft = hoursLeft * 60 + Time.Minutes;
            int secondsLeft = minutesLeft * 60 + Time.Seconds;
            return secondsLeft;
        }
        public override string ToString()
        {
            string fullTime = $"Дней осталось: {Time.Days}\nЧасов осталось: {Time.Hours}\nМинут осталось: {Time.Minutes}\nСекунд осталось: {Time.Seconds}";
            return fullTime;
        }
    }
}
