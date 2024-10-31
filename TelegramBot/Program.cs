using TelegramBot.Services;
using VideoProcessing;
using VideoProcessing.Services;

namespace TelegramBot;

internal class Program
{
    static async Task Main(string[] args)
    {
        // Ваш API-ключ для Telegram бота
        string token = "7946448900:AAGJZqNHXqTQ44XDW38zhdhHTmkkyu4Tjcg";

        IVideoProcessingService videoService = new VideoProcessingService();

        var telegramBotService = new TelegramBotService(token, videoService);

        telegramBotService.Start();

        Console.WriteLine("Бот запущен. Нажмите Enter для завершения.");
        Console.ReadLine();
    }
}