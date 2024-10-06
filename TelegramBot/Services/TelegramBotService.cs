using Telegram.Bot.Types;
using Telegram.Bot;
using PaymentService_NOWPaymentsService_;
using VideoProcessing;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;
using Telegram.Bot.Types.InputFiles;

namespace TelegramBot.Services;

public class TelegramBotService
{
    // Хранение сессий пользователей
    private readonly ConcurrentDictionary<long, UserSession> userSessions = new ConcurrentDictionary<long, UserSession>();

    private readonly TelegramBotClient _botClient;
    private readonly IVideoProcessingService _videoService;
    private readonly INowPaymentsService _paymentService;

    public TelegramBotService(string token, IVideoProcessingService videoService, INowPaymentsService paymentService)
    {
        _botClient = new TelegramBotClient(token);
        _videoService = videoService;
        _paymentService = paymentService;
    }

    public void Start()
    {
        _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message is Message message)
        {
            long chatId = message.Chat.Id;

            // Если сессии для данного пользователя нет, создаем новую
            if (!userSessions.ContainsKey(chatId))
            {
                userSessions[chatId] = new UserSession();
            }

            // Если пользователь отправил видео
            if (message.Video != null)
            {
                // Проверяем статус в сессии пользователя
                if (userSessions[chatId].Status == "awaiting_main_video")
                {
                    // Сохранение основного видео
                    string mainVideoPath = Path.Combine(Path.GetTempPath(), $"{chatId}_main_video.mp4");

                    var fileId = message.Video.FileId;
                    var fileInfo = await botClient.GetFileAsync(fileId);
                    using (var fileStream = new FileStream(mainVideoPath, FileMode.Create))
                    {
                        await botClient.DownloadFileAsync(fileInfo.FilePath, fileStream);
                    }

                    userSessions[chatId].MainVideoPath = mainVideoPath;
                    userSessions[chatId].Status = "awaiting_chroma_video";

                    // Сообщение пользователю
                    await botClient.SendTextMessageAsync(chatId, "Основное видео получено. Теперь загрузите видео с зеленым фоном.");
                }
                else if (userSessions[chatId].Status == "awaiting_chroma_video")
                {
                    // Сохранение видео с хромакеем
                    string chromaVideoPath = Path.Combine(Path.GetTempPath(), $"{chatId}_chroma_video.mp4");

                    var fileId = message.Video.FileId;
                    var fileInfo = await botClient.GetFileAsync(fileId);
                    using (var fileStream = new FileStream(chromaVideoPath, FileMode.Create))
                    {
                        await botClient.DownloadFileAsync(fileInfo.FilePath, fileStream);
                    }

                    userSessions[chatId].ChromaVideoPath = chromaVideoPath;
                    userSessions[chatId].Status = "processing_videos";

                    // Сообщение пользователю
                    await botClient.SendTextMessageAsync(chatId, "Видео с зеленым фоном получено. Начинаем обработку...");

                    // 1. Сначала обрабатываем основное видео методом ProcessVideo
                    var processedMainVideoPath = Path.Combine(Path.GetTempPath(), $"{chatId}_processed_main_video.mp4");
                    _videoService.ProcessVideo(userSessions[chatId].MainVideoPath, processedMainVideoPath);

                    // 2. Затем добавляем видео с хромакеем методом AddVideoWithChromaKey
                    var finalVideoPath = Path.Combine(Path.GetTempPath(), $"{chatId}_final_video.mp4");
                    _videoService.AddVideoWithChromaKey(processedMainVideoPath, userSessions[chatId].ChromaVideoPath, finalVideoPath, 0, 0);

                    // Сообщение о завершении
                    await botClient.SendTextMessageAsync(chatId, "Видео успешно обработано. Отправляю результат...");

                    // Отправляем пользователю финальное видео
                    using (var finalVideoStream = new FileStream(finalVideoPath, FileMode.Open, FileAccess.Read))
                    {
                        var inputOnlineFile = new InputOnlineFile(finalVideoStream, $"{chatId}_final_video.mp4");
                        await botClient.SendVideoAsync(chatId, inputOnlineFile, caption: "Вот ваше готовое видео!");
                    }

                    // Обнуляем сессию после завершения
                    userSessions[chatId].Status = null;
                    userSessions[chatId].MainVideoPath = null;
                    userSessions[chatId].ChromaVideoPath = null;

                    await botClient.SendTextMessageAsync(chatId, "Процесс завершен. Вы можете начать новую обработку.");
                }
            }
            // Если пользователь вводит команду /start
            else if (message.Text == "/start")
            {
                // Приветственное сообщение с описанием возможностей бота
                string welcomeMessage = "Привет! Представляю телеграм-бота, который автоматически обрабатывает видео из TikTok.\n\n" +
                                        "Бот позволяет вам:\n" +
                                        "1. Загрузить видео для обработки.\n" +
                                        "2. Узнать информацию о ценах на подписку.\n" +
                                        "3. Связаться с администратором для помощи.\n\n" +
                                        "Выберите действие:";

                // Главное меню с кнопками
                var mainMenu = new InlineKeyboardMarkup(new[]
                {
                new[] // Первая строка кнопок
                {
                    InlineKeyboardButton.WithCallbackData("Загрузить видео для обработки", "upload_video"),
                    InlineKeyboardButton.WithCallbackData("Узнать цены на подписку", "subscription_prices")
                },
                new[] // Вторая строка кнопок
                {
                    InlineKeyboardButton.WithCallbackData("Связаться с админом", "contact_admin")
                }
            });

                var sentMessage = await botClient.SendTextMessageAsync(message.Chat.Id, welcomeMessage, replyMarkup: mainMenu);
                userSessions[chatId].LastMessageId = sentMessage.MessageId; // Сохраняем ID сообщения для последующего удаления
            }
        }
        else if (update.CallbackQuery != null)
        {
            long chatId = update.CallbackQuery.Message.Chat.Id;

            // Если пользователь с этим chatId еще не имеет сессии, создаем ее
            if (!userSessions.ContainsKey(chatId))
            {
                userSessions[chatId] = new UserSession();
            }

            // Если сессия уже существует, пробуем удалить предыдущее сообщение
            if (userSessions[chatId].LastMessageId.HasValue)
            {
                try
                {
                    await botClient.DeleteMessageAsync(chatId, (int)userSessions[chatId].LastMessageId.Value);
                }
                catch (Exception ex)
                {
                    if (ex.Message.Contains("message to delete not found"))
                    {
                        // Если сообщение уже было удалено, отправляем уведомление
                        await botClient.SendTextMessageAsync(chatId, "Сообщение не найдено. Попробуйте снова.");

                        // Можно добавить кнопку для перезапуска действия или возврата в главное меню
                        var retryMenu = new InlineKeyboardMarkup(new[]
                        {
                        InlineKeyboardButton.WithCallbackData("Назад в главное меню", "back_to_menu")
                    });

                        await botClient.SendTextMessageAsync(chatId, "Попробуйте снова или вернитесь в главное меню:", replyMarkup: retryMenu);

                        // Сбрасываем сохраненный ID сообщения, чтобы не пытаться его удалить снова
                        userSessions[chatId].LastMessageId = null;
                    }
                    else
                    {
                        // Логируем и обрабатываем другие возможные ошибки
                        Console.WriteLine($"Ошибка при удалении сообщения: {ex.Message}");
                    }
                }
            }

            string callbackData = update.CallbackQuery.Data;

            // Логика кнопок
            if (callbackData == "upload_video")
            {
                // Переключаем статус сессии на ожидание основного видео
                userSessions[chatId].Status = "awaiting_main_video";

                // Добавляем кнопку для отмены загрузки
                var cancelUploadMenu = new InlineKeyboardMarkup(new[]
                {
                InlineKeyboardButton.WithCallbackData("Отмена", "cancel_upload")
            });

                var sentMessage = await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Пожалуйста, отправьте видео для обработки или нажмите отмена.", replyMarkup: cancelUploadMenu);
                userSessions[chatId].LastMessageId = sentMessage.MessageId;
            }
            else if (callbackData == "cancel_upload")
            {
                // Возвращаем в главное меню
                var mainMenu = new InlineKeyboardMarkup(new[]
                {
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Загрузить видео для обработки", "upload_video"),
                    InlineKeyboardButton.WithCallbackData("Узнать цены на подписку", "subscription_prices")
                },
                new[]
                {
                    InlineKeyboardButton.WithCallbackData("Связаться с админом", "contact_admin")
                }
            });

                var sentMessage = await botClient.SendTextMessageAsync(update.CallbackQuery.Message.Chat.Id, "Вы вернулись в главное меню. Выберите действие:", replyMarkup: mainMenu);
                userSessions[chatId].LastMessageId = sentMessage.MessageId;
            }
        }
    }


    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }
}