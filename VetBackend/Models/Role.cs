namespace VetClinicBackend.Models
{
    /// <summary>Роль RBAC (Director, ChiefVet, Vet, Receptionist).</summary>
    public class Role
    {
        /// <summary>Уникальный идентификатор роли в системе RBAC.</summary>
        public int RoleId { get; set; }
        /// <summary>Системный код роли (например, Director, Vet).</summary>
        public string Code { get; set; } = string.Empty;
        /// <summary>Отображаемое название роли.</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>Навигационное свойство: список связей пользователей с данной ролью.</summary>
        public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    }
}
