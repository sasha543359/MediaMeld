namespace Domain.Models;

public class User
{
    public Guid UserId { get; set; } // Уникальный идентификатор пользователя (GUID)
    public long TelegramId { get; set; } // Telegram ID пользователя
    public DateTime SubscriptionExpiresAt { get; set; } // Когда истекает подписка
    public int DailyVideoUploadCount { get; set; } // Количество загруженных видео за день
    public DateTime LastUploadDate { get; set; } // Дата последней загруженной видео
    public ICollection<Video> Videos { get; set; } // Список видео, загруженных пользователем

    // Связь с платежами (один ко многим)
    public ICollection<Payment> Payments { get; set; } // Коллекция платежей пользователя
}