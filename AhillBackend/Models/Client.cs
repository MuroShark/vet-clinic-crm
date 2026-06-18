namespace AhillBackend.Models
{
    /// <summary>Владелец животного (клиент клиники).</summary>
    public class Client
    {
        /// <summary>Уникальный идентификатор клиента.</summary>
        public int ClientId { get; set; }
        /// <summary>Фамилия клиента.</summary>
        public string LastName { get; set; } = string.Empty;
        /// <summary>Имя клиента.</summary>
        public string FirstName { get; set; } = string.Empty;
        /// <summary>Отчество клиента (при наличии).</summary>
        public string? MiddleName { get; set; }
        /// <summary>Контактный номер телефона клиента.</summary>
        public string Phone { get; set; } = string.Empty;
        /// <summary>Адрес электронной почты клиента.</summary>
        public string? Email { get; set; }
        /// <summary>Дата регистрации клиента в клинике.</summary>
        public DateTime RegistrationDate { get; set; }
        /// <summary>Внешний ключ: идентификатор филиала, к которому прикреплен клиент.</summary>
        public int BranchId { get; set; }
        /// <summary>Дополнительные заметки о клиенте.</summary>
        public string? Notes { get; set; }
        /// <summary>Флаг подписания согласия на обработку персональных данных (true - подписано).</summary>
        public bool ConsentSigned { get; set; }
        /// <summary>Флаг активности клиента.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Навигационное свойство: филиал, к которому прикреплен клиент.</summary>
        public Branch Branch { get; set; } = null!;
        /// <summary>Навигационное свойство: список питомцев (пациентов) клиента.</summary>
        public ICollection<Patient> Patients { get; set; } = new List<Patient>();
    }
}
