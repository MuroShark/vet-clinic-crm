namespace VetClinicBackend.Models
{
    /// <summary>Учётная запись пользователя системы (связана с сотрудником 1:1).</summary>
    public class AppUser
    {
        /// <summary>Уникальный идентификатор учетной записи пользователя.</summary>
        public int UserId { get; set; }
        /// <summary>Внешний ключ: идентификатор сотрудника, к которому привязана учетная запись.</summary>
        public int EmployeeId { get; set; }
        /// <summary>Логин пользователя для входа в систему.</summary>
        public string Login { get; set; } = string.Empty;
        /// <summary>Хэш пароля пользователя.</summary>
        public string PasswordHash { get; set; } = string.Empty;
        /// <summary>Дата и время последнего входа в систему.</summary>
        public DateTime? LastLoginAt { get; set; }
        /// <summary>Флаг блокировки учетной записи (true - заблокирован, false - активен).</summary>
        public bool IsLocked { get; set; }
        /// <summary>Токен оптимистичной блокировки (EF Core rowversion).</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        /// <summary>Навигационное свойство: сотрудник, к которому привязана учетная запись.</summary>
        public Employee Employee { get; set; } = null!;
        /// <summary>Навигационное свойство: список ролей, назначенных пользователю.</summary>
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
        /// <summary>Навигационное свойство: список цен на услуги, созданных данным пользователем.</summary>
        public ICollection<ServicePrice> CreatedServicePrices { get; set; } = new List<ServicePrice>();
        /// <summary>Навигационное свойство: список приемов, созданных данным пользователем.</summary>
        public ICollection<Appointment> CreatedAppointments { get; set; } = new List<Appointment>();
        /// <summary>Навигационное свойство: список платежей, обработанных данным пользователем.</summary>
        public ICollection<Payment> ProcessedPayments { get; set; } = new List<Payment>();
    }
}
