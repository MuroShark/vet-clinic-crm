using AhillBackend.Models;

namespace AhillBackend.Dtos
{
    /// <summary>
    /// Преобразование сущностей БД в DTO-контракт фронтенда:
    /// строковые идентификаторы вида «prefix-{int}», точные имена полей,
    /// вложенные массивы для приёмов.
    /// </summary>
    public static class Mapper
    {
        /// <summary>Сформировать строковый идентификатор вида «prefix-123».</summary>
        public static string Sid(string prefix, int id) => $"{prefix}-{id}";

        /// <summary>
        /// Перевести UTC-время в московское (UTC+3, постоянный сдвиг с 2014 г.)
        /// и вернуть в формате ISO без суффикса Z.
        /// </summary>
        public static string Msk(DateTime utc) =>
            DateTime.SpecifyKind(utc, DateTimeKind.Unspecified).AddHours(3).ToString("yyyy-MM-ddTHH:mm:ss");

        public static string? MskNullable(DateTime? utc) => utc.HasValue ? Msk(utc.Value) : null;

        /// <summary>Разобрать строковый идентификатор «prefix-123» обратно в int. Допускает и чистое число.</summary>
        public static int ParseId(string? sid)
        {
            if (string.IsNullOrWhiteSpace(sid)) return 0;
            var dash = sid.LastIndexOf('-');
            var tail = dash >= 0 ? sid[(dash + 1)..] : sid;
            return int.TryParse(tail, out var n) ? n : 0;
        }

        /// <summary>Код роли бэкенда → строка фронтенда.</summary>
        public static string RoleToFront(string code) => code switch
        {
            "Director"     => "director",
            "ChiefVet"     => "chief_vet",
            "Vet"          => "vet",
            "Receptionist" => "receptionist",
            _              => code.ToLowerInvariant()
        };

        /// <summary>Строка роли фронтенда → код бэкенда.</summary>
        public static string RoleToBack(string front) => front switch
        {
            "director"     => "Director",
            "chief_vet"    => "ChiefVet",
            "vet"          => "Vet",
            "receptionist" => "Receptionist",
            _              => front
        };

        public static object Branch(Branch b) => new
        {
            id = Sid("br", b.BranchId),
            name = b.Name,
            address = b.Address,
            phone = b.Phone,
            isActive = b.IsActive
        };

        public static object Position(Position p) => new
        {
            id = Sid("pos", p.PositionId),
            name = p.Name
        };

        public static object Employee(Employee e) => new
        {
            id = Sid("emp", e.EmployeeId),
            name = e.FullName,
            positionId = Sid("pos", e.PositionId),
            phone = e.Phone,
            email = e.Email,
            KPIRate = e.KPIRate ?? 0m,
            branchIds = new[] { Sid("br", e.BranchId) },
            status = e.IsActive ? "active" : "inactive"
        };

        public static object User(AppUser u) => new
        {
            id = Sid("usr", u.UserId),
            username = u.Login,
            employeeId = Sid("emp", u.EmployeeId),
            roles = u.UserRoles.Select(ur => RoleToFront(ur.Role.Code)).ToArray(),
            status = u.IsLocked ? "inactive" : "active"
        };

        /// <summary>Сборка полного имени клиента из частей.</summary>
        public static string ClientName(Client c) =>
            string.Join(' ', new[] { c.LastName, c.FirstName, c.MiddleName }
                .Where(s => !string.IsNullOrWhiteSpace(s)));

        public static object Client(Client c) => new
        {
            id = Sid("cl", c.ClientId),
            name = ClientName(c),
            phone = c.Phone,
            email = c.Email,
            consentSigned = c.ConsentSigned,
            createdAt = c.RegistrationDate.ToString("yyyy-MM-dd")
        };

        public static object Patient(Patient p) => new
        {
            id = Sid("pt", p.PatientId),
            clientId = Sid("cl", p.ClientId),
            name = p.PetName,
            species = p.Species?.Name ?? "",
            breed = p.Breed ?? "",
            gender = p.Gender ?? "M",
            birthDate = p.BirthDate?.ToString("yyyy-MM-dd") ?? "",
            weight = p.Weight ?? 0m,
            color = p.Color ?? ""
        };

