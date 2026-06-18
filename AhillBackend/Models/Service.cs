namespace AhillBackend.Models
{
    /// <summary>Ветеринарная услуга (терапия, диагностика, хирургия и т.д.).</summary>
    public class Service
    {
        /// <summary>Уникальный идентификатор ветеринарной услуги.</summary>
        public int ServiceId { get; set; }
        /// <summary>Наименование услуги.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Код категории услуги (терапия, хирургия и т.д.).</summary>
        public string? CategoryCode { get; set; }
        /// <summary>Стандартная продолжительность оказания услуги в минутах.</summary>
        public int DefaultDurationMin { get; set; }
        /// <summary>Флаг активности услуги в справочнике.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Навигационное свойство: история цен на данную услугу.</summary>
        public ICollection<ServicePrice> ServicePrices { get; set; } = new List<ServicePrice>();
        /// <summary>Навигационное свойство: список фактов оказания данной услуги на приемах.</summary>
        public ICollection<AppointmentService> AppointmentServices { get; set; } = new List<AppointmentService>();
    }
}
