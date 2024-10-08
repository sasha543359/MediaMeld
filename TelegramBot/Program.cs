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
using DataAccess;
using Microsoft.EntityFrameworkCore;



namespace TelegramBot;

internal class Program
{
    static async Task Main(string[] args)
    {
        // Ваш API-ключ для Telegram бота
        string token = "7946448900:AAGJZqNHXqTQ44XDW38zhdhHTmkkyu4Tjcg";

        //var connectionString = "Server=San4o\\SQLEXPRESS;Database=TelegramDB;trusted_connection=True;TrustServerCertificate=True;";

        //// Настраиваем параметры DbContext
        //var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        //optionsBuilder.UseSqlServer(connectionString);

        // Создаём экземпляр контекста базы данных
        //using (var dbContext = new AppDbContext(optionsBuilder.Options))
        //{
            // Создаём экземпляры сервисов для обработки видео и платежей
            IVideoProcessingService videoService = new VideoProcessingService();  // Реализация вашего сервиса видеообработки
            INowPaymentsService paymentService = new NowPaymentsService();        // Реализация вашего платежного сервиса

            // Создаём экземпляр TelegramBotService и передаём в него зависимости
            var telegramBotService = new TelegramBotService(token, videoService, paymentService);

            // Запускаем бота
            telegramBotService.Start();

            Console.WriteLine("Бот запущен. Нажмите Enter для завершения.");
            Console.ReadLine(); // Ожидание завершения программы
        // }
    }   
}