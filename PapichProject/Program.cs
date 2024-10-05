using System;
using System.Net.Http;  // Для работы с HTTP-запросами
using System.IO;  // Для работы с файловой системой
using System.Threading.Tasks;  // Для асинхронных операций
using FFMpegCore;  // Для работы с видео
using InstaSharper.API;  // Для загрузки видео в Instagram
using InstaSharper.API.Builder;  // Для создания сессии Instagram
using InstaSharper.Classes;  // Для работы с данными сессии и видеофайлами
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace PapichProject;

internal class Program
{
    static async Task Main(string[] args)
    {
        string video = $"C:\\Users\\sasha\\Desktop\\video.mp4";  // Указываем путь для сохранения видео

        Console.WriteLine("Введите ссылку на видео с TikTok:");
        string videoUrl = Console.ReadLine();  // Чтение ссылки на видео из консоли

        string downloadUrl = await GetTikTokDownloadUrl(videoUrl);  // Получение ссылки для скачивания видео через API
        if (!string.IsNullOrEmpty(downloadUrl))
        {
            await DownloadVideo(downloadUrl, video);  // Скачивание видео по полученной ссылке
        }
        else
        {
            Console.WriteLine("Не удалось получить ссылку для скачивания видео.");
        }

        ProcessVideo(video, $"C:\\Users\\sasha\\Desktop\\video1.mp4");
    }

    // Метод для получения ссылки на скачивание видео через API
    static async Task<string> GetTikTokDownloadUrl(string videoUrl)
    {
        using HttpClient client = new HttpClient();
        string apiUrl = "https://tiktok-video-downloader-api.p.rapidapi.com/media";  // URL API для получения ссылки на скачивание

        // Настройка заголовков для API запроса
        client.DefaultRequestHeaders.Add("x-rapidapi-key", "ed5c3a695bmsh55130b52fc4014ap1e8e0fjsnb4e3d709d29a");  // Вставь сюда свой API ключ
        client.DefaultRequestHeaders.Add("x-rapidapi-host", "tiktok-video-downloader-api.p.rapidapi.com");

        // Настройка параметров запроса
        var query = new UriBuilder(apiUrl);
        query.Query = $"videoUrl={videoUrl}";

        try
        {
            // Выполнение запроса
            HttpResponseMessage response = await client.GetAsync(query.Uri);
            response.EnsureSuccessStatusCode();  // Проверка успешного выполнения запроса

            // Чтение результата как строки
            string jsonResponse = await response.Content.ReadAsStringAsync();
            var jsonData = JObject.Parse(jsonResponse);

            // Извлечение ссылки на скачивание из ответа API
            string downloadUrl = jsonData["downloadUrl"].ToString();
            Console.WriteLine("Ссылка для скачивания видео: " + downloadUrl);

            return downloadUrl;  // Возвращаем ссылку для скачивания
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении ссылки: {ex.Message}");
            return null;
        }
    }

    // Метод для скачивания видео по полученной ссылке
    static async Task DownloadVideo(string downloadUrl, string savePath)
    {
        try
        {
            // Инициализация HTTP клиента для скачивания видео
            using HttpClient client = new HttpClient();

            // Асинхронный запрос на скачивание контента по ссылке
            using HttpResponseMessage response = await client.GetAsync(downloadUrl);
            response.EnsureSuccessStatusCode();  // Проверка успешного ответа

            // Чтение ответа как потока данных
            await using Stream streamToReadFrom = await response.Content.ReadAsStreamAsync();

            // Убедимся, что директория существует
            string directoryPath = Path.GetDirectoryName(savePath);
            if (!Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);  // Создаем директорию, если она отсутствует
            }

            // Открытие файла для записи
            await using FileStream streamToWriteTo = new FileStream(savePath, FileMode.Create, FileAccess.Write, FileShare.None);

            // Копирование данных из потока в файл
            await streamToReadFrom.CopyToAsync(streamToWriteTo);

            // Сообщение о завершении скачивания
            Console.WriteLine("Видео успешно скачано и сохранено.");
        }
        catch (UnauthorizedAccessException ex)
        {
            Console.WriteLine($"Ошибка доступа к директории или файлу: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Произошла ошибка при скачивании видео: {ex.Message}");
        }
    }

    static void ProcessVideo(string inputVideoPath, string outputVideoPath)
    {
        Thread.Sleep(3000);

        // https://www.gyan.dev/ffmpeg/builds/
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
}