namespace Domain.Models;

public class Payment
{
    public Guid PaymentId { get; set; } // Уникальный идентификатор платежа (GUID)
    public Guid? UserId { get; set; } // Внешний ключ на пользователя, может быть null до момента создания пользователя
    public User User { get; set; } // Навигационное свойство пользователя

    public string PaymentStatus { get; set; } // Статус платежа
    public decimal PriceAmount { get; set; } // Сумма платежа
    public DateTime CreatedAt { get; set; } // Дата создания платежа
    public DateTime UpdatedAt { get; set; } // Дата обновления платежа
    public string PayAddress { get; set; } // Адрес для оплаты
    public decimal? ActuallyPaid { get; set; } // Сумма фактически уплаченных средств
    public string PayCurrency { get; set; } // Валюта оплаты
    public string PayinHash { get; set; } // Хэш транзакции
    public string OutcomeCurrency { get; set; } // Валюта, в которой были получены средства
}