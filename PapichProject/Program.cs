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
using VideoProcessing.Services;
using static System.Net.Mime.MediaTypeNames;

namespace VideoProcessing;

internal class Program
{
    static async Task Main(string[] args)
    {
        //string video = $"C:\\Users\\sasha\\Desktop\\video.mp4";  // Указываем путь для сохранения видео

        //Console.WriteLine("Введите ссылку на видео с TikTok:");
        //string videoUrl = Console.ReadLine();  // Чтение ссылки на видео из консоли

        //string downloadUrl = await VideoProcessingService.GetTikTokDownloadUrl(videoUrl);  // Получение ссылки для скачивания видео через API
        //if (!string.IsNullOrEmpty(downloadUrl))
        //{
        //    await VideoProcessingService.DownloadVideo(downloadUrl, video);  // Скачивание видео по полученной ссылке
        //}
        //else
        //{
        //    Console.WriteLine("Не удалось получить ссылку для скачивания видео.");
        //}

        ProcessVideo($"C:\\Users\\sasha\\Desktop\\hm.mp4", $"C:\\Users\\sasha\\Desktop\\video1.mp4");

        //VideoProcessingService.AddImageToVideo($"C:\\Users\\sasha\\Desktop\\video1.mp4",
        //                $"C:\\Users\\sasha\\Desktop\\win.png",
        //                $"C:\\Users\\sasha\\Desktop\\video2.mp4", 
        //                296, 132);

        //AddVideoWithChromaKey($"C:\\Users\\sasha\\Desktop\\hm.mp4",
        //                       $"C:\\Users\\sasha\\Desktop\\test1.mp4",
        //                       $"C:\\Users\\sasha\\Desktop\\new.mp4"); // 518, 1556



        AddVideoWithChromaKey($"C:\\Users\\sasha\\Desktop\\video1.mp4",
                              $"C:\\Users\\sasha\\Desktop\\testing.mov",
                              $"C:\\Users\\sasha\\Desktop\\result.mp4"); 
    }

    static public void AddVideoWithChromaKey(string backgroundVideoPath, string overlayVideoPath, string outputPath)
    {
        Thread.Sleep(4000);

        GlobalFFOptions.Configure(options => options.BinaryFolder = @"C:\Users\sasha\Desktop\ffmpeg-7.1-essentials_build\bin");

        try
        {
            // Get background video information
            var backgroundInfo = FFProbe.Analyse(backgroundVideoPath);
            int backgroundWidth = backgroundInfo.PrimaryVideoStream.Width;
            int backgroundHeight = backgroundInfo.PrimaryVideoStream.Height;

            // Get overlay (green screen) video information
            var overlayInfo = FFProbe.Analyse(overlayVideoPath);
            int overlayWidth = overlayInfo.PrimaryVideoStream.Width;
            int overlayHeight = overlayInfo.PrimaryVideoStream.Height;
            TimeSpan overlayDuration = overlayInfo.Duration;

            // Calculate the number of loops for the overlay video
            TimeSpan backgroundDuration = backgroundInfo.Duration;
            int loopCount = (int)Math.Ceiling(backgroundDuration.TotalSeconds / overlayDuration.TotalSeconds);
            int overlayFrameCount = (int)Math.Ceiling(overlayDuration.TotalSeconds * 59.94);  // Assuming FPS = 59.94

            // Calculate the x and y position to center the overlay horizontally and place it at the bottom
            int xPos = (backgroundWidth - overlayWidth) / 2;
            int yPos = backgroundHeight - overlayHeight;

            // Set chroma key filter with adjusted xPos and yPos
            FFMpegArguments
                .FromFileInput(backgroundVideoPath)  // Background video
                .AddFileInput(overlayVideoPath)      // Overlay video (green screen)
                .OutputToFile(outputPath, true, options => options
                    .WithCustomArgument($"-filter_complex \"[1:v]loop=loop={loopCount - 1}:size={overlayFrameCount}:start=0, chromakey=0x00FF00:0.2:0.1[ckout];[0:v][ckout] overlay={xPos}:{yPos}, scale={backgroundWidth}:{backgroundHeight}\"")
                )
                .ProcessSynchronously();  // Run the process synchronously

            Console.WriteLine("Видео с хромакеем успешно добавлено.");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Ошибка при добавлении видео с хромакеем: {ex.Message}");
        }
    }

    static public TimeSpan GetVideoDuration(string videoPath)
    {
        var mediaInfo = FFProbe.Analyse(videoPath);
        return mediaInfo.Duration;
    }

    static public void ProcessVideo(string inputVideoPath, string outputVideoPath)
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