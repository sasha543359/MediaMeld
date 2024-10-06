namespace TelegramBot;

public class UserSession
{
    public string MainVideoPath { get; set; }  // Путь к основному видео
    public string ChromaVideoPath { get; set; }  // Путь к видео с хромакеем
    public string Status { get; set; }  // Статус сессии пользователя
    public long? LastMessageId { get; set; }  // Последний ID сообщения для удаления
}