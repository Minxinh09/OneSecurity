namespace OneSecurity.Server.Configuration
{
    public class TelegramOptions
    {
        public const string SectionName = "Telegram";

        public required string BotToken { get; set; }
        public required string DefaultChatId { get; set; }
    }
}
