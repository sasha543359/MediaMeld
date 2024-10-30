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
        try
        {
            using HttpClient client = new HttpClient();

            // Построение запроса с корректной строкой запроса
            var request = new HttpRequestMessage
            {
                Method = HttpMethod.Get,
                RequestUri = new Uri($"https://tiktok-api23.p.rapidapi.com/api/download/video?url={Uri.EscapeDataString(videoUrl)}"),
                Headers =
            {
                { "x-rapidapi-key", "6aebd4a500mshea1841310a4bb23p157005jsn21e83c6ca51c" },  // Ваш API ключ
                { "x-rapidapi-host", "tiktok-api23.p.rapidapi.com" }
            }
            };

            // Выполнение запроса
            using var response = await client.SendAsync(request);
            response.EnsureSuccessStatusCode();

            // Чтение и парсинг ответа
            var jsonResponse = await response.Content.ReadAsStringAsync();
            var jsonData = JObject.Parse(jsonResponse);

            // Теперь используем ключ "play" для получения ссылки на видео
            if (jsonData.ContainsKey("play"))
            {
                string downloadUrl = jsonData["play"].ToString();
                Console.WriteLine("Ссылка для скачивания видео: " + downloadUrl);
                return downloadUrl;
            }
            else
            {
                Console.WriteLine("Ссылка для скачивания не найдена.");
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при получении ссылки на скачивание: {ex.Message}");
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

            var TempVideoPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid()}_final_video.mp4");

            FFMpegArguments
                .FromFileInput(backgroundVideoPath)  // Фон: видео
                .AddFileInput(overlayVideoPath)      // Видеоролик с зелёным фоном
                .OutputToFile(TempVideoPath, true, options => options
                    .WithCustomArgument($"-filter_complex \"{filterComplex}\"")
                    .OverwriteExisting() // Перезаписываем, если файл уже существует
                )
                .ProcessSynchronously();  // Запуск синхронного процесса

            Console.WriteLine("Полные циклы обработаны.");

            // Этап 2: Если оставшееся время больше 1 секунды, добавляем хромакейное видео
            if (remainingSeconds >= 1)
            {
                // Получаем путь к системной временной директории
                string tempFolder = Path.GetTempPath();
                string trimmedOverlayPath = Path.Combine(tempFolder, "trimmed_overlay.mp4");

                // Удаляем временный файл, если он существует
                if (File.Exists(trimmedOverlayPath))
                {
                    File.Delete(trimmedOverlayPath);
                }

                // Обрезаем хромакейное видео до нужной продолжительности
                FFMpegArguments
                    .FromFileInput(overlayVideoPath)
                    .OutputToFile(trimmedOverlayPath, true, options => options
                        .WithCustomArgument($"-t {remainingSeconds}") // Обрезаем до оставшегося времени
                        .OverwriteExisting())
                    .ProcessSynchronously();

                // Удаляем выходной файл, если он уже существует
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                // Применяем фильтр с обрезанным видео для оставшегося времени, наложив его на фон
                FFMpegArguments
                    .FromFileInput(TempVideoPath)           // Обработанное временное видео
                    .AddFileInput(trimmedOverlayPath)       // Обрезанное хромакейное видео
                    .OutputToFile(outputPath, true, options => options
                        .WithCustomArgument($"-filter_complex \"[0:v]setpts=PTS-STARTPTS[main];[1:v]setpts=PTS-STARTPTS[ckout];[main][ckout]overlay=0:0:enable='gte(t,{lastFullLoopEndTime})'\"")
                        .WithCustomArgument("-shortest")
                        .OverwriteExisting())
                    .ProcessSynchronously();

                Console.WriteLine("Обрезанное видео добавлено.");
            }
            else
            {
                // Если оставшееся время меньше или равно 1 секунде, просто сохраняем результат как итоговый файл
                if (File.Exists(outputPath))
                {
                    File.Delete(outputPath);
                }

                File.Move(TempVideoPath, outputPath);
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