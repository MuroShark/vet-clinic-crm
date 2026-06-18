using VetClinicBackend.Models;

namespace VetClinicBackend.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(AppDbContext context)
        {
            if (context.Branches.Any()) return;

            // Филиалы (адреса — из Приложения А диплома)
            var brCentral = new Branch { Name = "Центральный филиал", Address = "г. Москва, ул. Дубнинская, д. 32", Phone = "+7 (495) 555-0121", IsActive = true };
            var brNorth = new Branch { Name = "Северный филиал", Address = "г. Москва, ул. Белозерская, д. 17Г", Phone = "+7 (495) 555-0122", IsActive = true };
            context.Branches.AddRange(brCentral, brNorth);
            await context.SaveChangesAsync();

            // Должности. Порядок важен: Администратор = 4-я (не врач).
            var posChief = new Position { Name = "Главный врач", IsSystemUser = true };
            var posDirector = new Position { Name = "Директор клиники", IsSystemUser = true };
            var posTherapist = new Position { Name = "Врач-терапевт", IsSystemUser = true };
            var posAdmin = new Position { Name = "Администратор регистратуры", IsSystemUser = true };
            var posSurgeon = new Position { Name = "Врач-хирург", IsSystemUser = true };
            var posOncologist = new Position { Name = "Онколог", IsSystemUser = true };
            var posRatologist = new Position { Name = "Ратолог", IsSystemUser = true };
            context.Positions.AddRange(posChief, posDirector, posTherapist, posAdmin, posSurgeon, posOncologist, posRatologist);
            context.Positions.AddRange(
                new Position { Name = "Уборщица", IsSystemUser = false },
                new Position { Name = "Охрана", IsSystemUser = false });
            await context.SaveChangesAsync();

            var species = new[]
            {
                new AnimalSpecies { Name = "Собака"   },
                new AnimalSpecies { Name = "Кошка"    },
                new AnimalSpecies { Name = "Хорёк"    },
                new AnimalSpecies { Name = "Кролик"   },
                new AnimalSpecies { Name = "Попугай"  },
                new AnimalSpecies { Name = "Черепаха" }
            };
            context.AnimalSpecies.AddRange(species);
            await context.SaveChangesAsync();

            var roles = new[]
            {
                new Role { Code = "Director",     Name = "Директор"                  },
                new Role { Code = "ChiefVet",     Name = "Главный ветеринарный врач" },
                new Role { Code = "Vet",          Name = "Ветеринарный врач"         },
                new Role { Code = "Receptionist", Name = "Администратор"             }
            };
            context.Roles.AddRange(roles);
            await context.SaveChangesAsync();

            var services = new[]
            {
                new Service { Name = "Первичный приём",                CategoryCode = "THERAPY", DefaultDurationMin = 30, IsActive = true },
                new Service { Name = "Повторный приём",                CategoryCode = "THERAPY", DefaultDurationMin = 20, IsActive = true },
                new Service { Name = "Вакцинация",                     CategoryCode = "THERAPY", DefaultDurationMin = 15, IsActive = true },
                new Service { Name = "УЗИ органов брюшной полости",    CategoryCode = "DIAG",    DefaultDurationMin = 30, IsActive = true },
                new Service { Name = "Общий анализ крови",             CategoryCode = "DIAG",    DefaultDurationMin = 10, IsActive = true },
                new Service { Name = "Чистка зубов (ультразвук)",      CategoryCode = "DENTAL",  DefaultDurationMin = 60, IsActive = true },
                new Service { Name = "Кастрация (кот)",                CategoryCode = "SURGERY", DefaultDurationMin = 45, IsActive = true },
                new Service { Name = "Стерилизация (кошка)",           CategoryCode = "SURGERY", DefaultDurationMin = 60, IsActive = true }
            };
            context.Services.AddRange(services);
            await context.SaveChangesAsync();

            var diagnoses = new[]
            {
                new Diagnosis { Code = "ДЫХ",  Name = "Респираторные заболевания", Category = "Терапия",       IsActive = true },
                new Diagnosis { Code = "ЖКТ",  Name = "Заболевания ЖКТ",           Category = "Гастроэнтерология", IsActive = true },
                new Diagnosis { Code = "ДЕРМ", Name = "Кожные заболевания",        Category = "Дерматология",   IsActive = true },
                new Diagnosis { Code = "ОРТО", Name = "Ортопедические заболевания",Category = "Ортопедия",      IsActive = true },
                new Diagnosis { Code = "КАРД", Name = "Кардиомиопатия",            Category = "Кардиология",    IsActive = true },
                new Diagnosis { Code = "ПРОФ", Name = "Профилактика (здоров)",     Category = "Терапия",        IsActive = true }
            };
            context.Diagnoses.AddRange(diagnoses);
            await context.SaveChangesAsync();

            var materials = new[]
            {
                new Material { Name = "Шприц 2мл",                   UnitOfMeasure = "шт",   CategoryCode = "DISPOSABLE", IsActive = true },
                new Material { Name = "Шприц 5мл",                   UnitOfMeasure = "шт",   CategoryCode = "DISPOSABLE", IsActive = true },
                new Material { Name = "Перчатки нитриловые M",       UnitOfMeasure = "пара", CategoryCode = "DISPOSABLE", IsActive = true },
                new Material { Name = "Нобивак DHPPi",               UnitOfMeasure = "доза", CategoryCode = "DRUG",       IsActive = true },
                new Material { Name = "Нобивак Rabies",              UnitOfMeasure = "доза", CategoryCode = "DRUG",       IsActive = true },
                new Material { Name = "Физраствор NaCl 0.9% 200мл", UnitOfMeasure = "фл",   CategoryCode = "DRUG",       IsActive = true }
            };
            context.Materials.AddRange(materials);
            await context.SaveChangesAsync();

            // Инициализация сотрудников
            var hireDate = new DateOnly(2024, 1, 15);
            var empChief = new Employee { FullName = "Соколов Андрей Петрович", Phone = "+7 (495) 111-2201", Email = "sokolovap@vet-clinic.ru", BranchId = brCentral.BranchId, PositionId = posChief.PositionId, HireDate = hireDate, KPIRate = 0.35m, IsActive = true };
            var empDirector = new Employee { FullName = "Абрамов Дмитрий Александрович", Phone = "+7 (495) 111-2202", Email = "abramovda@vet-clinic.ru", BranchId = brCentral.BranchId, PositionId = posDirector.PositionId, HireDate = hireDate, KPIRate = 0.30m, IsActive = true };
            var empTherapist = new Employee { FullName = "Карабанова Елена Александровна", Phone = "+7 (495) 111-2203", Email = "karabanovaea@vet-clinic.ru", BranchId = brCentral.BranchId, PositionId = posTherapist.PositionId, HireDate = hireDate, KPIRate = 0.25m, IsActive = true };
            var empAdmin = new Employee { FullName = "Бенидзе Елизавета Игоревна", Phone = "+7 (495) 111-2204", Email = "benidzeei@vet-clinic.ru", BranchId = brCentral.BranchId, PositionId = posAdmin.PositionId, HireDate = hireDate, KPIRate = 0.00m, IsActive = true };
            var empSurgeon = new Employee { FullName = "Фридман Артём Игоревич", Phone = "+7 (495) 111-2205", Email = "fridmanai@vet-clinic.ru", BranchId = brCentral.BranchId, PositionId = posSurgeon.PositionId, HireDate = hireDate, KPIRate = 0.30m, IsActive = true };
            var empOncologist = new Employee { FullName = "Юпатова Екатерина Владимировна", Phone = "+7 (495) 111-2206", Email = "yupatovaev@vet-clinic.ru", BranchId = brCentral.BranchId, PositionId = posOncologist.PositionId, HireDate = hireDate, KPIRate = 0.28m, IsActive = true };
            var empRatologist = new Employee { FullName = "Брюхно Виктор Васильевич", Phone = "+7 (495) 111-2207", Email = "bryuhnovv@vet-clinic.ru", BranchId = brNorth.BranchId, PositionId = posRatologist.PositionId, HireDate = hireDate, KPIRate = 0.27m, IsActive = true };
            context.Employees.AddRange(empChief, empDirector, empTherapist, empAdmin, empSurgeon, empOncologist, empRatologist);
            await context.SaveChangesAsync();

            // === Учётные записи (пароль у всех демо-профилей: 123) ===
            var roleDirector = roles[0];
            var roleChief = roles[1];
            var roleVet = roles[2];
            var roleReception = roles[3];
            var pwd = Auth.PasswordHasher.Hash("123");

            var usrAbramov = new AppUser { EmployeeId = empDirector.EmployeeId, Login = "abramovda", PasswordHash = pwd, IsLocked = false };
            var usrBenidze = new AppUser { EmployeeId = empAdmin.EmployeeId, Login = "benidzeei", PasswordHash = pwd, IsLocked = false };
            var usrKarabanova = new AppUser { EmployeeId = empTherapist.EmployeeId, Login = "karabanovaea", PasswordHash = pwd, IsLocked = false };
            var usrSokolov = new AppUser { EmployeeId = empChief.EmployeeId, Login = "sokolovap", PasswordHash = pwd, IsLocked = false };
            var usrBryuhno = new AppUser { EmployeeId = empRatologist.EmployeeId, Login = "bryuhnovv", PasswordHash = pwd, IsLocked = false };
            context.Users.AddRange(usrAbramov, usrBenidze, usrKarabanova, usrSokolov, usrBryuhno);
            await context.SaveChangesAsync();

            // Назначение ролей (RBAC). abramovda — все 4 роли (системный администратор/демо).
            context.UserRoles.AddRange(
                new UserRole { UserId = usrAbramov.UserId, RoleId = roleDirector.RoleId },
                new UserRole { UserId = usrAbramov.UserId, RoleId = roleChief.RoleId },
                new UserRole { UserId = usrAbramov.UserId, RoleId = roleVet.RoleId },
                new UserRole { UserId = usrAbramov.UserId, RoleId = roleReception.RoleId },
                new UserRole { UserId = usrBenidze.UserId, RoleId = roleReception.RoleId },
                new UserRole { UserId = usrKarabanova.UserId, RoleId = roleVet.RoleId },
                new UserRole { UserId = usrSokolov.UserId, RoleId = roleVet.RoleId },
                new UserRole { UserId = usrSokolov.UserId, RoleId = roleChief.RoleId },
                new UserRole { UserId = usrBryuhno.UserId, RoleId = roleVet.RoleId });
            await context.SaveChangesAsync();

            // === Прейскурант (текущие цены услуг) ===
            decimal[] prices = { 950m, 600m, 1200m, 2000m, 1100m, 4500m, 2400m, 5200m };
            var effFrom = DateTime.UtcNow.AddMonths(-3);
            for (int i = 0; i < services.Length; i++)
            {
                context.ServicePrices.Add(new ServicePrice
                {
                    ServiceId = services[i].ServiceId,
                    Price = prices[i],
                    EffectiveFrom = effFrom,
                    EffectiveTo = null,
                    CreatedByUserId = usrAbramov.UserId,
                    CreatedAt = effFrom
                });
            }
            await context.SaveChangesAsync();

            // === Клиенты ===
            var clKuznetsov = new Client { LastName = "Кузнецов", FirstName = "Дмитрий", MiddleName = "Сергеевич", Phone = "+7 (921) 123-4567", Email = "dmitry.kuznetsov@mail.ru", RegistrationDate = DateTime.UtcNow.AddDays(-30), BranchId = brCentral.BranchId, ConsentSigned = true, IsActive = true };
            var clVasileva = new Client { LastName = "Васильева", FirstName = "Елена", MiddleName = "Игоревна", Phone = "+7 (952) 987-6543", Email = "elena.vas@gmail.com", RegistrationDate = DateTime.UtcNow.AddDays(-25), BranchId = brCentral.BranchId, ConsentSigned = true, IsActive = true };
            var clPetrov = new Client { LastName = "Петров", FirstName = "Сергей", MiddleName = "Иванович", Phone = "+7 (916) 234-5678", Email = "sergey.petrov@mail.ru", RegistrationDate = DateTime.UtcNow.AddDays(-20), BranchId = brNorth.BranchId, ConsentSigned = true, IsActive = true };
            context.Clients.AddRange(clKuznetsov, clVasileva, clPetrov);
            await context.SaveChangesAsync();

            // === Пациенты === (species: 0 Собака, 1 Кошка)
            var ptBarsik = new Patient { ClientId = clKuznetsov.ClientId, SpeciesId = species[1].SpeciesId, PetName = "Барсик", Breed = "Британская короткошёрстная", Gender = "M", BirthDate = new DateOnly(2022, 4, 12), Weight = 5.4m, Color = "Голубой окрас", IsActive = true };
            var ptAlfa = new Patient { ClientId = clKuznetsov.ClientId, SpeciesId = species[0].SpeciesId, PetName = "Альфа", Breed = "Немецкая овчарка", Gender = "F", BirthDate = new DateOnly(2020, 9, 24), Weight = 32.1m, Color = "Чепрачный окрас", IsActive = true };
            var ptSonya = new Patient { ClientId = clVasileva.ClientId, SpeciesId = species[1].SpeciesId, PetName = "Соня", Breed = "Сибирская", Gender = "F", BirthDate = new DateOnly(2022, 12, 5), Weight = 4.2m, Color = "Серая полосатая", IsActive = true };
            var ptReks = new Patient { ClientId = clPetrov.ClientId, SpeciesId = species[0].SpeciesId, PetName = "Рекс", Breed = "Лабрадор-ретривер", Gender = "M", BirthDate = new DateOnly(2021, 6, 30), Weight = 28.5m, Color = "Палевый", IsActive = true };
            context.Patients.AddRange(ptBarsik, ptAlfa, ptSonya, ptReks);
            await context.SaveChangesAsync();

            // === Партии материалов (с розничной ценой) ===
            DateOnly Exp(int days) => DateOnly.FromDateTime(DateTime.UtcNow.AddDays(days));
            var batC1 = new MaterialBatch { MaterialId = materials[0].MaterialId, BranchId = brCentral.BranchId, SupplierName = "ЗооМедСнаб", ReceiptDate = DateTime.UtcNow.AddDays(-20), ExpirationDate = Exp(180), InitialQuantity = 1000, RemainingQuantity = 820, UnitCost = 5m, ClientPrice = 15m };
            var batC2 = new MaterialBatch { MaterialId = materials[3].MaterialId, BranchId = brCentral.BranchId, SupplierName = "ВетТоргИнтер", ReceiptDate = DateTime.UtcNow.AddDays(-30), ExpirationDate = Exp(60), InitialQuantity = 100, RemainingQuantity = 60, UnitCost = 450m, ClientPrice = 950m };
            var batN1 = new MaterialBatch { MaterialId = materials[0].MaterialId, BranchId = brNorth.BranchId, SupplierName = "ЗооМедСнаб", ReceiptDate = DateTime.UtcNow.AddDays(-30), ExpirationDate = Exp(-2), InitialQuantity = 600, RemainingQuantity = 480, UnitCost = 5m, ClientPrice = 15m };
            var batN2 = new MaterialBatch { MaterialId = materials[4].MaterialId, BranchId = brNorth.BranchId, SupplierName = "ВетМаркет", ReceiptDate = DateTime.UtcNow.AddDays(-35), ExpirationDate = Exp(20), InitialQuantity = 80, RemainingQuantity = 50, UnitCost = 400m, ClientPrice = 850m };
            var batN3 = new MaterialBatch { MaterialId = materials[5].MaterialId, BranchId = brNorth.BranchId, SupplierName = "МедПрибор", ReceiptDate = DateTime.UtcNow.AddDays(-8), ExpirationDate = Exp(400), InitialQuantity = 200, RemainingQuantity = 150, UnitCost = 120m, ClientPrice = 300m };
            context.MaterialBatches.AddRange(batC1, batC2, batN1, batN2, batN3);
            await context.SaveChangesAsync();

            // === Приёмы (закрытые — со снимками цен, плюс запланированные) ===
            var aptCentralClosed = new Appointment
            {
                AppointmentDate = DateTime.UtcNow.Date.AddDays(-3).AddHours(11), PatientId = ptBarsik.PatientId,
                DoctorEmployeeId = empTherapist.EmployeeId, BranchId = brCentral.BranchId, Status = "Closed",
                DoctorComment = "Жалобы на снижение аппетита. Поставлен предварительный гастрит.",
                TotalAmount = 2065m, ClosedAt = DateTime.UtcNow.AddDays(-3), CreatedByUserId = usrBenidze.UserId, CreatedAt = DateTime.UtcNow.AddDays(-3),
                AppointmentServices = new List<AppointmentService>
                {
                    new() { ServiceId = services[0].ServiceId, Quantity = 1, PriceSnapshot = 950m, LineTotal = 950m, PerformedByEmployeeId = empTherapist.EmployeeId },
                    new() { ServiceId = services[4].ServiceId, Quantity = 1, PriceSnapshot = 1100m, LineTotal = 1100m, PerformedByEmployeeId = empTherapist.EmployeeId }
                },
                AppointmentMaterials = new List<AppointmentMaterial>
                {
                    new() { MaterialBatchId = batC1.MaterialBatchId, MaterialId = materials[0].MaterialId, Quantity = 1, UnitCostSnapshot = 5m, ClientPriceSnapshot = 15m, LineTotal = 15m }
                },
                AppointmentDiagnoses = new List<AppointmentDiagnosis>
                {
                    new() { DiagnosisId = diagnoses[1].DiagnosisId, IsPreliminary = true, DoctorNote = "Исключить погрешности диеты" }
                }
            };
            var aptCentralPlanned = new Appointment
            {
                AppointmentDate = DateTime.UtcNow.Date.AddHours(15), PatientId = ptAlfa.PatientId,
                DoctorEmployeeId = empSurgeon.EmployeeId, BranchId = brCentral.BranchId, Status = "Planned",
                DoctorComment = "Плановый осмотр перед вакцинацией.", TotalAmount = 0m,
                CreatedByUserId = usrBenidze.UserId, CreatedAt = DateTime.UtcNow
            };
            var aptNorthClosed = new Appointment
            {
                AppointmentDate = DateTime.UtcNow.Date.AddDays(-2).AddHours(13), PatientId = ptReks.PatientId,
                DoctorEmployeeId = empRatologist.EmployeeId, BranchId = brNorth.BranchId, Status = "Closed",
                DoctorComment = "Кардиологический контроль, выписана поддерживающая терапия.",
                TotalAmount = 3800m, ClosedAt = DateTime.UtcNow.AddDays(-2), CreatedByUserId = usrBenidze.UserId, CreatedAt = DateTime.UtcNow.AddDays(-2),
                AppointmentServices = new List<AppointmentService>
                {
                    new() { ServiceId = services[3].ServiceId, Quantity = 1, PriceSnapshot = 2000m, LineTotal = 2000m, PerformedByEmployeeId = empRatologist.EmployeeId },
                    new() { ServiceId = services[0].ServiceId, Quantity = 1, PriceSnapshot = 950m, LineTotal = 950m, PerformedByEmployeeId = empRatologist.EmployeeId }
                },
                AppointmentMaterials = new List<AppointmentMaterial>
                {
                    new() { MaterialBatchId = batN2.MaterialBatchId, MaterialId = materials[4].MaterialId, Quantity = 1, UnitCostSnapshot = 400m, ClientPriceSnapshot = 850m, LineTotal = 850m }
                },
                AppointmentDiagnoses = new List<AppointmentDiagnosis>
                {
                    new() { DiagnosisId = diagnoses[4].DiagnosisId, IsPreliminary = false, DoctorNote = "Кардиомиопатия, контроль через месяц" }
                }
            };
            var aptNorthPlanned = new Appointment
            {
                AppointmentDate = DateTime.UtcNow.Date.AddHours(11), PatientId = ptReks.PatientId,
                DoctorEmployeeId = empRatologist.EmployeeId, BranchId = brNorth.BranchId, Status = "Planned",
                DoctorComment = "Повторный приём, контроль динамики.", TotalAmount = 0m,
                CreatedByUserId = usrBenidze.UserId, CreatedAt = DateTime.UtcNow
            };
            context.Appointments.AddRange(aptCentralClosed, aptCentralPlanned, aptNorthClosed, aptNorthPlanned);
            await context.SaveChangesAsync();

            // Инициализация платежей по закрытым приёмам
            context.Payments.AddRange(
                new Payment { AppointmentId = aptCentralClosed.AppointmentId, Amount = 2065m, PaymentMethod = "cash", PaidAt = DateTime.UtcNow.AddDays(-3), ProcessedByUserId = usrBenidze.UserId },
                new Payment { AppointmentId = aptNorthClosed.AppointmentId, Amount = 3800m, PaymentMethod = "card", PaidAt = DateTime.UtcNow.AddDays(-2), ProcessedByUserId = usrBenidze.UserId });
            await context.SaveChangesAsync();
        }
    }
}
