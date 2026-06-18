namespace AhillBackend.Models
{
    /// <summary>Связь пользователь — роль (RBAC, многие-ко-многим).</summary>
    public class UserRole
    {
        /// <summary>Уникальный идентификатор связи пользователя и роли.</summary>
        public int UserRoleId { get; set; }
        /// <summary>Внешний ключ: идентификатор пользователя.</summary>
        public int UserId { get; set; }
        /// <summary>Внешний ключ: идентификатор роли.</summary>
        public int RoleId { get; set; }

        /// <summary>Навигационное свойство: пользователь системы.</summary>
        public AppUser User { get; set; } = null!;
        /// <summary>Навигационное свойство: назначенная роль.</summary>
        public Role Role { get; set; } = null!;
    }
}
