using Telegram.Bot.Types;
using Telegram.Bot;
using PaymentService_NOWPaymentsService_;
using VideoProcessing;
using Telegram.Bot.Types.ReplyMarkups;
using System.Collections.Concurrent;
using Telegram.Bot.Types.InputFiles;
using Domain.Models;
using System;
using DataAccess;
using Microsoft.EntityFrameworkCore;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace TelegramBot.Services;

public class TelegramBotService
{
    private readonly string _api_key_nowpayments;

    private readonly TelegramBotClient _botClient;
    private readonly IVideoProcessingService _videoService;
    private readonly INowPaymentsService _paymentService;
    private readonly AppDbContext _dbContext;

    private readonly Dictionary<long, string> userStates = new Dictionary<long, string>();

    public TelegramBotService(string telegramToken, string api_key_payments, IVideoProcessingService videoService, INowPaymentsService paymentService, AppDbContext dbContext)
    {
        _botClient = new TelegramBotClient(telegramToken);
        _videoService = videoService;
        _paymentService = paymentService;
        _dbContext = dbContext;
        _api_key_nowpayments = api_key_payments;
    }

    public void Start()
    {
        _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Telegram.Bot.Types.Update update, CancellationToken cancellationToken)
    {
        // Получаем Telegram ID пользователя
        long telegramUserId = update.Message?.From?.Id ?? update.CallbackQuery?.From?.Id ?? -100;

        if (update.Message != null && update.Message.Text != null)
        {
            long chatId = update.Message.Chat.Id;

            if (update.Message.Text.StartsWith("/start"))
            {
                // Вызов метода для обработки команды /start
                await HandleStartCommand(chatId);
            }
            else if (update.Message.Text.StartsWith("/menu"))
            {
                // Вызов метода для отображения главного меню
                await ShowMainMenu(chatId);
            }
        }
        else if (update.Message != null && update.Message.Video != null)
        {
            // Если сообщение содержит видео, обрабатываем загрузку
            await HandleMessageUpdateAsync(telegramUserId, update);
        }
        else if (update.CallbackQuery != null)
        {
            long chatId = update.CallbackQuery.Message.Chat.Id;
            string callbackData = update.CallbackQuery.Data;

            // Обработка нажатий на кнопки
            if (callbackData == "create_payment")
            {
                // Создание нового платежа
                await HandleCreatePaymentCallback(chatId, _api_key_nowpayments, update);
            }
            else if (callbackData == "check_payment_status")
            {
                await HandleUpdatePaymentStatus(chatId, _api_key_nowpayments, update);
            }
            else if (callbackData == "upload_video")
            {
                // Вызов метода для загрузки видео
                await HandleUploadVideoCallback(telegramUserId);
            }
            else if (callbackData == "back_to_menu")
            {
                // Возвращаем пользователя в главное меню
                await ShowMainMenu(chatId);
            }
        }
    }

    // Метод для обработки сообщений и видео
    private async Task HandleMessageUpdateAsync(long chatId, Telegram.Bot.Types.Update update)
    {
        var message = update.Message;

        if (message?.Video != null)
        {
            try
            {
                var fileSize = message.Video.FileSize; // Размер файла в байтах
                var resolutionWidth = message.Video.Width;
                var resolutionHeight = message.Video.Height;

                // Проверяем разрешение видео (для горизонтальных и вертикальных видео)
                if ((resolutionWidth > 1920 && resolutionHeight > 1080) && // Для горизонтальных видео
                    (resolutionWidth > 1080 && resolutionHeight > 1920)) // Для вертикальных видео
                {
                    await _botClient.SendTextMessageAsync(chatId, "Разрешение видео слишком высокое. Максимальное разрешение — 1920x1080 (горизонтальное) или 1080x1920 (вертикальное).");
                    return;
                }

                // Проверяем размер файла
                if (fileSize > 48 * 1024 * 1024) // 45 MB в байтах
                {
                    await _botClient.SendTextMessageAsync(chatId, "Файл слишком большой. Максимальный размер видео — 45 MB.");
                    return;
                }

                // Если ожидается загрузка основного видео
                if (userStates.ContainsKey(chatId) && userStates[chatId] == "awaiting_main_video")
                {
                    await HandleMainVideoUpload(chatId, message);
                }
                // Если ожидается загрузка видео с хромакеем
                else if (userStates.ContainsKey(chatId) && userStates[chatId] == "awaiting_chroma_video")
                {
                    await HandleChromaVideoUpload(chatId, message);
                }
            }
            catch (Exception ex)
            {
                // Логирование ошибки в консоль с детальной информацией
                Console.WriteLine($"Ошибка при обработке видео: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");

                // Отправляем пользователю сообщение об ошибке
                await _botClient.SendTextMessageAsync(chatId, "Произошла ошибка при загрузке видео. Пожалуйста, попробуйте позже.");

                // Выводим сообщение в консоль о размере файла и других деталях
                Console.WriteLine($"Размер файла: {message.Video.FileSize}");
                Console.WriteLine($"Разрешение видео: {message.Video.Width}x{message.Video.Height}");
                Console.WriteLine($"Длительность видео: {message.Video.Duration} секунд");
            }

        }
    }

