using System.Diagnostics;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using VideoProcessing;

namespace TelegramBot.Services;

public class TelegramBotService
{
    private readonly string _api_key_nowpayments;

    private readonly TelegramBotClient _botClient;
    private readonly IVideoProcessingService _videoService;

    private readonly Dictionary<long, string> userStates = new Dictionary<long, string>();

    public TelegramBotService(string telegramToken, IVideoProcessingService videoService)
    {
        _botClient = new TelegramBotClient(telegramToken);
        _videoService = videoService;
    }

    public async Task HandleUpdateAsync(ITelegramBotClient botClient, Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
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
                var inputVideoPath = $"C:\\Users\\sasha\\Desktop\\tiktokvideos\\{guid}.mp4";

                // Путь для сохранения обработанного видео
                var outputVideoPath = $"C:\\Users\\sasha\\Desktop\\readyvideo\\{guid}.mp4";

                // Обрабатываем видео
                _videoService.ProcessVideo(inputVideoPath, outputVideoPath);

                Thread.Sleep(2000);

                // Запускаем процесс Selenium для загрузки видео в Instagram
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = @"C:\Users\sasha\Desktop\PapichProject\SeleniumProject\bin\Debug\net8.0\SeleniumProject.exe",  // Укажите реальный путь к исполняемому файлу SeleniumProject
                    Arguments = $"{outputVideoPath}"  // Передаем только обработанное видео
                };

                using (Process process = Process.Start(startInfo))
                {
                    process.WaitForExit();  // Ждем завершения процесса
                }

                // Отправляем сообщение пользователю, что видео обработано и опубликовано
                await _botClient.SendTextMessageAsync(update.Message.Chat.Id, "Видео обработано и опубликовано!", cancellationToken: cancellationToken);
            }
            catch (Exception ex)
            {
                // Ловим возможные ошибки
                await _botClient.SendTextMessageAsync(update.Message.Chat.Id, $"Произошла ошибка: {ex.Message}", cancellationToken: cancellationToken);
            }
        }
    }


    public void Start()
    {
        _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync);
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }
}