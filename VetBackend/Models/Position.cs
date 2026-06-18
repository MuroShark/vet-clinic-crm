namespace VetClinicBackend.Models
{
    /// <summary>Должность сотрудника. IsSystemUser = true означает наличие учётной записи в системе.</summary>
    public class Position
    {
        /// <summary>Уникальный идентификатор должности.</summary>
        public int PositionId { get; set; }
        /// <summary>Наименование должности.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Флаг, указывающий, требуется ли учетная запись в системе для данной должности.</summary>
        public bool IsSystemUser { get; set; }

        /// <summary>Навигационное свойство: список сотрудников на данной должности.</summary>
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    }
}
