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

            // Создаем новую сессию для пользователя, если ее нет
            if (!userSessions.ContainsKey(chatId))
            {
                userSessions[chatId] = new UserSession();
            }

            // Обработка видео от пользователя
            if (message.Video != null)
            {
                if (userSessions[chatId].Status == "awaiting_main_video")
                {
                    await HandleMainVideoUpload(chatId, message);
                }
                else if (userSessions[chatId].Status == "awaiting_chroma_video")
                {
                    await HandleChromaVideoUpload(chatId, message);
                }
            }
            // Обработка команды /start
            else if (message.Text == "/start")
            {
                await HandleStartCommand(chatId);
            }
        }
        else if (update.CallbackQuery != null)
        {
            long chatId = update.CallbackQuery.Message.Chat.Id;

            // Создаем сессию, если ее нет
            if (!userSessions.ContainsKey(chatId))
            {
                userSessions[chatId] = new UserSession();
            }

            // Пробуем удалить предыдущее сообщение, если оно есть
            if (userSessions[chatId].LastMessageId.HasValue)
            {
                try
                {
                    await botClient.DeleteMessageAsync(chatId, (int)userSessions[chatId].LastMessageId.Value);
                }
                catch (Exception ex)
                {
                    // Обрабатываем ситуацию, если сообщение уже удалено
                    if (ex.Message.Contains("message to delete not found"))
                    {
                        await botClient.SendTextMessageAsync(chatId, "Сообщение не найдено. Попробуйте снова.");
                        await ShowRetryMenu(chatId);
                        userSessions[chatId].LastMessageId = null;
                    }
                    else
                    {
                        Console.WriteLine($"Ошибка при удалении сообщения: {ex.Message}");
                    }
                }
            }

            string callbackData = update.CallbackQuery.Data;

            // Логика обработки кнопок
            if (callbackData == "upload_video")
            {
                await HandleUploadVideoCallback(chatId);
            }
            else if (callbackData == "cancel_upload")
            {
                await HandleCancelUploadCallback(chatId);
            }
            else if (callbackData == "contact_admin")
            {
                await HandleContactAdminCallback(chatId);
            }
            else if (callbackData == "back_to_menu")
            {
                await HandleBackToMainMenu(chatId);  // Переход в главное меню
            }
            else if (callbackData == "subscription_prices")  // Здесь "subscription_prices" - это callback data для кнопки "Узнать цены на подписку"
            {
                await HandleSubscriptionPricesCallback(chatId);  // Переход к обработке подписки
            }
            else if (callbackData == "pay_subscription")
            {
                await HandlePaySubscriptionCallback(chatId); // Обработка оплаты
            }
            else if (callbackData == "check_payment_status")
            {
                await HandleCheckPaymentStatusCallback(chatId); // Проверка статуса платежа
            }
        }
    }

    private async Task HandleStartCommand(long chatId)
    {
        // Приветственное сообщение с описанием возможностей бота
        string welcomeMessage = "Привет! Представляю телеграм-бота, который автоматически обрабатывает видео из TikTok.\n\n" +
                                "Бот позволяет вам:\n" +
                                "1. Загрузить видео для обработки.\n" +
                                "2. Узнать информацию о ценах на подписку.\n" +
                                "3. Связаться с администратором для помощи.\n\n" +
                                "Выберите действие:";

        await ShowMainMenu(chatId); // Показываем главное меню
    }

    private async Task HandleMainVideoUpload(long chatId, Message message)
    {
        // Сохранение основного видео
        string mainVideoPath = Path.Combine(Path.GetTempPath(), $"{chatId}_main_video.mp4");

        var fileId = message.Video.FileId;
        var fileInfo = await _botClient.GetFileAsync(fileId);
        using (var fileStream = new FileStream(mainVideoPath, FileMode.Create))
        {
            await _botClient.DownloadFileAsync(fileInfo.FilePath, fileStream);
        }

        userSessions[chatId].MainVideoPath = mainVideoPath;
        userSessions[chatId].Status = "awaiting_chroma_video";

        // Сообщение пользователю
        await _botClient.SendTextMessageAsync(chatId, "Основное видео получено. Теперь загрузите видео с зеленым фоном.");
    }

    private async Task HandleChromaVideoUpload(long chatId, Message message)
    {
        // Сохранение видео с хромакеем
        string chromaVideoPath = Path.Combine(Path.GetTempPath(), $"{chatId}_chroma_video.mp4");

        var fileId = message.Video.FileId;
        var fileInfo = await _botClient.GetFileAsync(fileId);
        using (var fileStream = new FileStream(chromaVideoPath, FileMode.Create))
        {
            await _botClient.DownloadFileAsync(fileInfo.FilePath, fileStream);
        }

        userSessions[chatId].ChromaVideoPath = chromaVideoPath;
        userSessions[chatId].Status = "processing_videos";

        // Сообщение пользователю
        await _botClient.SendTextMessageAsync(chatId, "Видео с зеленым фоном получено. Начинаем обработку...");

        // Начало обработки видео
        await ProcessVideos(chatId);
    }

    private async Task ProcessVideos(long chatId)
    {
        // 1. Обрабатываем основное видео
        var processedMainVideoPath = Path.Combine(Path.GetTempPath(), $"{chatId}_processed_main_video.mp4");
        _videoService.ProcessVideo(userSessions[chatId].MainVideoPath, processedMainVideoPath);

        // 2. Добавляем видео с хромакеем
        var finalVideoPath = Path.Combine(Path.GetTempPath(), $"{chatId}_final_video.mp4");
        _videoService.AddVideoWithChromaKey(processedMainVideoPath, userSessions[chatId].ChromaVideoPath, finalVideoPath, 0, 0);

        // Отправляем сообщение о завершении
        await _botClient.SendTextMessageAsync(chatId, "Видео успешно обработано. Отправляю результат...");

        // Отправляем финальное видео
        using (var finalVideoStream = new FileStream(finalVideoPath, FileMode.Open, FileAccess.Read))
        {
            var inputOnlineFile = new InputOnlineFile(finalVideoStream, $"{chatId}_final_video.mp4");
            await _botClient.SendVideoAsync(chatId, inputOnlineFile, caption: "Вот ваше готовое видео!");
        }

        // Обнуляем сессию после завершения
        ResetUserSession(chatId);

        // После обработки видео возвращаемся в главное меню
        await ShowMainMenu(chatId);
    }

    private void ResetUserSession(long chatId)
    {
        userSessions[chatId].Status = null;
        userSessions[chatId].MainVideoPath = null;
        userSessions[chatId].ChromaVideoPath = null;
    }

    private async Task HandleUploadVideoCallback(long chatId)
    {
        // Переключаем статус сессии на ожидание основного видео
        userSessions[chatId].Status = "awaiting_main_video";

        // Добавляем кнопку для отмены загрузки
        var cancelUploadMenu = new InlineKeyboardMarkup(new[]
        {
        InlineKeyboardButton.WithCallbackData("Отмена", "cancel_upload")
    });

        var sentMessage = await _botClient.SendTextMessageAsync(chatId, "Пожалуйста, отправьте видео для обработки или нажмите отмена.", replyMarkup: cancelUploadMenu);
        userSessions[chatId].LastMessageId = sentMessage.MessageId;
    }

    private async Task ShowRetryMenu(long chatId)
    {
        var retryMenu = new InlineKeyboardMarkup(new[]
        {
        InlineKeyboardButton.WithCallbackData("Назад в главное меню", "back_to_menu")
    });

        await _botClient.SendTextMessageAsync(chatId, "Попробуйте снова или вернитесь в главное меню:", replyMarkup: retryMenu);
    }

    private async Task ShowMainMenu(long chatId)
    {
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

        var sentMessage = await _botClient.SendTextMessageAsync(chatId, "Вы вернулись в главное меню. Выберите действие:", replyMarkup: mainMenu);
        userSessions[chatId].LastMessageId = sentMessage.MessageId;
    }

    private async Task HandleCancelUploadCallback(long chatId)
    {
        // Возвращаем пользователя в главное меню при отмене загрузки
        await ShowMainMenu(chatId);
    }

    private async Task HandleSubscriptionPricesCallback(long chatId)
    {
        string priceInfo = "Подписка стоит 5 USDT в месяц. Оплатить подписку можно через наш бот.";

        var paymentMenu = new InlineKeyboardMarkup(new[]
        {
        InlineKeyboardButton.WithCallbackData("Оплатить подписку", "pay_subscription"),
        InlineKeyboardButton.WithCallbackData("Назад в главное меню", "back_to_menu")
    });

        // Удаляем предыдущее сообщение, если оно есть
        if (userSessions.ContainsKey(chatId) && userSessions[chatId].LastMessageId.HasValue)
        {
            try
            {
                await _botClient.DeleteMessageAsync(chatId, (int)userSessions[chatId].LastMessageId.Value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при удалении сообщения: {ex.Message}");
            }
        }

        // Отправляем информацию о подписке с кнопками оплаты и возврата в меню
        var sentMessage = await _botClient.SendTextMessageAsync(chatId, priceInfo, replyMarkup: paymentMenu);
        userSessions[chatId].LastMessageId = sentMessage.MessageId; // Сохраняем ID сообщения для удаления
    }

    private async Task HandleContactAdminCallback(long chatId)
    {
        string adminContact = "Свяжитесь с администратором через @present_s1mple11";

        var backMenu = new InlineKeyboardMarkup(new[]
        {
        InlineKeyboardButton.WithCallbackData("Назад в главное меню", "back_to_menu")
    });

        // Удаляем предыдущее сообщение, если оно есть
        if (userSessions.ContainsKey(chatId) && userSessions[chatId].LastMessageId.HasValue)
        {
            try
            {
                await _botClient.DeleteMessageAsync(chatId, (int)userSessions[chatId].LastMessageId.Value);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("message to delete not found"))
                {
                    // Сообщение уже удалено, ничего не делаем
                    Console.WriteLine("Сообщение уже удалено или не найдено.");
                }
                else
                {
                    // Обработка других ошибок
                    Console.WriteLine($"Ошибка при удалении сообщения: {ex.Message}");
                }
            }
        }

        // Отправляем сообщение с контактами админа и кнопкой назад в меню
        var sentMessage = await _botClient.SendTextMessageAsync(chatId, adminContact, replyMarkup: backMenu);
        userSessions[chatId].LastMessageId = sentMessage.MessageId; // Сохраняем ID сообщения для удаления
    }

    private async Task HandleBackToMainMenu(long chatId)
    {
        // Возвращаем пользователя в главное меню
        if (userSessions.ContainsKey(chatId) && userSessions[chatId].LastMessageId.HasValue)
        {
            try
            {
                await _botClient.DeleteMessageAsync(chatId, (int)userSessions[chatId].LastMessageId.Value);
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("message to delete not found"))
                {
                    // Сообщение уже удалено
                    Console.WriteLine("Сообщение уже удалено или не найдено.");
                }
                else
                {
                    // Обработка других ошибок
                    Console.WriteLine($"Ошибка при удалении сообщения: {ex.Message}");
                }
            }
        }

        await ShowMainMenu(chatId); // Переход в главное меню
    }

    private async Task HandleCheckPaymentStatusCallback(long chatId)
    {
        Thread.Sleep(3000);

        string paymentId = userSessions[chatId].PaymentId; // Идентификатор платежа сохраняется в сессии

        try
        {
            // Проверяем статус платежа через NowPayments
            var paymentStatus = await _paymentService.GetPaymentStatus("HWHWJ12-XMBMH68-GSH7XSV-5YE60WJ", paymentId);

            // Извлекаем статус платежа
            string status = paymentStatus.PaymentStatus; // Пример: "waiting", "confirmed", "failed"
            string message;

            // В зависимости от статуса, формируем сообщение
            if (status == "waiting")
            {
                message = "Платёж ожидает подтверждения. Переведите средства на указанный адрес.";
            }
            else if (status == "confirmed")
            {
                message = "Платёж успешно подтверждён! Спасибо за оплату.";
            }
            else
            {
                message = $"Платёж имеет статус: {status}. Попробуйте позже или свяжитесь с поддержкой.";
            }

            var backMenu = new InlineKeyboardMarkup(new[]
            {
        InlineKeyboardButton.WithCallbackData("Назад в главное меню", "back_to_menu")
    });

            // Отправляем информацию о статусе платежа
            var sentMessage = await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: backMenu);
            userSessions[chatId].LastMessageId = sentMessage.MessageId;
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(chatId, $"Ошибка при проверке статуса платежа: {ex.Message}");
        }
    }

    private async Task HandlePaySubscriptionCallback(long chatId)
    {
        // Определяем сумму и валюту для оплаты
        string amount = "8"; // Сумма в USDT
        string currency = "usdtbsc"; // Валюта (USDT на сети BSC)

        // Генерируем уникальный идентификатор заказа
        string orderId = Guid.NewGuid().ToString();

        try
        {
            // Создаём платёж через NowPayments
            var paymentResponse = await _paymentService.CreatePayment("HWHWJ12-XMBMH68-GSH7XSV-5YE60WJ", amount, currency, orderId);

            // Извлекаем нужную информацию из ответа
            var paymentId = paymentResponse.PaymentId; // Получаем идентификатор платежа
            var walletAddress = paymentResponse.PayAddress; // Адрес для перевода
            var paymentAmount = paymentResponse.PayAmount; // Сумма платежа

            // Сохраняем ID платежа в сессии пользователя
            userSessions[chatId].PaymentId = paymentId;

            // Формируем сообщение с информацией для оплаты
            string paymentInfo = $"Счёт на {paymentAmount} USDT создан.\n\n" +
                                 $"Для оплаты переведите {paymentAmount} USDT на адрес: {walletAddress}.\n\n" +
                                 $"Статус платежа можно проверить через меню.";

            var paymentMenu = new InlineKeyboardMarkup(new[]
            {
        InlineKeyboardButton.WithCallbackData("Проверить статус платежа", "check_payment_status"),
        InlineKeyboardButton.WithCallbackData("Назад в главное меню", "back_to_menu")
    });

            // Отправляем информацию о платеже пользователю
            var sentMessage = await _botClient.SendTextMessageAsync(chatId, paymentInfo, replyMarkup: paymentMenu);
            userSessions[chatId].LastMessageId = sentMessage.MessageId;
        }
        catch (Exception ex)
        {
            await _botClient.SendTextMessageAsync(chatId, $"Ошибка при создании платежа: {ex.Message}");
        }

        Thread.Sleep(2000);
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }
}