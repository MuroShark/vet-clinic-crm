namespace VetClinicBackend.Models
{
    /// <summary>Диагноз, поставленный в рамках приёма.</summary>
    public class AppointmentDiagnosis
    {
        /// <summary>Уникальный идентификатор записи о диагнозе на приеме.</summary>
        public int AppointmentDiagnosisId { get; set; }
        /// <summary>Внешний ключ: идентификатор приема.</summary>
        public int AppointmentId { get; set; }
        /// <summary>Внешний ключ: идентификатор диагноза из справочника.</summary>
        public int DiagnosisId { get; set; }
        /// <summary>Флаг предварительного диагноза: true — предварительный, false — окончательный.</summary>
        public bool IsPreliminary { get; set; }
        /// <summary>Заметка врача к поставленному диагнозу.</summary>
        public string? DoctorNote { get; set; }

        /// <summary>Навигационное свойство: прием, на котором поставлен диагноз.</summary>
        public Appointment Appointment { get; set; } = null!;
        /// <summary>Навигационное свойство: диагноз из справочника.</summary>
        public Diagnosis Diagnosis { get; set; } = null!;
    }
}
