namespace AhillBackend.Models
{
    /// <summary>Исторический прейскурант услуги. Текущая цена — запись с EffectiveTo = null.</summary>
    public class ServicePrice
    {
        /// <summary>Уникальный идентификатор записи о цене на услугу.</summary>
        public int ServicePriceId { get; set; }
        /// <summary>Внешний ключ: идентификатор услуги.</summary>
        public int ServiceId { get; set; }
        /// <summary>Стоимость услуги.</summary>
        public decimal Price { get; set; }
        /// <summary>Дата начала действия данной цены.</summary>
        public DateTime EffectiveFrom { get; set; }
        /// <summary>Дата окончания действия данной цены (null, если цена текущая).</summary>
        public DateTime? EffectiveTo { get; set; }
        /// <summary>Внешний ключ: идентификатор пользователя, установившего цену.</summary>
        public int CreatedByUserId { get; set; }
        /// <summary>Дата и время создания записи о цене.</summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>Навигационное свойство: услуга из прейскуранта.</summary>
        public Service Service { get; set; } = null!;
        /// <summary>Навигационное свойство: пользователь, установивший данную цену.</summary>
        public AppUser CreatedByUser { get; set; } = null!;
    }
}
