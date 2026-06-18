namespace VetClinicBackend.Models
{
    /// <summary>Услуга, оказанная в рамках приёма (со снимком цены на момент закрытия).</summary>
    public class AppointmentService
    {
        /// <summary>Уникальный идентификатор записи об оказанной услуге.</summary>
        public int AppointmentServiceId { get; set; }
        /// <summary>Внешний ключ: идентификатор приема.</summary>
        public int AppointmentId { get; set; }
        /// <summary>Внешний ключ: идентификатор услуги из справочника.</summary>
        public int ServiceId { get; set; }
        /// <summary>Количество оказанных услуг.</summary>
        public int Quantity { get; set; } = 1;
        /// <summary>Цена услуги на момент закрытия приема (исторический снимок).</summary>
        public decimal PriceSnapshot { get; set; }
        /// <summary>Общая стоимость услуги по данной позиции.</summary>
        public decimal LineTotal { get; set; }
        /// <summary>Внешний ключ: идентификатор сотрудника, выполнившего услугу (если отличается от врача приема).</summary>
        public int? PerformedByEmployeeId { get; set; }
        /// <summary>Примечание к оказанной услуге.</summary>
        public string? Note { get; set; }

        /// <summary>Навигационное свойство: прием, в рамках которого оказана услуга.</summary>
        public Appointment Appointment { get; set; } = null!;
        /// <summary>Навигационное свойство: оказанная услуга.</summary>
        public Service Service { get; set; } = null!;
        /// <summary>Навигационное свойство: сотрудник, выполнивший услугу.</summary>
        public Employee? PerformedByEmployee { get; set; }
    }
}
