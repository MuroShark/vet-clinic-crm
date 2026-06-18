namespace AhillBackend.Models
{
    /// <summary>Диагноз (справочник). Поддерживает иерархию через ParentDiagnosisId.</summary>
    public class Diagnosis
    {
        /// <summary>Уникальный идентификатор диагноза в справочнике.</summary>
        public int DiagnosisId { get; set; }
        /// <summary>Международный или внутренний код диагноза.</summary>
        public string? Code { get; set; }
        /// <summary>Наименование диагноза.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Категория или группа заболеваний.</summary>
        public string? Category { get; set; }
        /// <summary>Внешний ключ: идентификатор родительского диагноза (для иерархии).</summary>
        public int? ParentDiagnosisId { get; set; }
        /// <summary>Флаг активности диагноза в справочнике.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Навигационное свойство: родительский диагноз.</summary>
        public Diagnosis? ParentDiagnosis { get; set; }
        /// <summary>Навигационное свойство: список дочерних диагнозов.</summary>
        public ICollection<Diagnosis> ChildDiagnoses { get; set; } = new List<Diagnosis>();
        /// <summary>Навигационное свойство: список случаев постановки данного диагноза на приемах.</summary>
        public ICollection<AppointmentDiagnosis> AppointmentDiagnoses { get; set; } = new List<AppointmentDiagnosis>();
    }
}
