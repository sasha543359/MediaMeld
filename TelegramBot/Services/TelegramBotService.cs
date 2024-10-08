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

namespace TelegramBot.Services;

public class TelegramBotService
{
    private const string api_key_nowpayments = "HWHWJ12-XMBMH68-GSH7XSV-5YE60WJ";
    // string amount = "8"; // Сумма в USDT
    // string currency = "usdtbsc"; // Валюта (USDT на сети BSC)

    private readonly TelegramBotClient _botClient;
    private readonly IVideoProcessingService _videoService;
    private readonly INowPaymentsService _paymentService;
    private readonly AppDbContext _dbContext;

    private readonly Dictionary<long, string> userStates = new Dictionary<long, string>();

    public TelegramBotService(string token, IVideoProcessingService videoService, INowPaymentsService paymentService)
    {
        _botClient = new TelegramBotClient(token);
        _videoService = videoService;
        _paymentService = paymentService;
        // _dbContext = dbContext;
    }

    public void Start()
    {
        _botClient.StartReceiving(HandleUpdateAsync, HandleErrorAsync);
    }

    private async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Message != null && update.Message.Text != null)
        {
            long chatId = update.Message.Chat.Id;

            // Проверяем, что пользователь ввел Payment ID
            if (userStates.ContainsKey(chatId) && userStates[chatId] == "waiting_for_payment_id")
            {
                string paymentId = update.Message.Text.Trim(); // Это введенный Payment ID
                await HandleCheckPaymentStatusCallback(chatId, api_key_nowpayments, paymentId);

                // После ввода Payment ID очищаем состояние пользователя
                userStates[chatId] = null;
            }
            else if (update.Message.Text.StartsWith("/start"))
            {
                await HandleStartCommand(chatId);
            }
            else if (update.Message.Text.StartsWith("/menu"))
            {
                await ShowMainMenu(chatId);
            }
        }
        else if (update.CallbackQuery != null)
        {
            long chatId = update.CallbackQuery.Message.Chat.Id;

            string callbackData = update.CallbackQuery.Data;


            if (callbackData == "create_payment")
            {
                await HandleCreatePaymentCallback(chatId, api_key_nowpayments);
            }
            else if (callbackData == "check_payment_status")
            {
                // Меняем состояние пользователя на ожидание ввода Payment ID
                userStates[chatId] = "waiting_for_payment_id";
                await _botClient.SendTextMessageAsync(chatId, "Пожалуйста, введите ваш Payment ID в чат что мы выдали вам при оплате.");
            }
            else if (callbackData == "back_to_menu")
            {
                await ShowMainMenu(chatId);
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
            InlineKeyboardButton.WithCallbackData("Узнать статус платежа", "check_payment_status")
        }
    });

        await _botClient.SendTextMessageAsync(chatId, "Выберите действие:", replyMarkup: mainMenu);
    }

    private async Task HandleCreatePaymentCallback(long chatId, string apiKey)
    {
        var menu = new InlineKeyboardMarkup(new[]
        {
        InlineKeyboardButton.WithCallbackData("Назад в меню", "back_to_menu")
    });

        string amount = "10"; // Пример суммы
        string currency = "usdtbsc"; // Пример валюты
        string orderId = Guid.NewGuid().ToString(); // Пример ID заказа

        var paymentResponse = await _paymentService.CreatePayment(apiKey, amount, currency, orderId);

        if (paymentResponse != null)
        {
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
        }
        else
        {
            await _botClient.SendTextMessageAsync(chatId, "Ошибка при создании платежа.", replyMarkup: menu);
        }
    }

    private async Task HandleCheckPaymentStatusCallback(long chatId, string apiKey, string paymentId)
    {
        var menu = new InlineKeyboardMarkup(new[]
        {
        InlineKeyboardButton.WithCallbackData("Назад в меню", "back_to_menu")
    });

        if (string.IsNullOrWhiteSpace(paymentId))
        {
            await _botClient.SendTextMessageAsync(chatId, "Вы не ввели Payment ID. Пожалуйста, отправьте правильный Payment ID.", replyMarkup: menu);
            return;
        }

        var paymentStatusResponse = await _paymentService.GetPaymentStatus(apiKey, paymentId);

        if (paymentStatusResponse != null)
        {
            var message = $"<b>Статус платежа:</b> {paymentStatusResponse.PaymentStatus}\n" +
                          $"<b>Адрес для оплаты:</b> {paymentStatusResponse.PayAddress}\n" +
                          $"<b>Сумма платежа:</b> {paymentStatusResponse.PayAmount} {paymentStatusResponse.PayCurrency}\n" +
                          $"<b>Оплачено:</b> {paymentStatusResponse.ActuallyPaid} {paymentStatusResponse.PayCurrency}\n" +
                          $"<b>Дата создания:</b> {paymentStatusResponse.CreatedAt}\n\n" +
                          $"<i>Платеж выполнен в валюте {paymentStatusResponse.OutcomeCurrency}. Если статус 'finished', ваш платеж завершен.</i>";

            await _botClient.SendTextMessageAsync(
                chatId,
                message,
                replyMarkup: menu,
                parseMode: Telegram.Bot.Types.Enums.ParseMode.Html
            );
        }
        else
        {
            await _botClient.SendTextMessageAsync(chatId, "Ошибка при проверке статуса платежа. Убедитесь, что вы ввели правильный Payment ID.", replyMarkup: menu);
        }
    }

    private Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine($"Ошибка: {exception.Message}");
        return Task.CompletedTask;
    }
}