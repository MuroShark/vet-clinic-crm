namespace AhillBackend.Models
{
    /// <summary>Платёж по приёму.</summary>
    public class Payment
    {
        /// <summary>Уникальный идентификатор платежа.</summary>
        public int PaymentId { get; set; }
        /// <summary>Внешний ключ: идентификатор приема, по которому произведена оплата.</summary>
        public int AppointmentId { get; set; }
        /// <summary>Сумма платежа.</summary>
        public decimal Amount { get; set; }
        /// <summary>Способ оплаты (например, cash, card, transfer).</summary>
        public string PaymentMethod { get; set; } = string.Empty;
        /// <summary>Дата и время совершения платежа.</summary>
        public DateTime PaidAt { get; set; }
        /// <summary>Номер квитанции или чека об оплате.</summary>
        public string? ReceiptNumber { get; set; }
        /// <summary>Внешний ключ: идентификатор пользователя, обработавшего платеж.</summary>
        public int ProcessedByUserId { get; set; }

        /// <summary>Навигационное свойство: оплаченный прием.</summary>
        public Appointment Appointment { get; set; } = null!;
        /// <summary>Навигационное свойство: пользователь, принявший платеж.</summary>
        public AppUser ProcessedByUser { get; set; } = null!;
    }
}
