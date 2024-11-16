using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
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

        Host.CreateDefaultBuilder(args)
            .UseWindowsService() // Настройка как Windows-служба
            .ConfigureServices((hostContext, services) =>
            {
                services.AddSingleton<IVideoProcessingService, VideoProcessingService>();
                services.AddHostedService<TelegramService>(); 
            })
            .Build()
            .Run();
    }
}