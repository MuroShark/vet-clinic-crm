namespace AhillBackend.Models
{
    /// <summary>Запись журнала аудита действий пользователей.</summary>
    public class AuditEntry
    {
        /// <summary>Уникальный идентификатор записи аудита.</summary>
        public int AuditEntryId { get; set; }
        /// <summary>Дата и время совершения действия.</summary>
        public DateTime Timestamp { get; set; }
        /// <summary>Имя пользователя (логин), совершившего действие.</summary>
        public string Username { get; set; } = string.Empty;
        /// <summary>Название совершенного действия.</summary>
        public string Action { get; set; } = string.Empty;
        /// <summary>Детальное описание совершенного действия и изменений.</summary>
        public string? Details { get; set; }
    }
}
