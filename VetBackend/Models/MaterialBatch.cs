namespace VetClinicBackend.Models
{
    /// <summary>Партия материала на складе конкретного филиала.</summary>
    public class MaterialBatch
    {
        /// <summary>Уникальный идентификатор партии материала на складе.</summary>
        public int MaterialBatchId { get; set; }
        /// <summary>Внешний ключ: идентификатор материала из справочника.</summary>
        public int MaterialId { get; set; }
        /// <summary>Внешний ключ: идентификатор филиала (склада), где находится партия.</summary>
        public int BranchId { get; set; }
        /// <summary>Наименование поставщика партии.</summary>
        public string? SupplierName { get; set; }
        /// <summary>Номер партии или серии производителя.</summary>
        public string? BatchNumber { get; set; }
        /// <summary>Дата поступления партии на склад.</summary>
        public DateTime ReceiptDate { get; set; }
        /// <summary>Срок годности партии материала.</summary>
        public DateOnly? ExpirationDate { get; set; }
        /// <summary>Изначальное количество материала в партии при поступлении.</summary>
        public int InitialQuantity { get; set; }
        /// <summary>Текущий остаток материала в партии.</summary>
        public int RemainingQuantity { get; set; }
        /// <summary>Закупочная себестоимость единицы материала.</summary>
        public decimal UnitCost { get; set; }
        /// <summary>Розничная цена реализации единицы материала клиенту.</summary>
        public decimal ClientPrice { get; set; }
        /// <summary>Токен оптимистичной блокировки (EF Core rowversion).</summary>
        public byte[] RowVersion { get; set; } = Array.Empty<byte>();

        /// <summary>Навигационное свойство: материал из справочника.</summary>
        public Material Material { get; set; } = null!;
        /// <summary>Навигационное свойство: филиал, на складе которого находится партия.</summary>
        public Branch Branch { get; set; } = null!;
        /// <summary>Навигационное свойство: списания из данной партии в рамках приемов.</summary>
        public ICollection<AppointmentMaterial> AppointmentMaterials { get; set; } = new List<AppointmentMaterial>();
    }
}
