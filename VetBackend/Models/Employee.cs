namespace VetClinicBackend.Models
{
    /// <summary>Сотрудник клиники.</summary>
    public class Employee
    {
        /// <summary>Уникальный идентификатор сотрудника.</summary>
        public int EmployeeId { get; set; }
        /// <summary>Полное имя (ФИО) сотрудника.</summary>
        public string FullName { get; set; } = string.Empty;
        /// <summary>Контактный телефон сотрудника.</summary>
        public string Phone { get; set; } = string.Empty;
        /// <summary>Электронная почта сотрудника.</summary>
        public string? Email { get; set; }
        /// <summary>Внешний ключ: идентификатор филиала, в котором работает сотрудник.</summary>
        public int BranchId { get; set; }
        /// <summary>Внешний ключ: идентификатор должности сотрудника.</summary>
        public int PositionId { get; set; }
        /// <summary>Дата приема сотрудника на работу.</summary>
        public DateOnly HireDate { get; set; }
        /// <summary>Дата увольнения сотрудника (если применимо).</summary>
        public DateOnly? DismissalDate { get; set; }
        /// <summary>Ставка KPI сотрудника (например, 0.25 = 25%). Значение null означает фиксированную ставку без KPI.</summary>
        public decimal? KPIRate { get; set; }
        /// <summary>Флаг активности сотрудника (работает или уволен).</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Навигационное свойство: филиал, к которому прикреплен сотрудник.</summary>
        public Branch Branch { get; set; } = null!;
        /// <summary>Навигационное свойство: должность сотрудника.</summary>
        public Position Position { get; set; } = null!;
        /// <summary>Навигационное свойство: учетная запись пользователя, связанная с сотрудником.</summary>
        public AppUser? User { get; set; }
        /// <summary>Навигационное свойство: список приемов, проведенных сотрудником в роли врача.</summary>
        public ICollection<Appointment> DoctorAppointments { get; set; } = new List<Appointment>();
        /// <summary>Навигационное свойство: список услуг, выполненных данным сотрудником.</summary>
        public ICollection<AppointmentService> PerformedServices { get; set; } = new List<AppointmentService>();
    }
}
