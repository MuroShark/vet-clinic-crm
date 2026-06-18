namespace AhillBackend.Models
{
    /// <summary>Филиал ветеринарной клиники.</summary>
    public class Branch
    {
        /// <summary>Уникальный идентификатор филиала.</summary>
        public int BranchId { get; set; }
        /// <summary>Название филиала.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Физический адрес филиала.</summary>
        public string Address { get; set; } = string.Empty;
        /// <summary>Контактный телефон филиала.</summary>
        public string Phone { get; set; } = string.Empty;
        /// <summary>Флаг активности филиала (true - действующий, false - закрыт).</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Навигационное свойство: список сотрудников, прикрепленных к филиалу.</summary>
        public ICollection<Employee> Employees { get; set; } = new List<Employee>();
        /// <summary>Навигационное свойство: список клиентов филиала.</summary>
        public ICollection<Client> Clients { get; set; } = new List<Client>();
        /// <summary>Навигационное свойство: партии материалов на складе филиала.</summary>
        public ICollection<MaterialBatch> MaterialBatches { get; set; } = new List<MaterialBatch>();
        /// <summary>Навигационное свойство: список приемов в данном филиале.</summary>
        public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}