    private async Task HandleStartCommand(long chatId)
    {
        var subscriptionMenu = new InlineKeyboardMarkup(new[]
        {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("Оплатить подписку", "create_payment"),
            InlineKeyboardButton.WithCallbackData("Проверить статус платежа", "check_payment_status")
        }
    });

        await _botClient.SendTextMessageAsync(chatId, "Добро пожаловать! Выберите действие:", replyMarkup: subscriptionMenu);
    }

    private async Task ShowMainMenu(long chatId)
    {
        var mainMenu = new InlineKeyboardMarkup(new[]
        {
        new[]
        {
            InlineKeyboardButton.WithCallbackData("Загрузить видео", "upload_video"),
            InlineKeyboardButton.WithCallbackData("Оплата", "create_payment")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("Связь с админом", "contact_admin")
        },
        new[]
        {
            InlineKeyboardButton.WithCallbackData("Обновить и узнать статус платежа", "check_payment_status")
        }
    });

        await _botClient.SendTextMessageAsync(chatId, "Выберите действие:", replyMarkup: mainMenu);
    }

    private async Task HandleCreatePaymentCallback(long chatId, string apiKey, Telegram.Bot.Types.Update update)
    {
        var menu = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Назад в меню", "back_to_menu"),
        });

        try
        {
            // Получаем ID пользователя Telegram
            long telegramUserId = update.Message?.From?.Id ?? update.CallbackQuery?.From?.Id ?? 0;

            if (telegramUserId == 0)
            {
                throw new Exception("Не удалось получить ID пользователя Telegram.");
            }

            // Проверяем, существует ли незавершённый платеж для данного пользователя
            var existingPayment = await _dbContext.Payments
                .Where(p => p.TelegramId == telegramUserId && p.PaymentStatus == "waiting")
                .FirstOrDefaultAsync();

            if (existingPayment != null)
            {
                // Если у пользователя уже есть незавершённый платеж, выводим сообщение и не создаем новый
                await _botClient.SendTextMessageAsync(chatId, "У вас уже есть незавершённый платеж. Завершите его перед созданием нового.", replyMarkup: menu);
                return;
            }

            // Ищем существующего пользователя по его Telegram ID
            var existingUser = await _dbContext.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramUserId);

            if (existingUser != null)
            {
                // Если пользователь существует и подписка истекла, создаем новый платёж
                if (existingUser.SubscriptionExpiresAt == null || existingUser.SubscriptionExpiresAt < DateTime.UtcNow)
                {
                    await CreateNewPayment(chatId, apiKey, existingUser.UserId, telegramUserId, Guid.NewGuid().ToString(), menu);
                }
                else
                {
                    await _botClient.SendTextMessageAsync(chatId, "У вас уже активная подписка. Нет необходимости в новом платеже.", replyMarkup: menu);
                }
            }
            else
            {
                // Если пользователя нет в базе, создаем нового и делаем платёж
                await CreateNewPayment(chatId, apiKey, null, telegramUserId, Guid.NewGuid().ToString(), menu);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при создании платежа: {ex.Message}");
            await _botClient.SendTextMessageAsync(chatId, "Произошла ошибка при создании платежа. Пожалуйста, попробуйте позже.", replyMarkup: menu);
        }
    }

    private async Task CreateNewPayment(long chatId, string apiKey, Guid? userId, long telegramUserId, string orderId, InlineKeyboardMarkup menu)
    {
        // Создаем новый платёж через сервис
        var paymentResponse = await _paymentService.CreatePayment(apiKey, "10", "usdtbsc", orderId);

        if (paymentResponse != null)
        {
            // Выводим информацию о платеже пользователю
            var message = $"<b>Платеж создан!</b>\n\n" +
                          $"<b>Адрес для оплаты:</b> {paymentResponse.PayAddress}\n" +
                          $"<b>Сумма к оплате:</b> {paymentResponse.PayAmount} {paymentResponse.PayCurrency}\n" +
                          $"<b>ID Платежа:</b> {paymentResponse.PaymentId}\n\n" +
                          $"<i>После оплаты, не забудьте проверить статус платежа с использованием Payment ID.</i>";

            await _botClient.SendTextMessageAsync(
                chatId,
                message,
                replyMarkup: menu,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
            );

            // Если пользователя нет, создаем нового
            if (userId == null)
            {
                var newUser = new Domain.Models.User
                {
                    UserId = Guid.NewGuid(),
                    TelegramId = telegramUserId,
                    SubscriptionExpiresAt = null, // Подписка пока не активирована
                    DailyVideoUploadCount = 0,
                    LastUploadDate = DateTime.MinValue, // Пустое значение для последней загрузки видео
                    Payments = new List<Payment>() // Инициализируем коллекцию платежей
                };

                // Добавляем нового пользователя в базу данных
                await _dbContext.Users.AddAsync(newUser);
                await _dbContext.SaveChangesAsync();

                // Присваиваем userId только что созданного пользователя
                userId = newUser.UserId;
            }

            // Создаем запись о платеже и привязываем её к пользователю
            var payment = new Payment
            {
                Id = Guid.NewGuid(),
                TelegramId = telegramUserId,
                UserId = userId, // Привязываем платеж к новому пользователю или существующему
                OrderId = orderId,
                PaymentStatus = paymentResponse.PaymentStatus,
                PriceAmount = paymentResponse.PriceAmount,
                CreatedAt = DateTime.UtcNow,
                PayAddress = paymentResponse.PayAddress,
                PayCurrency = paymentResponse.PayCurrency,
                PaymentId = paymentResponse.PaymentId,
                OutcomeCurrency = "N/A",
                PayinHash = "N/A"
            };

            // Добавляем платёж в базу данных
            await _dbContext.Payments.AddAsync(payment);
            await _dbContext.SaveChangesAsync();
        }
        else
        {
            await _botClient.SendTextMessageAsync(chatId, "Ошибка при создании платежа.", replyMarkup: menu);
        }
    }

    private async Task HandleUpdatePaymentStatus(long chatId, string apiKey, Telegram.Bot.Types.Update update)
    {
        var menu = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Назад в меню", "back_to_menu")
        });

        // Получаем ID пользователя Telegram
        long telegramUserId = update.Message?.From?.Id ?? update.CallbackQuery?.From?.Id ?? 0;

        if (telegramUserId == 0)
        {
            await _botClient.SendTextMessageAsync(chatId, "Не удалось получить ID пользователя Telegram.", replyMarkup: menu);
            return;
        }

        // Поиск последнего платежа пользователя
        var lastPayment = await _dbContext.Payments
            .Where(p => p.TelegramId == telegramUserId)
            .OrderByDescending(p => p.CreatedAt) // Сортировка по дате создания
            .FirstOrDefaultAsync();

        if (lastPayment == null)
        {
            await _botClient.SendTextMessageAsync(chatId, "У вас нет платежей. Пожалуйста, оплатите подписку.", replyMarkup: menu);
            return;
        }

        // Проверка статуса через платежный сервис
        var paymentStatusResponse = await _paymentService.GetPaymentStatus(apiKey, lastPayment.PaymentId);

        if (paymentStatusResponse != null)
        {
            // Обновляем статус платежа, если он изменился
            if (lastPayment.PaymentStatus != paymentStatusResponse.PaymentStatus)
            {
                lastPayment.PaymentStatus = paymentStatusResponse.PaymentStatus;
                lastPayment.UpdatedAt = DateTime.UtcNow;
                _dbContext.Payments.Update(lastPayment);
                await _dbContext.SaveChangesAsync();

                // Если платеж завершен, обновляем подписку пользователя
                if (paymentStatusResponse.PaymentStatus == "finished")
                {
                    var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramUserId);

                    if (user != null)
                    {
                        user.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(7); // Продление подписки на 7 дней
                        user.MaxDailyVideoUploadLimit = 10;
                        _dbContext.Users.Update(user);
                        await _dbContext.SaveChangesAsync();

                        await _botClient.SendTextMessageAsync(chatId, "Ваш платеж завершен, подписка продлена на 7 дней!", replyMarkup: menu);
                    }
                }
            }

            // Отображаем информацию о платеже
            var message = $"<b>Статус платежа:</b> {paymentStatusResponse.PaymentStatus}\n" +
                          $"<b>Адрес для оплаты:</b> {paymentStatusResponse.PayAddress}\n" +
                          $"<b>Сумма платежа:</b> {paymentStatusResponse.PayAmount} {paymentStatusResponse.PayCurrency}\n" +
                          $"<b>Оплачено:</b> {paymentStatusResponse.ActuallyPaid} {paymentStatusResponse.PayCurrency}\n" +
                          $"<b>Дата создания:</b> {paymentStatusResponse.CreatedAt}\n\n" +
                          $"<i>Платеж выполнен в валюте {paymentStatusResponse.OutcomeCurrency}. Если статус 'finished', ваш платеж завершен.</i>";

            await _botClient.SendTextMessageAsync(chatId, message, replyMarkup: menu, parseMode: Telegram.Bot.Types.Enums.ParseMode.Html);
        }
        else
        {
            await _botClient.SendTextMessageAsync(chatId, "Ошибка при проверке статуса платежа. Убедитесь, что вы ввели правильный Payment ID.", replyMarkup: menu);
        }
    }

    // Метод обработки команды загрузки видео
    public async Task HandleUploadVideoCallback(long telegramUserId)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramUserId);

        if (user == null)
        {
            await _botClient.SendTextMessageAsync(telegramUserId, "Ваш аккаунт не найден. Убедитесь, что вы зарегистрированы после успешной оплаты.");
            return;
        }

        // Проверка, если у пользователя есть активная подписка
        if (!user.SubscriptionExpiresAt.HasValue || user.SubscriptionExpiresAt.Value < DateTime.UtcNow)
        {
            // Если подписки нет или она истекла, проверяем последний платеж
            var lastPayment = await _dbContext.Payments
                .Where(p => p.TelegramId == telegramUserId && p.PaymentStatus == "finished")
                .OrderByDescending(p => p.CreatedAt)
                .FirstOrDefaultAsync();

            if (lastPayment == null)
            {
                // Если нет успешных платежей, уведомляем пользователя о необходимости оплаты
                await _botClient.SendTextMessageAsync(telegramUserId, "У вас нет активной подписки. Пожалуйста, оплатите подписку.");
                return;
            }
            else
            {
                // Если найден успешный платеж, обновляем срок действия подписки
                user.SubscriptionExpiresAt = DateTime.UtcNow.AddDays(7);
                user.MaxDailyVideoUploadLimit = 15;
                _dbContext.Users.Update(user);
                await _dbContext.SaveChangesAsync();
            }
        }

        // Проверка дневного лимита на загрузку видео
        if (user.LastUploadDate.Date < DateTime.UtcNow.Date)
        {
            user.DailyVideoUploadCount = 0; // Обнуляем лимит для нового дня
            user.LastUploadDate = DateTime.UtcNow;
        }

        if (user.DailyVideoUploadCount >= user.MaxDailyVideoUploadLimit)
        {
            await _botClient.SendTextMessageAsync(telegramUserId, $"Вы достигли лимита загрузки видео на сегодня. Лимит: {user.MaxDailyVideoUploadLimit} видео.");
            return;
        }

        // Обновляем состояние пользователя и готовимся к загрузке видео
        userStates[telegramUserId] = "awaiting_main_video";
        await _botClient.SendTextMessageAsync(telegramUserId, "Отправьте основное видео для обработки.");

        // Опциональная кнопка отмены загрузки
        var cancelUploadMenu = new InlineKeyboardMarkup(new[]
        {
            InlineKeyboardButton.WithCallbackData("Отмена", "cancel_upload")
        });
        await _botClient.SendTextMessageAsync(telegramUserId, "Вы можете отменить загрузку, нажав на кнопку ниже.", replyMarkup: cancelUploadMenu);
    }

    // Метод для обработки загруженного основного видео
    public async Task HandleMainVideoUpload(long telegramUserId, Message message)
    {
        try
        {
            if (message.Video == null)
            {
                await _botClient.SendTextMessageAsync(telegramUserId, "Видео не найдено. Попробуйте снова.");
                return;
            }

            var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramUserId);
            if (user == null)
            {
                await _botClient.SendTextMessageAsync(telegramUserId, "Ваш аккаунт не найден.");
                return;
            }

            // Проверка размера видео
            if (message.Video.FileSize > 45 * 1024 * 1024) // 45 MB лимит
            {
                await _botClient.SendTextMessageAsync(telegramUserId, "Файл слишком большой. Максимальный размер видео — 45 MB.");
                return;
            }

            // Сохранение основного видео на сервере
            string mainVideoPath = Path.Combine(Path.GetTempPath(), $"{telegramUserId}_main_video.mp4");

            var fileId = message.Video.FileId;
            var fileInfo = await _botClient.GetFileAsync(fileId);
            using (var fileStream = new FileStream(mainVideoPath, FileMode.Create))
            {
                await _botClient.DownloadFileAsync(fileInfo.FilePath, fileStream);
            }

            // Сохранение информации о видео в базе данных
            var newVideo = new Domain.Models.Video
            {
                VideoId = Guid.NewGuid(),
                UserId = user.UserId,
                FilePath = mainVideoPath,
                UploadedAt = DateTime.UtcNow,
                Duration = TimeSpan.FromSeconds(message.Video.Duration)
            };

            await _dbContext.Videos.AddAsync(newVideo);

            // Обновляем количество загруженных видео
            user.DailyVideoUploadCount++;
            user.LastUploadDate = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync();

            // Сообщение пользователю о том, что видео успешно загружено
            await _botClient.SendTextMessageAsync(telegramUserId, "Основное видео успешно загружено. Отправьте видео с зеленым фоном.");
            userStates[telegramUserId] = "awaiting_chroma_video"; // обновляем состояние пользователя
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при обработке видео: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");

            // Сообщаем пользователю об ошибке
            await _botClient.SendTextMessageAsync(telegramUserId, "Произошла ошибка при загрузке видео. Попробуйте снова.");
        }
    }


    // Метод для обработки загруженного видео с хромакеем
    public async Task HandleChromaVideoUpload(long telegramUserId, Message message)
    {
        if (message.Video == null)
        {
            await _botClient.SendTextMessageAsync(telegramUserId, "Видео с хромакеем не найдено. Попробуйте снова.");
            return;
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramUserId);

        if (user == null)
        {
            await _botClient.SendTextMessageAsync(telegramUserId, "Ваш аккаунт не найден.");
            return;
        }

        // Сохранение видео с хромакеем на сервере
        string chromaVideoPath = Path.Combine(Path.GetTempPath(), $"{telegramUserId}_chroma_video.mp4");

        var fileId = message.Video.FileId;
        var fileInfo = await _botClient.GetFileAsync(fileId);
        using (var fileStream = new FileStream(chromaVideoPath, FileMode.Create))
        {
            await _botClient.DownloadFileAsync(fileInfo.FilePath, fileStream);
        }

        // Обновляем состояние и начинаем обработку
        userStates[telegramUserId] = "processing_videos";
        await _botClient.SendTextMessageAsync(telegramUserId, "Видео с зеленым фоном успешно загружено. Начинаем обработку...");

        // Обработка видео
        await ProcessVideos(telegramUserId, chromaVideoPath);
    }

    // Метод для обработки видео
    private async Task ProcessVideos(long telegramUserId, string chromaVideoPath)
    {
        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.TelegramId == telegramUserId);
        if (user == null)
        {
            await _botClient.SendTextMessageAsync(telegramUserId, "Не удалось найти данные пользователя.");
            return;
        }

        var lastVideo = await _dbContext.Videos
            .Where(v => v.UserId == user.UserId)
            .OrderByDescending(v => v.UploadedAt)
            .FirstOrDefaultAsync();

        if (lastVideo == null)
        {
            await _botClient.SendTextMessageAsync(telegramUserId, "Не удалось найти загруженное видео.");
            return;
        }

        var processedMainVideoPath = Path.Combine(Path.GetTempPath(), $"{telegramUserId}_processed_main_video.mp4");
        _videoService.ProcessVideo(lastVideo.FilePath, processedMainVideoPath);

        // Объединяем с видео с хромакеем
        var finalVideoPath = Path.Combine(Path.GetTempPath(), $"{telegramUserId}_final_video.mp4");
        _videoService.AddVideoWithChromaKey(processedMainVideoPath, chromaVideoPath, finalVideoPath);

        await _botClient.SendTextMessageAsync(telegramUserId, "Видео успешно обработано. Отправляю результат...");

        // Отправляем пользователю обработанное видео
        using (var finalVideoStream = new FileStream(finalVideoPath, FileMode.Open, FileAccess.Read))
        {
            var inputOnlineFile = new InputOnlineFile(finalVideoStream, $"{telegramUserId}_final_video.mp4");
            await _botClient.SendVideoAsync(telegramUserId, inputOnlineFile, caption: "Вот ваше готовое видео!");
        }

        // Обнуляем состояние пользователя после завершения обработки
        userStates[telegramUserId] = null;

        // Возвращаем пользователя в главное меню
        await ShowMainMenu(telegramUserId);
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }
}