using KiotVietTool.Domain.Exceptions;

namespace KiotVietTool.Domain.Entities;

public sealed class TelegramConnection
{
    public const int NameMaxLength = 200;

    public int Id { get; private set; }
    public string EncryptedBotToken { get; private set; } = "";
    public string BotUsername { get; private set; } = "";
    public long ChatId { get; private set; }
    public string ChatTitle { get; private set; } = "";
    public DateTime ConnectedAtUtc { get; private set; }

    private TelegramConnection() { } // EF Core

    public static TelegramConnection Create(string encryptedBotToken, string botUsername, long chatId, string chatTitle, DateTime nowUtc)
    {
        var connection = new TelegramConnection();
        connection.Replace(encryptedBotToken, botUsername, chatId, chatTitle, nowUtc);
        return connection;
    }

    public void Replace(string encryptedBotToken, string botUsername, long chatId, string chatTitle, DateTime nowUtc)
    {
        if (string.IsNullOrEmpty(encryptedBotToken)) throw new DomainException("Bot Token không được để trống.");
        if (chatId == 0) throw new DomainException("Chưa xác định được cuộc trò chuyện Telegram.");

        EncryptedBotToken = encryptedBotToken;
        BotUsername = Truncate(botUsername);
        ChatId = chatId;
        ChatTitle = Truncate(chatTitle);
        ConnectedAtUtc = nowUtc;
    }

    static string Truncate(string value) => value.Length > NameMaxLength ? value[..NameMaxLength] : value;
}
