using FFMpegCore;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace VideoProcessing.Services;

public class VideoProcessingService : IVideoProcessingService
{
    static VideoProcessingService()
    {
        // https://www.gyan.dev/ffmpeg/builds/
        GlobalFFOptions.Configure(options => options.BinaryFolder = @"C:\Users\sasha\Desktop\ffmpeg-7.1-essentials_build\bin");
    }

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

        try
        {
            // Масштабирование до 1080x1920 и центрирование видео
            FFMpegArguments
                .FromFileInput(inputVideoPath)
                .OutputToFile(outputVideoPath, true, options => options
                    .WithCustomArgument("-vf \"scale=1080:-1,pad=1080:1920:(ow-iw)/2:(oh-ih)/2\"") // Масштабируем и центрируем видео
                    .WithFramerate(60)  // Устанавливаем 60 FPS
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

    public void AddVideoWithChromaKey(string backgroundVideoPath, string overlayVideoPath, string outputPath)
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

            // Рассчитываем количество полных повторов видео с хромакеем
            int fullLoopCount = (int)(backgroundDuration.TotalSeconds / overlayDuration.TotalSeconds);
            double remainingSeconds = backgroundDuration.TotalSeconds - (fullLoopCount * overlayDuration.TotalSeconds);

            Console.WriteLine($"Количество полных повторов: {fullLoopCount}");
            Console.WriteLine($"Остаток времени для последнего повтора: {remainingSeconds} секунд");

            // Округляем оставшееся время вниз для избежания лишних циклов
            remainingSeconds = Math.Floor(remainingSeconds);
            int lastFullLoopEndTime = (int)Math.Floor(fullLoopCount * overlayDuration.TotalSeconds);  // Округляем время окончания последнего полного повторения
            int roundedBackgroundDuration = (int)Math.Floor(backgroundDuration.TotalSeconds);  // Округляем длительность основного видео
            Console.WriteLine($"Время окончания последнего полного повторения: {lastFullLoopEndTime} секунд");

            // Округляем размер (количество кадров) до целого числа
            int overlayFrameCount = (int)Math.Round(overlayDuration.TotalSeconds * GetVideoFps(overlayVideoPath));
            Console.WriteLine($"Количество кадров для видео с хромакеем: {overlayFrameCount}");

            // Этап 1: Создаем фильтр с циклом для хромакея и fade по альфа-каналу
            string filterComplex = string.Empty;

            // Создаем циклы для повторов хромакейного видео с мгновенным исчезновением по альфа-каналу после последнего полного повтора
            filterComplex += $"[1:v]loop=loop={fullLoopCount - 1}:size={overlayFrameCount},chromakey=0x00FF00:0.2:0.1[ckout];" +
                             $"[ckout]fade=t=out:st={lastFullLoopEndTime}:d=0.01:alpha=1[fadeout];" +  // Мгновенное исчезновение после последнего полного повтора
                             $"[0:v][fadeout]overlay=0:0";

            // Этап 1: Запуск FFMpeg для полных циклов с fade
            string tempOutput = "temp_output.mp4";  // Временный файл для промежуточного результата

            // Удаляем временный файл, если он существует
            if (File.Exists(tempOutput))
            {
                File.Delete(tempOutput);
            }

            FFMpegArguments
                .FromFileInput(backgroundVideoPath)  // Фон: видео
                .AddFileInput(overlayVideoPath)      // Видеоролик с зелёным фоном
                .OutputToFile(tempOutput, true, options => options
                    .WithCustomArgument($"-filter_complex \"{filterComplex}\"")
                    .OverwriteExisting() // Перезаписываем, если файл уже существует
                )
                .ProcessSynchronously();  // Запуск синхронного процесса

            Console.WriteLine("Полные циклы обработаны.");

            // Этап 2: Если оставшееся время больше 1 секунды, добавляем хромакейное видео
            if (remainingSeconds >= 1)
            {
                // Если оставшегося времени больше, чем длительность хромакейного видео, обрезаем его
                double trimDuration = remainingSeconds > overlayDuration.TotalSeconds
                    ? overlayDuration.TotalSeconds
                    : remainingSeconds;

                // Обрезанное видео добавляется начиная с момента завершения последнего полного цикла
                string finalFilterComplex = $"[1:v]loop=1:size={(int)(trimDuration * GetVideoFps(overlayVideoPath))},setpts=PTS-STARTPTS," +
                                            $"chromakey=0x00FF00:0.2:0.1[ckout_remain];" +
                                            $"[0:v][ckout_remain]overlay=0:0:enable='gte(t,{lastFullLoopEndTime})'";

                // Удаляем выходной файл, если он уже существует
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                // Применяем дополнительный фильтр с обрезанным видео для оставшегося времени
                FFMpegArguments
                    .FromFileInput(tempOutput)       // Обработанное временное видео
                    .AddFileInput(overlayVideoPath)  // Хромакейное видео для обрезки
                    .OutputToFile(outputPath, true, options => options
                        .WithCustomArgument($"-filter_complex \"{finalFilterComplex}\"")
                        .OverwriteExisting() // Перезаписываем, если файл уже существует
                    )
                    .ProcessSynchronously();  // Запуск синхронного процесса

                Console.WriteLine("Обрезанное видео добавлено.");
            }
            else
            {
                // Если оставшееся время меньше или равно 1 секунде, просто сохраняем результат как итоговый файл
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                File.Move(tempOutput, outputPath);
            }

            Console.WriteLine("Видео с хромакеем успешно добавлено.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при добавлении видео с хромакеем: {ex.Message}");
        }
    }

    public double GetVideoFps(string videoPath)
    {
        // Используем FFmpeg для получения FPS видео через его метаданные
        var mediaInfo = FFProbe.Analyse(videoPath);
        return mediaInfo.PrimaryVideoStream.FrameRate;
    }

    public TimeSpan GetVideoDuration(string videoPath)
    {
        var mediaInfo = FFProbe.Analyse(videoPath);
        return mediaInfo.Duration;
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
}