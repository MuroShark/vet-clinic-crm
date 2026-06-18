namespace VetClinicBackend.Models
{
    /// <summary>Вид животного (кошка, собака и т.д.).</summary>
    public class AnimalSpecies
    {
        /// <summary>Уникальный идентификатор вида животного.</summary>
        public int SpeciesId { get; set; }
        /// <summary>Название вида животного (например, кошка, собака).</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Навигационное свойство: список пациентов данного вида.</summary>
        public ICollection<Patient> Patients { get; set; } = new List<Patient>();
    }
}
