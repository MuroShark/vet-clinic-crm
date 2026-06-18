namespace VetClinicBackend.Models
{
    /// <summary>Расходный материал или препарат.</summary>
    public class Material
    {
        /// <summary>Уникальный идентификатор материала или препарата в справочнике.</summary>
        public int MaterialId { get; set; }
        /// <summary>Наименование материала.</summary>
        public string Name { get; set; } = string.Empty;
        /// <summary>Единица измерения материала (шт., мл, мг и т.д.).</summary>
        public string? UnitOfMeasure { get; set; }
        /// <summary>Код категории материала.</summary>
        public string? CategoryCode { get; set; }
        /// <summary>Флаг активности материала в справочнике.</summary>
        public bool IsActive { get; set; } = true;

        /// <summary>Навигационное свойство: партии данного материала на складах.</summary>
        public ICollection<MaterialBatch> MaterialBatches { get; set; } = new List<MaterialBatch>();
    }
}
