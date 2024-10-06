using System;
using System.IO;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Args;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using FFMpegCore;
using PaymentService_NOWPaymentsService_.Services;
using PaymentService_NOWPaymentsService_;
using TelegramBot.Services;
using VideoProcessing.Services;
using VideoProcessing;
using Telegram.Bot.Types.InputFiles;



namespace TelegramBot;

internal class Program
{
    static string? mainVideoPath = null;  // Путь к основному видео
    static string? chromaKeyVideoPath = null;  // Путь к видео с хромакеем

    static int videoCounter = 1; // Номер видеофайла


    static async Task Main(string[] args)
    {
        // Ваш API-ключ для Telegram бота
        string token = "7946448900:AAGJZqNHXqTQ44XDW38zhdhHTmkkyu4Tjcg";

        // Создаём экземпляры сервисов для обработки видео и платежей
        IVideoProcessingService videoService = new VideoProcessingService();  // Реализация вашего сервиса видеообработки
        INowPaymentsService paymentService = new NowPaymentsService();        // Реализация вашего платежного сервиса

        // Создаём экземпляр TelegramBotService и передаём в него зависимости
        var telegramBotService = new TelegramBotService(token, videoService, paymentService);

        // Запускаем бота
        telegramBotService.Start();

        Console.WriteLine("Бот запущен. Нажмите Enter для завершения.");
        Console.ReadLine(); // Ожидание завершения программы
    }

