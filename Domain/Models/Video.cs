namespace Domain.Models;

public class Video
{
    public Guid VideoId { get; set; } // Уникальный идентификатор видео
    public Guid UserId { get; set; } // Ссылка на пользователя
    public User User { get; set; } // Навигационное свойство
    public string FilePath { get; set; } // Путь к файлу видео
    public DateTime UploadedAt { get; set; } // Время загрузки видео
    public TimeSpan Duration { get; set; } // Длительность видео
}