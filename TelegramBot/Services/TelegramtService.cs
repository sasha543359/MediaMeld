using Microsoft.Extensions.Hosting;
using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using VideoProcessing;
using VideoProcessing.Services;

namespace TelegramBot.Services
{
    public class TelegramService : BackgroundService
    {
        private readonly TelegramBotClient _botClient;
        private readonly IVideoProcessingService _videoService;

        public TelegramService(IVideoProcessingService videoService)
        {
            _videoService = videoService;
            _botClient = new TelegramBotClient("7946448900:AAGJZqNHXqTQ44XDW38zhdhHTmkkyu4Tjcg");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Начало получения сообщений
            _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync, cancellationToken: stoppingToken);

            // Бесконечный цикл для поддержания службы
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(1000, stoppingToken); // Пауза между проверками
            }
        }

        private async Task HandleUpdateAsync(ITelegramBotClient botClient, Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
        {
            if (update.Type == UpdateType.Message && update.Message.Text != null)
            {
                var messageText = update.Message.Text;

                try
                {
                    // Получаем ссылку для скачивания видео
                    var downloadUrl = await _videoService.GetTikTokDownloadUrl(messageText);

                    Guid guid = Guid.NewGuid();

                    // Путь, куда будет скачиваться видео
                    var savePath = $"C:\\Users\\sasha\\Desktop\\tiktokvideos\\{guid}.mp4";

                    // Скачиваем видео
                    await _videoService.DownloadVideo(downloadUrl, savePath);

                    Thread.Sleep(2000);

                    // Путь к скачанному видео
                    var inputVideoPath = savePath;

                    // Путь для сохранения обработанного видео
                    var outputVideoPath = $"C:\\Users\\sasha\\Desktop\\readyvideo\\{guid}.mp4";

                    // Обрабатываем видео
                    _videoService.ProcessVideo(inputVideoPath, outputVideoPath);

                    Thread.Sleep(2000);

                    ProcessStartInfo startInfo = new ProcessStartInfo
                    {
                        FileName = @"C:\Users\sasha\Desktop\PapichProject\SeleniumProject\bin\Debug\net8.0\SeleniumProject.exe",
                        Arguments = $"{outputVideoPath}"
                    };

                    Console.WriteLine($"Передаем аргумент: {startInfo.Arguments}");

                    using (Process process = Process.Start(startInfo))
                    {
                        process.WaitForExit();
                    }

                    // Отправляем сообщение пользователю
                    await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "Видео успешно обработано и опубликовано!", cancellationToken: cancellationToken);
                }
                catch (Exception ex)
                {
                    // Ловим возможные ошибки
                    await _botClient.SendTextMessageAsync(update.Message.Chat.Id, $"Произошла ошибка: {ex.Message}", cancellationToken: cancellationToken);
                }
            }
        }

        private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Ошибка: {exception.Message}");
            return Task.CompletedTask;
        }
    }
}