    // Метод для обработки обновлений от Telegram
    // Метод для обработки обновлений от Telegram
    static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is Message message)
        {
            // Если это текстовое сообщение и команда /start
            if (message.Text == "/start")
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Привет! Пожалуйста, скинь основное видео для обработки.");
            }
            // Получение основного видео
            else if (message.Video != null && mainVideoPath == null)
            {
                var fileId = message.Video.FileId;
                var fileInfo = await botClient.GetFileAsync(fileId, cancellationToken);

                mainVideoPath = Path.Combine(@"C:\Users\sasha\Desktop", $"{message.Chat.Id}_main_video.mp4");
                using (var saveStream = new FileStream(mainVideoPath, FileMode.Create))
                {
                    await botClient.DownloadFileAsync(fileInfo.FilePath, saveStream, cancellationToken);
                }

                Console.WriteLine($"Основное видео сохранено в: {mainVideoPath}");
                await botClient.SendTextMessageAsync(message.Chat.Id, "Основное видео получено. Пожалуйста, скинь видео с зелёным фоном.");
            }
            // Получение видео с хромакеем
            else if (message.Video != null && mainVideoPath != null && chromaKeyVideoPath == null)
            {
                var fileId = message.Video.FileId;
                var fileInfo = await botClient.GetFileAsync(fileId, cancellationToken);

                chromaKeyVideoPath = Path.Combine(@"C:\Users\sasha\Desktop", $"{message.Chat.Id}_chroma_video.mp4");
                using (var saveStream = new FileStream(chromaKeyVideoPath, FileMode.Create))
                {
                    await botClient.DownloadFileAsync(fileInfo.FilePath, saveStream, cancellationToken);
                }

                Console.WriteLine($"Видео с хромакеем сохранено в: {chromaKeyVideoPath}");
                await botClient.SendTextMessageAsync(message.Chat.Id, "Видео с хромакеем получено. Начинаю обработку...");

                // Обработка основного видео
                string processedVideoPath = Path.Combine(Path.GetTempPath(), $"processed_video_{videoCounter}.mp4");
                ProcessVideo(mainVideoPath, processedVideoPath);

                // Проверка, существуют ли файлы
                if (!System.IO.File.Exists(mainVideoPath))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Ошибка: основное видео не найдено.");
                    return;
                }

                if (!System.IO.File.Exists(chromaKeyVideoPath))
                {
                    await botClient.SendTextMessageAsync(message.Chat.Id, "Ошибка: видео с хромакеем не найдено.");
                    return;
                }

                // Добавление видео с хромакеем
                string finalVideoPath = Path.Combine(Path.GetTempPath(), $"final_video_{videoCounter}.mp4");
                AddVideoWithChromaKey(processedVideoPath, chromaKeyVideoPath, finalVideoPath, 518, 1556); // Твои параметры

                await botClient.SendTextMessageAsync(message.Chat.Id, "Видео успешно обработано. Отправляю результат...");

                // Отправляем финальное видео обратно пользователю
                using (var stream = new FileStream(finalVideoPath, FileMode.Open, FileAccess.Read))
                {
                    var inputOnlineFile = new InputOnlineFile(stream, $"final_video_{videoCounter}.mp4");
                    await botClient.SendVideoAsync(
                        chatId: message.Chat.Id,
                        video: inputOnlineFile,
                        caption: "Вот ваше готовое видео!"
                    );
                }

                // Увеличиваем счётчик для следующего видео
                videoCounter++;

                // Очищаем переменные после обработки
                mainVideoPath = null;
                chromaKeyVideoPath = null;
            }
            else
            {
                await botClient.SendTextMessageAsync(message.Chat.Id, "Пожалуйста, отправьте видео в формате MP4.");
            }
        }
    }
    static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(exception.Message);
        return Task.CompletedTask;
    }

    // Логика из твоего метода ProcessVideo
    static void ProcessVideo(string inputVideoPath, string outputVideoPath)
    {
        // Устанавливаем путь к бинарникам FFmpeg
        GlobalFFOptions.Configure(options => options.BinaryFolder = @"C:\Users\sasha\Desktop\ffmpeg-7.1-essentials_build\bin");

        try
        {
            // Удаление метаданных и изменение разрешения видео на 9:16 с 60 FPS
            FFMpegArguments
                .FromFileInput(inputVideoPath)  // Входной файл
                .OutputToFile(outputVideoPath, true, options => options
                    .WithVideoFilters(filter => filter
                        .Scale(1080, 1920)  // Масштабирование до 1080x1920 (9:16)
                    )
                    .WithFramerate(60)  // Установка 60 FPS
                    .WithCustomArgument("-map_metadata -1")  // Удаление метаданных
                )
                .ProcessSynchronously();  // Запуск синхронного процесса

            Console.WriteLine("Видео успешно обработано и сохранено в формате 9:16 с 60 FPS.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке видео: {ex.Message}");
        }
    }

    // Логика из твоего метода AddVideoWithChromaKey
    static void AddVideoWithChromaKey(string backgroundVideoPath, string overlayVideoPath, string outputPath, int x, int y)
    {
        try
        { 
            // Получаем длительность основного видео
            TimeSpan backgroundDuration = GetVideoDuration(backgroundVideoPath);
            Console.WriteLine($"Длительность основного видео: {backgroundDuration.TotalSeconds} секунд");

            // Получаем длительность видео с хромакеем
            TimeSpan overlayDuration = GetVideoDuration(overlayVideoPath);
            Console.WriteLine($"Длительность видео с хромакеем: {overlayDuration.TotalSeconds} секунд");

            // Рассчитываем, сколько раз нужно повторить видео с хромакеем
            int loopCount = (int)Math.Ceiling(backgroundDuration.TotalSeconds / overlayDuration.TotalSeconds);
            Console.WriteLine($"Количество повторов: {loopCount}");

            // Рассчитываем количество кадров для видео с хромакеем
            int overlayFrameCount = (int)Math.Ceiling(overlayDuration.TotalSeconds * 59.94);  // Предполагаем, что FPS = 59.94
            Console.WriteLine($"Количество кадров для видео с хромакеем: {overlayFrameCount}");

            // Настроенный фильтр chromakey с уменьшенной чувствительностью
            FFMpegArguments
                .FromFileInput(backgroundVideoPath)  // Фон: видео
                .AddFileInput(overlayVideoPath)      // Видеоролик с зелёным фоном
                .OutputToFile(outputPath, true, options => options
                    .WithCustomArgument($"-filter_complex \"[1:v]loop=loop={loopCount - 1}:size={overlayFrameCount}:start=0, chromakey=0x00FF01:0.1:0.05[ckout];[0:v][ckout] overlay=0:0, scale=1080:1920\"")
                )
                .ProcessSynchronously();  // Запуск синхронного процесса

            Console.WriteLine("Видео с хромакеем успешно добавлено.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при добавлении видео с хромакеем: {ex.Message}");
        }
    }

    // Метод для получения длительности видео
    public static TimeSpan GetVideoDuration(string videoPath)
    {
        var mediaInfo = FFProbe.Analyse(videoPath);
        return mediaInfo.Duration;
    }
}