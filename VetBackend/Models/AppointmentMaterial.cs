namespace VetClinicBackend.Models
{
    /// <summary>Материал, списанный в рамках приёма (со снимком цен на момент закрытия).</summary>
    public class AppointmentMaterial
    {
        /// <summary>Уникальный идентификатор записи о списанном материале.</summary>
        public int AppointmentMaterialId { get; set; }
        /// <summary>Внешний ключ: идентификатор приема.</summary>
        public int AppointmentId { get; set; }
        /// <summary>Внешний ключ: идентификатор партии материала на складе.</summary>
        public int MaterialBatchId { get; set; }
        /// <summary>Внешний ключ: идентификатор материала из справочника.</summary>
        public int MaterialId { get; set; }
        /// <summary>Количество списанного материала.</summary>
        public int Quantity { get; set; } = 1;
        /// <summary>Закупочная себестоимость единицы материала на момент списания (исторический снимок).</summary>
        public decimal UnitCostSnapshot { get; set; }
        /// <summary>Цена реализации единицы материала клиенту на момент списания (исторический снимок).</summary>
        public decimal ClientPriceSnapshot { get; set; }
        /// <summary>Общая стоимость списанного материала для клиента по данной позиции.</summary>
        public decimal LineTotal { get; set; }

        /// <summary>Навигационное свойство: прием, в рамках которого списан материал.</summary>
        public Appointment Appointment { get; set; } = null!;
        /// <summary>Навигационное свойство: партия материала на складе.</summary>
        public MaterialBatch MaterialBatch { get; set; } = null!;
    }
}
