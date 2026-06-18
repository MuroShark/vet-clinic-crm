namespace VetClinicBackend.Models
{
    /// <summary>
    /// Приём (визит пациента). Статусы: Planned → InProgress → Closed / Cancelled.
    /// </summary>
    public class Appointment
    {
        /// <summary>Уникальный идентификатор приема.</summary>
        public int AppointmentId { get; set; }
        /// <summary>Дата и время начала приема.</summary>
        public DateTime AppointmentDate { get; set; }
        /// <summary>Продолжительность приема в минутах.</summary>
        public int? DurationMinutes { get; set; }
        /// <summary>Внешний ключ: идентификатор пациента.</summary>
        public int PatientId { get; set; }
        /// <summary>Внешний ключ: идентификатор врача (сотрудника), проводящего прием.</summary>
        public int DoctorEmployeeId { get; set; }
        /// <summary>Внешний ключ: идентификатор филиала, в котором проходит прием.</summary>
        public int BranchId { get; set; }
        /// <summary>Статус приема (Planned, InProgress, Closed, Cancelled).</summary>
        public string Status { get; set; } = "Planned";
        /// <summary>Комментарий врача к приему.</summary>
        public string? DoctorComment { get; set; }
        /// <summary>Общая сумма к оплате за прием.</summary>
        public decimal TotalAmount { get; set; }
        /// <summary>Дата и время закрытия (завершения) приема.</summary>
        public DateTime? ClosedAt { get; set; }
        /// <summary>Внешний ключ: идентификатор пользователя, создавшего запись о приеме.</summary>
        public int CreatedByUserId { get; set; }
        /// <summary>Дата и время создания записи о приеме.</summary>
        public DateTime CreatedAt { get; set; }
        /// <summary>Токен оптимистичной блокировки (EF Core rowversion).</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        /// <summary>Навигационное свойство: пациент, записанный на прием.</summary>
        public Patient Patient { get; set; } = null!;
        /// <summary>Навигационное свойство: врач, проводящий прием.</summary>
        public Employee Doctor { get; set; } = null!;
        /// <summary>Навигационное свойство: филиал клиники.</summary>
        public Branch Branch { get; set; } = null!;
        /// <summary>Навигационное свойство: пользователь, создавший запись.</summary>
        public AppUser CreatedByUser { get; set; } = null!;
        /// <summary>Навигационное свойство: список услуг, оказанных в рамках приема.</summary>
        public ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();
        /// <summary>Навигационное свойство: список материалов, использованных в рамках приема.</summary>
        public ICollection<AppointmentMaterial> AppointmentMaterials { get; set; } = new List<AppointmentMaterial>();
        /// <summary>Навигационное свойство: список диагнозов, поставленных на приеме.</summary>
        public ICollection<AppointmentDiagnosis> AppointmentDiagnoses { get; set; } = new List<AppointmentDiagnosis>();
        /// <summary>Навигационное свойство: список платежей по данному приему.</summary>
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
