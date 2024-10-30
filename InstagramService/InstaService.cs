using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace InstagramService
{
    public class InstaService
    {
        private readonly HttpClient _httpClient;

        public InstaService()
        {
            _httpClient = new HttpClient();
        }

        public async Task<bool> UploadVideoAsync(string videoPath, string thumbnailPath, string caption)
        {
            if (!File.Exists(videoPath))
            {
                Console.WriteLine("Файл видео не найден.");
                return false;
            }

            if (!File.Exists(thumbnailPath))
            {
                Console.WriteLine("Файл миниатюры не найден.");
                return false;
            }

            // Пример с логированием заголовков
            var headers = new Dictionary<string, string>
            {
                { "User-Agent", "Instagram 123.0.0.21.114 Android (28/9; 320dpi; 720x1280; Xiaomi; Mi 4; m4; qcom; en_US)" },
                { "X-IG-Capabilities", "3brTvw==" },
                { "X-IG-Connection-Type", "WIFI" }
            };

            var uploadUrl = "https://i.instagram.com/api/v1/upload/video/";

            // Логируем отправку запроса
            Console.WriteLine("Request: POST " + uploadUrl);
            foreach (var header in headers)
            {
                Console.WriteLine($"{header.Key}: {header.Value}");
            }

            // Установим заголовки в клиент
            foreach (var header in headers)
            {
                _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
            }

            try
            {
                var videoUpload = new InstaVideoUpload
                {
                    Video = new InstaVideo(videoPath, 1080, 1920), // Путь к видео
                    VideoThumbnail = new InstaImage(thumbnailPath, 1080, 1920) // Путь к миниатюре
                };

                // Подготовка данных для запроса
                var content = new MultipartFormDataContent
                {
                    { new StringContent(caption), "caption" },
                    { new ByteArrayContent(File.ReadAllBytes(videoPath)), "video" },
                    { new ByteArrayContent(File.ReadAllBytes(thumbnailPath)), "thumbnail" }
                };

                // Отправляем запрос
                var response = await _httpClient.PostAsync(uploadUrl, content);

                var responseContent = await response.Content.ReadAsStringAsync();

                // Логируем ответ
                Console.WriteLine("Response Content:");
                Console.WriteLine(responseContent);

                // Проверка успешности ответа
                if (response.IsSuccessStatusCode)
                {
                    var result = JsonConvert.DeserializeObject<InstaResponse>(responseContent);
                    if (result != null && result.Status == "ok")
                    {
                        Console.WriteLine("Видео успешно загружено.");
                        return true;
                    }
                }
                else
                {
                    Console.WriteLine("Ошибка загрузки видео: " + responseContent);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Ошибка: " + ex.Message);
            }

            return false;
        }
    }

    // Пример классов для обработки ответа (для десериализации JSON-ответа)
    public class InstaResponse
    {
        public string Status { get; set; }
    }

    public class InstaVideoUpload
    {
        public InstaVideo Video { get; set; }
        public InstaImage VideoThumbnail { get; set; }
    }

    public class InstaVideo
    {
        public string Path { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public InstaVideo(string path, int width, int height)
        {
            Path = path;
            Width = width;
            Height = height;
        }
    }

    public class InstaImage
    {
        public string Path { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public InstaImage(string path, int width, int height)
        {
            Path = path;
            Width = width;
            Height = height;
        }
    }
}
