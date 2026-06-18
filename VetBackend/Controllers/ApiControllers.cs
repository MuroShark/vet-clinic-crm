using System.Globalization;
using VetClinicBackend.Auth;
using VetClinicBackend.Data;
using VetClinicBackend.Dtos;
using VetClinicBackend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace VetClinicBackend.Controllers
{
    /// <summary>Базовый класс API-контроллеров: вспомогательные методы разбора данных.</summary>
    [Authorize]
    public abstract class ApiBase : ControllerBase
    {
        protected int CurrentUserId() =>
            int.TryParse(User.FindFirst("uid")?.Value, out var n) ? n : 0;

        protected static DateTime ParseDate(string s) =>
            DateTime.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var d)
                ? d : DateTime.UtcNow;

        protected static DateTime CombineDateTime(string date, string time)
        {
            var d = DateTime.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var dd) ? dd.Date : DateTime.UtcNow.Date;
            if (TimeSpan.TryParse(time, out var t)) d = d.Add(t);
            return DateTime.SpecifyKind(d, DateTimeKind.Utc);
        }

        protected static DateOnly? ParseDateOnly(string? s) =>
            DateOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d) ? d : null;
    }

    [Route("api/[controller]")]
    [ApiController]
    public class BranchesController : ApiBase
    {
        private readonly AppDbContext _db;
        public BranchesController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Get() =>
            Ok((await _db.Branches.AsNoTracking().OrderBy(b => b.BranchId).ToListAsync()).Select(Mapper.Branch));

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] BranchReq req)
        {
            Branch b;
            if (!string.IsNullOrEmpty(req.Id))
            {
                b = await _db.Branches.FindAsync(Mapper.ParseId(req.Id)) ?? new Branch();
                if (b.BranchId == 0) _db.Branches.Add(b);
            }
            else { b = new Branch(); _db.Branches.Add(b); }
            b.Name = req.Name; b.Address = req.Address; b.Phone = req.Phone ?? ""; b.IsActive = req.IsActive;
            await _db.SaveChangesAsync();
            return Ok(Mapper.Branch(b));
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class PositionsController : ApiBase
    {
        private readonly AppDbContext _db;
        public PositionsController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Get() =>
            Ok((await _db.Positions.AsNoTracking().OrderBy(p => p.PositionId).ToListAsync()).Select(Mapper.Position));
    }

    [Route("api/[controller]")]
    [ApiController]
    public class EmployeesController : ApiBase
    {
        private readonly AppDbContext _db;
        public EmployeesController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Get() =>
            Ok((await _db.Employees.AsNoTracking().OrderBy(e => e.EmployeeId).ToListAsync()).Select(Mapper.Employee));

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] EmployeeReq req)
        {
            Employee e;
            if (!string.IsNullOrEmpty(req.Id))
            {
                e = await _db.Employees.FindAsync(Mapper.ParseId(req.Id)) ?? new Employee { HireDate = DateOnly.FromDateTime(DateTime.UtcNow) };
                if (e.EmployeeId == 0) _db.Employees.Add(e);
            }
            else { e = new Employee { HireDate = DateOnly.FromDateTime(DateTime.UtcNow) }; _db.Employees.Add(e); }
            e.FullName = req.Name;
            e.PositionId = Mapper.ParseId(req.PositionId);
            e.Phone = req.Phone ?? "";
            e.Email = req.Email;
            e.KPIRate = req.KPIRate;
            e.BranchId = req.BranchIds is { Length: > 0 } ? Mapper.ParseId(req.BranchIds[0]) : e.BranchId;
            e.IsActive = req.Status == "active";
            await _db.SaveChangesAsync();
            return Ok(Mapper.Employee(e));
        }
    }

    /// <summary>Управление учётными записями — только роль Директора.</summary>
    [Authorize(Roles = "Director")]
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ApiBase
    {
        private readonly AppDbContext _db;
        public UsersController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Get() =>
            Ok((await _db.Users.AsNoTracking().Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                    .OrderBy(u => u.UserId).ToListAsync()).Select(Mapper.User));

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] UserReq req)
        {
            AppUser u;
            if (!string.IsNullOrEmpty(req.Id))
            {
                u = await _db.Users.Include(x => x.UserRoles).FirstOrDefaultAsync(x => x.UserId == Mapper.ParseId(req.Id))
                    ?? new AppUser { PasswordHash = PasswordHasher.Hash("demo123") };
                if (u.UserId == 0) _db.Users.Add(u);
            }
            else { u = new AppUser { PasswordHash = PasswordHasher.Hash("demo123") }; _db.Users.Add(u); }

            u.Login = req.Username;
            if (!string.IsNullOrEmpty(req.EmployeeId)) u.EmployeeId = Mapper.ParseId(req.EmployeeId);
            u.IsLocked = req.Status != "active";

            // Заменяем набор ролей целиком
            _db.UserRoles.RemoveRange(u.UserRoles);
            var roleCodes = req.Roles.Select(Mapper.RoleToBack).ToList();
            var roles = await _db.Roles.Where(r => roleCodes.Contains(r.Code)).ToListAsync();
            await _db.SaveChangesAsync(); // сохранить UserId перед вставкой ролей
            foreach (var r in roles)
                _db.UserRoles.Add(new UserRole { UserId = u.UserId, RoleId = r.RoleId });
            await _db.SaveChangesAsync();

            var full = await _db.Users.Include(x => x.UserRoles).ThenInclude(ur => ur.Role).FirstAsync(x => x.UserId == u.UserId);
            return Ok(Mapper.User(full));
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ApiBase
    {
        private readonly AppDbContext _db;
        public ClientsController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Get() =>
            Ok((await _db.Clients.AsNoTracking().Where(c => c.IsActive)
                    .OrderByDescending(c => c.RegistrationDate).ToListAsync()).Select(Mapper.Client));

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] ClientReq req)
        {
            Client c;
            if (!string.IsNullOrEmpty(req.Id))
            {
                c = await _db.Clients.FindAsync(Mapper.ParseId(req.Id)) ?? new Client();
                if (c.ClientId == 0) { c.RegistrationDate = DateTime.UtcNow; _db.Clients.Add(c); }
            }
            else
            {
                c = new Client { RegistrationDate = DateTime.UtcNow, IsActive = true };
                c.BranchId = await _db.Branches.OrderBy(b => b.BranchId).Select(b => b.BranchId).FirstAsync();
                _db.Clients.Add(c);
            }
            // Имя приходит одной строкой: «Фамилия Имя Отчество»
            var parts = (req.Name ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            c.LastName = parts.ElementAtOrDefault(0) ?? req.Name ?? "";
            c.FirstName = parts.ElementAtOrDefault(1) ?? "";
            c.MiddleName = parts.Length > 2 ? string.Join(' ', parts.Skip(2)) : null;
            c.Phone = req.Phone; c.Email = req.Email; c.ConsentSigned = req.ConsentSigned; c.IsActive = true;
            await _db.SaveChangesAsync();
            return Ok(Mapper.Client(c));
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class PatientsController : ApiBase
    {
        private readonly AppDbContext _db;
        public PatientsController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Get() =>
            Ok((await _db.Patients.AsNoTracking().Include(p => p.Species)
                    .Where(p => p.IsActive).OrderBy(p => p.PatientId).ToListAsync()).Select(Mapper.Patient));

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] PatientReq req)
        {
            // Автоматически создаём вид животного, если он ещё не существует
            var sp = await _db.AnimalSpecies.FirstOrDefaultAsync(s => s.Name == req.Species);
            if (sp == null) { sp = new AnimalSpecies { Name = req.Species }; _db.AnimalSpecies.Add(sp); await _db.SaveChangesAsync(); }

            Patient p;
            if (!string.IsNullOrEmpty(req.Id))
            {
                p = await _db.Patients.FindAsync(Mapper.ParseId(req.Id)) ?? new Patient();
                if (p.PatientId == 0) _db.Patients.Add(p);
            }
            else { p = new Patient { IsActive = true }; _db.Patients.Add(p); }

            p.ClientId = Mapper.ParseId(req.ClientId);
            p.SpeciesId = sp.SpeciesId;
            p.PetName = req.Name;
            p.Breed = req.Breed;
            p.Gender = string.IsNullOrEmpty(req.Gender) ? "M" : req.Gender[..1];
            p.Weight = req.Weight;
            p.Color = req.Color;
            p.BirthDate = ParseDateOnly(req.BirthDate);
            p.IsActive = true;
            await _db.SaveChangesAsync();

            var full = await _db.Patients.Include(x => x.Species).FirstAsync(x => x.PatientId == p.PatientId);
            return Ok(Mapper.Patient(full));
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class ServicesController : ApiBase
    {
        private readonly AppDbContext _db;
        public ServicesController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var services = await _db.Services.AsNoTracking().Where(s => s.IsActive).OrderBy(s => s.ServiceId).ToListAsync();
            // Берём только актуальные цены (EffectiveTo = null)
            var priceByService = (await _db.ServicePrices.AsNoTracking().Where(sp => sp.EffectiveTo == null).ToListAsync())
                .GroupBy(sp => sp.ServiceId)
                .ToDictionary(g => g.Key, g => g.OrderByDescending(x => x.EffectiveFrom).First().Price);
            return Ok(services.Select(s => Mapper.Service(s, priceByService.GetValueOrDefault(s.ServiceId, 0m))));
        }
    }

    /// <summary>
    /// История цен на услуги. POST закрывает текущую цену и создаёт новую.
    /// </summary>
    [Route("api/[controller]")]
    [ApiController]
    public class ServicePricesController : ApiBase
    {
        private readonly AppDbContext _db;
        public ServicePricesController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Get() =>
            Ok((await _db.ServicePrices.AsNoTracking().OrderByDescending(sp => sp.EffectiveFrom).ToListAsync()).Select(Mapper.ServicePrice));

        [HttpPost]
        public async Task<IActionResult> AddPrice([FromBody] PriceReq req)
        {
            var serviceId = Mapper.ParseId(req.ServiceId);
            var now = DateTime.UtcNow;
            // Закрыть все текущие цены на данную услугу
            var current = await _db.ServicePrices.Where(sp => sp.ServiceId == serviceId && sp.EffectiveTo == null).ToListAsync();
            foreach (var c in current) c.EffectiveTo = now;
            var price = new ServicePrice
            {
                ServiceId = serviceId, Price = req.Price, EffectiveFrom = now, EffectiveTo = null,
                CreatedByUserId = CurrentUserId(), CreatedAt = now
            };
            _db.ServicePrices.Add(price);
            await _db.SaveChangesAsync();
            return Ok(Mapper.ServicePrice(price));
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class MaterialsController : ApiBase
    {
        private readonly AppDbContext _db;
        public MaterialsController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Get() =>
            Ok((await _db.Materials.AsNoTracking().Where(m => m.IsActive).OrderBy(m => m.MaterialId).ToListAsync()).Select(Mapper.Material));
    }

    [Route("api/[controller]")]
    [ApiController]
    public class BatchesController : ApiBase
    {
        private readonly AppDbContext _db;
        public BatchesController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Get() =>
            Ok((await _db.MaterialBatches.AsNoTracking().OrderBy(b => b.MaterialBatchId).ToListAsync()).Select(Mapper.Batch));

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] BatchReq req)
        {
            var b = new MaterialBatch
            {
                MaterialId = Mapper.ParseId(req.MaterialId),
                BranchId = Mapper.ParseId(req.BranchId),
                SupplierName = req.Supplier,
                UnitCost = req.PurchasePrice,
                ClientPrice = req.ClientPrice,
                InitialQuantity = req.TotalQuantity,
                RemainingQuantity = req.RemainingQuantity,
                ExpirationDate = ParseDateOnly(req.ExpiryDate),
                ReceiptDate = ParseDate(req.ReceivedAt)
            };
            _db.MaterialBatches.Add(b);
            await _db.SaveChangesAsync();
            return Ok(Mapper.Batch(b));
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class DiagnosesController : ApiBase
    {
        private readonly AppDbContext _db;
        public DiagnosesController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Get() =>
            Ok((await _db.Diagnoses.AsNoTracking().Where(d => d.IsActive).OrderBy(d => d.Name).ToListAsync()).Select(Mapper.Diagnosis));

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] DiagnosisReq req)
        {
            Diagnosis d;
            if (!string.IsNullOrEmpty(req.Id))
            {
                d = await _db.Diagnoses.FindAsync(Mapper.ParseId(req.Id)) ?? new Diagnosis();
                if (d.DiagnosisId == 0) _db.Diagnoses.Add(d);
            }
            else { d = new Diagnosis { IsActive = true }; _db.Diagnoses.Add(d); }
            d.Code = req.Code; d.Name = req.Name; d.Category = req.Category; d.IsActive = true;
            await _db.SaveChangesAsync();
            return Ok(Mapper.Diagnosis(d));
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(string id)
        {
            var d = await _db.Diagnoses.FindAsync(Mapper.ParseId(id));
            if (d != null) { d.IsActive = false; await _db.SaveChangesAsync(); }
            return NoContent();
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class AppointmentsController : ApiBase
    {
        private readonly AppDbContext _db;
        public AppointmentsController(AppDbContext db) => _db = db;

        private static IQueryable<Appointment> WithChildren(IQueryable<Appointment> q) => q
            .Include(a => a.Patient)
            .Include(a => a.AppointmentServices)
            .Include(a => a.AppointmentMaterials)
            .Include(a => a.AppointmentDiagnoses);

        [HttpGet]
        public async Task<IActionResult> Get() =>
            Ok((await WithChildren(_db.Appointments.AsNoTracking()).OrderByDescending(a => a.AppointmentDate).ToListAsync())
                .Select(Mapper.Appointment));

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOne(string id)
        {
            var a = await WithChildren(_db.Appointments.AsNoTracking()).FirstOrDefaultAsync(x => x.AppointmentId == Mapper.ParseId(id));
            return a == null ? NotFound() : Ok(Mapper.Appointment(a));
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] AppointmentReq req)
        {
            Appointment a;
            bool wasClosed = false;

            if (!string.IsNullOrEmpty(req.Id))
            {
                a = await WithChildren(_db.Appointments).FirstOrDefaultAsync(x => x.AppointmentId == Mapper.ParseId(req.Id))
                    ?? throw new InvalidOperationException("Приём не найден");
                wasClosed = a.Status == "Closed";
                _db.AppointmentServices.RemoveRange(a.AppointmentServices);
                _db.AppointmentMaterials.RemoveRange(a.AppointmentMaterials);
                _db.AppointmentDiagnoses.RemoveRange(a.AppointmentDiagnoses);
            }
            else
            {
                a = new Appointment { CreatedByUserId = CurrentUserId(), CreatedAt = DateTime.UtcNow };
                _db.Appointments.Add(a);
            }

            a.PatientId = Mapper.ParseId(req.PatientId);
            a.DoctorEmployeeId = Mapper.ParseId(req.VetId);
            a.BranchId = Mapper.ParseId(req.BranchId);
            a.AppointmentDate = CombineDateTime(req.AppointmentDate, req.TimeSlot);
            a.Status = req.Status;
            a.DoctorComment = req.Notes;
            a.TotalAmount = req.TotalAmount;

            a.AppointmentServices = req.Services.Select(s => new AppointmentService
            {
                ServiceId = Mapper.ParseId(s.ServiceId), Quantity = s.Quantity, PriceSnapshot = s.PriceSnapshot,
                LineTotal = s.PriceSnapshot * s.Quantity, PerformedByEmployeeId = a.DoctorEmployeeId
            }).ToList();
            a.AppointmentMaterials = req.Materials.Select(m => new AppointmentMaterial
            {
                MaterialBatchId = Mapper.ParseId(m.BatchId), MaterialId = Mapper.ParseId(m.MaterialId), Quantity = m.Quantity,
                UnitCostSnapshot = m.UnitCostSnapshot, ClientPriceSnapshot = m.ClientPriceSnapshot, LineTotal = m.ClientPriceSnapshot * m.Quantity
            }).ToList();
            a.AppointmentDiagnoses = req.Diagnoses.Select(d => new AppointmentDiagnosis
            {
                DiagnosisId = Mapper.ParseId(d.DiagnosisId), IsPreliminary = d.IsPreliminary, DoctorNote = d.DoctorComment
            }).ToList();

            // При закрытии приёма: фиксируем время и списываем материалы с остатка партии
            if (req.Status == "Closed" && !wasClosed)
            {
                a.ClosedAt = DateTime.UtcNow;
                foreach (var m in a.AppointmentMaterials)
                {
                    var batch = await _db.MaterialBatches.FindAsync(m.MaterialBatchId);
                    if (batch != null) batch.RemainingQuantity = Math.Max(0, batch.RemainingQuantity - m.Quantity);
                }
            }

            await _db.SaveChangesAsync();
            var full = await WithChildren(_db.Appointments.AsNoTracking()).FirstAsync(x => x.AppointmentId == a.AppointmentId);
            return Ok(Mapper.Appointment(full));
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class PaymentsController : ApiBase
    {
        private readonly AppDbContext _db;
        public PaymentsController(AppDbContext db) => _db = db;

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? appointmentId)
        {
            var q = _db.Payments.AsNoTracking().AsQueryable();
            if (!string.IsNullOrEmpty(appointmentId))
                q = q.Where(p => p.AppointmentId == Mapper.ParseId(appointmentId));
            return Ok((await q.OrderByDescending(p => p.PaidAt).ToListAsync()).Select(Mapper.Payment));
        }

        [HttpPost]
        public async Task<IActionResult> Add([FromBody] PaymentReq req)
        {
            var p = new Payment
            {
                AppointmentId = Mapper.ParseId(req.AppointmentId),
                Amount = req.Amount,
                PaymentMethod = req.Method,
                PaidAt = DateTime.UtcNow,
                ReceiptNumber = req.Notes,
                ProcessedByUserId = CurrentUserId()
            };
            _db.Payments.Add(p);
            await _db.SaveChangesAsync();
            return Ok(Mapper.Payment(p));
        }
    }

    [Route("api/[controller]")]
    [ApiController]
    public class AuditController : ApiBase
    {
        private readonly AppDbContext _db;
        public AuditController(AppDbContext db) => _db = db;

        /// <summary>Журнал аудита — доступен только Директору.</summary>
        [Authorize(Roles = "Director")]
        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var list = await _db.AuditEntries.AsNoTracking().OrderByDescending(a => a.Timestamp).Take(500).ToListAsync();
            return Ok(list.Select(a => new
            {
                id = Mapper.Sid("aud", a.AuditEntryId),
                timestamp = Mapper.Msk(a.Timestamp),
                username = a.Username,
                action = a.Action,
                details = a.Details
            }));
        }

        [HttpPost]
        public async Task<IActionResult> Log([FromBody] AuditReq req)
        {
            _db.AuditEntries.Add(new AuditEntry
            {
                Timestamp = DateTime.UtcNow,
                Username = string.IsNullOrEmpty(req.Username) ? (User.FindFirst("login")?.Value ?? "система") : req.Username,
                Action = req.Action,
                Details = req.Details
            });
            await _db.SaveChangesAsync();
            return NoContent();
        }
    }
}
