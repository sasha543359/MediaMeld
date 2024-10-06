using FFMpegCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoProcessing.Services;

public class VideoProcessingService : IVideoProcessingService
{
    public async Task<string> GetTikTokDownloadUrl(string videoUrl)
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

    public async Task DownloadVideo(string downloadUrl, string savePath)
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

    public void ProcessVideo(string inputVideoPath, string outputVideoPath)
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

    public void AddImageToVideo(string videoPath, string imagePath, string outputPath, int x, int y)
    {

        Thread.Sleep(3000);

        try
        {
            // Используем фильтр overlay для наложения изображения на видео по заданным координатам
            FFMpegArguments
                .FromFileInput(videoPath)  // Входное видео
                .AddFileInput(imagePath)    // Входное изображение
                .OutputToFile(outputPath, true, options => options
                    .WithCustomArgument($"-filter_complex \"[0:v][1:v] overlay={x}:{y}\"")  // Наложение изображения по координатам x:y
                )
                .ProcessSynchronously();  // Запуск синхронного процесса

            Console.WriteLine("Изображение успешно добавлено к видео.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при добавлении изображения: {ex.Message}");
        }
    }

    public void AddVideoWithChromaKey(string backgroundVideoPath, string overlayVideoPath, string outputPath, int x, int y)
    {
        Thread.Sleep(4000);

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
                    .WithCustomArgument($"-filter_complex \"[1:v]loop=loop={loopCount - 1}:size={overlayFrameCount}:start=0, chromakey=0x00FF00:0.2:0.1[ckout];[0:v][ckout] overlay=0:0, scale=1080:1920\"") // chromakey=0x00FF01:0.1:0.05[ckout];[0:v][ckout]
                )
                .ProcessSynchronously();  // Запуск синхронного процесса

            Console.WriteLine("Видео с хромакеем успешно добавлено.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при добавлении видео с хромакеем: {ex.Message}");
        }
    }

    public TimeSpan GetVideoDuration(string videoPath)
    {
        var mediaInfo = FFProbe.Analyse(videoPath);
        return mediaInfo.Duration;
    }
}