        /// <summary>Локализация кодов категорий услуг и материалов.</summary>
        private static readonly Dictionary<string, string> CategoryRu = new()
        {
            ["THERAPY"]    = "Терапия",
            ["DIAG"]       = "Диагностика",
            ["DENTAL"]     = "Стоматология",
            ["SURGERY"]    = "Хирургия",
            ["DISPOSABLE"] = "Расходные материалы",
            ["DRUG"]       = "Препараты"
        };

        public static string Category(string? code) =>
            code != null && CategoryRu.TryGetValue(code, out var ru) ? ru : (code ?? "");

        public static object Service(Service s, decimal currentPrice) => new
        {
            id = Sid("srv", s.ServiceId),
            category = Category(s.CategoryCode),
            name = s.Name,
            defaultPrice = currentPrice
        };

        public static object ServicePrice(ServicePrice sp) => new
        {
            id = Sid("spr", sp.ServicePriceId),
            serviceId = Sid("srv", sp.ServiceId),
            price = sp.Price,
            activeFrom = sp.EffectiveFrom.ToString("yyyy-MM-dd")
        };

        public static object Material(Material m) => new
        {
            id = Sid("mat", m.MaterialId),
            name = m.Name,
            sku = $"MAT-{m.MaterialId:000}",
            unit = m.UnitOfMeasure ?? "шт",
            category = Category(m.CategoryCode)
        };

        public static object Batch(MaterialBatch b) => new
        {
            id = Sid("bat", b.MaterialBatchId),
            materialId = Sid("mat", b.MaterialId),
            supplier = b.SupplierName ?? "",
            purchasePrice = b.UnitCost,
            clientPrice = b.ClientPrice,
            totalQuantity = b.InitialQuantity,
            remainingQuantity = b.RemainingQuantity,
            expiryDate = b.ExpirationDate?.ToString("yyyy-MM-dd") ?? "",
            receivedAt = b.ReceiptDate.ToString("yyyy-MM-dd"),
            branchId = Sid("br", b.BranchId)
        };

        public static object Diagnosis(Diagnosis d) => new
        {
            id = Sid("dg", d.DiagnosisId),
            code = d.Code ?? "",
            name = d.Name,
            category = d.Category ?? ""
        };

        public static object Appointment(Appointment a) => new
        {
            id = Sid("apt", a.AppointmentId),
            clientId = Sid("cl", a.Patient?.ClientId ?? 0),
            patientId = Sid("pt", a.PatientId),
            vetId = Sid("emp", a.DoctorEmployeeId),
            branchId = Sid("br", a.BranchId),
            appointmentDate = a.AppointmentDate.ToString("yyyy-MM-dd"),
            timeSlot = a.AppointmentDate.ToString("HH:mm"),
            status = a.Status,
            notes = a.DoctorComment ?? "",
            services = a.AppointmentServices.Select(s => new
            {
                serviceId = Sid("srv", s.ServiceId),
                quantity = s.Quantity,
                priceSnapshot = s.PriceSnapshot
            }).ToArray(),
            materials = a.AppointmentMaterials.Select(m => new
            {
                materialId = Sid("mat", m.MaterialId),
                batchId = Sid("bat", m.MaterialBatchId),
                quantity = m.Quantity,
                unitCostSnapshot = m.UnitCostSnapshot,
                clientPriceSnapshot = m.ClientPriceSnapshot
            }).ToArray(),
            diagnoses = a.AppointmentDiagnoses.Select(d => new
            {
                diagnosisId = Sid("dg", d.DiagnosisId),
                isPreliminary = d.IsPreliminary,
                doctorComment = d.DoctorNote ?? ""
            }).ToArray(),
            totalAmount = a.TotalAmount,
            closedAt = MskNullable(a.ClosedAt),
            createdAt = a.CreatedAt.ToString("yyyy-MM-dd")
        };

        public static object Payment(Payment p) => new
        {
            id = Sid("pay", p.PaymentId),
            appointmentId = Sid("apt", p.AppointmentId),
            paymentDate = Msk(p.PaidAt),
            amount = p.Amount,
            method = p.PaymentMethod,
            notes = p.ReceiptNumber
        };
    }
}
