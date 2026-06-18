namespace VetClinicBackend.Models
{
    /// <summary>Животное (пациент клиники), привязанное к клиенту.</summary>
    public class Patient
    {
        /// <summary>Уникальный идентификатор пациента (животного).</summary>
        public int PatientId { get; set; }
        /// <summary>Кличка питомца.</summary>
        public string PetName { get; set; } = string.Empty;
        /// <summary>Внешний ключ: идентификатор клиента (владельца).</summary>
        public int ClientId { get; set; }
        /// <summary>Внешний ключ: идентификатор вида животного.</summary>
        public int SpeciesId { get; set; }
        /// <summary>Порода животного.</summary>
        public string? Breed { get; set; }
        /// <summary>Пол животного: M (самец) или F (самка).</summary>
        public string Gender { get; set; } = "M";
        /// <summary>Дата рождения животного.</summary>
        public DateOnly? BirthDate { get; set; }
        /// <summary>Вес животного в килограммах.</summary>
        public decimal? Weight { get; set; }
        /// <summary>Окрас животного.</summary>
        public string? Color { get; set; }
        /// <summary>Флаг активности карты пациента.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Навигационное свойство: владелец пациента.</summary>
        public Client Client { get; set; } = null!;
        /// <summary>Навигационное свойство: вид животного.</summary>
        public AnimalSpecies Species { get; set; } = null!;
        /// <summary>Навигационное свойство: список приемов данного пациента.</summary>
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
