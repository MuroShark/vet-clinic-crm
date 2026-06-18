using Microsoft.EntityFrameworkCore;
using VetClinicBackend.Models;

namespace VetClinicBackend.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        // Организационная структура
        public DbSet<Branch> Branches { get; set; }
        public DbSet<Position> Positions { get; set; }
        public DbSet<Employee> Employees { get; set; }

        // Аутентификация и роли
        public DbSet<AppUser> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        // Клиенты и пациенты
        public DbSet<Client> Clients { get; set; }
        public DbSet<AnimalSpecies> AnimalSpecies { get; set; }
        public DbSet<Patient> Patients { get; set; }

        // Услуги и прейскурант
        public DbSet<Service> Services { get; set; }
        public DbSet<ServicePrice> ServicePrices { get; set; }
        public DbSet<Diagnosis> Diagnoses { get; set; }

        // Склад и материалы
        public DbSet<Material> Materials { get; set; }
        public DbSet<MaterialBatch> MaterialBatches { get; set; }

        // Приёмы
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<AppointmentService> AppointmentServices { get; set; }
        public DbSet<AppointmentMaterial> AppointmentMaterials { get; set; }
        public DbSet<AppointmentDiagnosis> AppointmentDiagnoses { get; set; }

        // Платежи и аудит
        public DbSet<Payment> Payments { get; set; }
        public DbSet<AuditEntry> AuditEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>().ToTable("Users");

            modelBuilder.Entity<Branch>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(100);
                e.Property(x => x.Address).HasMaxLength(255);
                e.Property(x => x.Phone).HasMaxLength(20);
            });

            modelBuilder.Entity<Position>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(100);
            });

            modelBuilder.Entity<Employee>(e =>
            {
                e.Property(x => x.FullName).HasMaxLength(150);
                e.Property(x => x.Phone).HasMaxLength(20);
                e.Property(x => x.Email).HasMaxLength(100);
                e.Property(x => x.KPIRate).HasPrecision(5, 4);

                e.HasOne(x => x.Branch)
                 .WithMany(x => x.Employees)
                 .HasForeignKey(x => x.BranchId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Position)
                 .WithMany(x => x.Employees)
                 .HasForeignKey(x => x.PositionId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AppUser>(e =>
            {
                e.HasKey(x => x.UserId);
                e.Property(x => x.Login).HasMaxLength(50);
                e.Property(x => x.PasswordHash).HasMaxLength(255);
                e.HasIndex(x => x.Login).IsUnique();
                e.HasIndex(x => x.EmployeeId).IsUnique();
                e.Property(x => x.RowVersion).IsRowVersion();

                e.HasOne(x => x.Employee)
                 .WithOne(x => x.User)
                 .HasForeignKey<AppUser>(x => x.EmployeeId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Role>(e =>
            {
                e.Property(x => x.Code).HasMaxLength(50);
                e.Property(x => x.Name).HasMaxLength(100);
                e.HasIndex(x => x.Code).IsUnique();
            });

            modelBuilder.Entity<UserRole>(e =>
            {
                e.HasIndex(x => new { x.UserId, x.RoleId }).IsUnique();

                e.HasOne(x => x.User)
                 .WithMany(x => x.UserRoles)
                 .HasForeignKey(x => x.UserId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Role)
                 .WithMany(x => x.UserRoles)
                 .HasForeignKey(x => x.RoleId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Client>(e =>
            {
                e.Property(x => x.LastName).HasMaxLength(50);
                e.Property(x => x.FirstName).HasMaxLength(50);
                e.Property(x => x.MiddleName).HasMaxLength(50);
                e.Property(x => x.Phone).HasMaxLength(20);
                e.Property(x => x.Email).HasMaxLength(100);

                e.HasOne(x => x.Branch)
                 .WithMany(x => x.Clients)
                 .HasForeignKey(x => x.BranchId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AnimalSpecies>(e =>
            {
                e.HasKey(x => x.SpeciesId);
                e.Property(x => x.Name).HasMaxLength(50);
            });

            modelBuilder.Entity<Patient>(e =>
            {
                e.Property(x => x.PetName).HasMaxLength(100);
                e.Property(x => x.Breed).HasMaxLength(100);
                e.Property(x => x.Gender).HasMaxLength(1);
                e.Property(x => x.Weight).HasPrecision(5, 2);
                e.Property(x => x.Color).HasMaxLength(50);

                e.HasOne(x => x.Client)
                 .WithMany(x => x.Patients)
                 .HasForeignKey(x => x.ClientId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Species)
                 .WithMany(x => x.Patients)
                 .HasForeignKey(x => x.SpeciesId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Service>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(200);
                e.Property(x => x.CategoryCode).HasMaxLength(50);
            });

            modelBuilder.Entity<ServicePrice>(e =>
            {
                e.Property(x => x.Price).HasPrecision(10, 2);

                e.HasOne(x => x.Service)
                 .WithMany(x => x.ServicePrices)
                 .HasForeignKey(x => x.ServiceId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.CreatedByUser)
                 .WithMany(x => x.CreatedServicePrices)
                 .HasForeignKey(x => x.CreatedByUserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Diagnosis>(e =>
            {
                e.Property(x => x.Code).HasMaxLength(20);
                e.Property(x => x.Name).HasMaxLength(255);

                // Самоссылочная иерархия диагнозов
                e.HasOne(x => x.ParentDiagnosis)
                 .WithMany(x => x.ChildDiagnoses)
                 .HasForeignKey(x => x.ParentDiagnosisId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Material>(e =>
            {
                e.Property(x => x.Name).HasMaxLength(200);
                e.Property(x => x.UnitOfMeasure).HasMaxLength(20);
                e.Property(x => x.CategoryCode).HasMaxLength(50);
            });

            modelBuilder.Entity<MaterialBatch>(e =>
            {
                e.Property(x => x.SupplierName).HasMaxLength(200);
                e.Property(x => x.BatchNumber).HasMaxLength(50);
                e.Property(x => x.UnitCost).HasPrecision(10, 4);
                e.Property(x => x.ClientPrice).HasPrecision(10, 2);
                e.Property(x => x.RowVersion).IsRowVersion();

                e.HasOne(x => x.Material)
                 .WithMany(x => x.MaterialBatches)
                 .HasForeignKey(x => x.MaterialId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Branch)
                 .WithMany(x => x.MaterialBatches)
                 .HasForeignKey(x => x.BranchId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Appointment>(e =>
            {
                e.Property(x => x.Status).HasMaxLength(20);
                e.Property(x => x.TotalAmount).HasPrecision(10, 2);
                e.Property(x => x.RowVersion).IsRowVersion();

                e.HasOne(x => x.Patient)
                 .WithMany(x => x.Appointments)
                 .HasForeignKey(x => x.PatientId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Doctor)
                 .WithMany(x => x.DoctorAppointments)
                 .HasForeignKey(x => x.DoctorEmployeeId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Branch)
                 .WithMany(x => x.Appointments)
                 .HasForeignKey(x => x.BranchId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.CreatedByUser)
                 .WithMany(x => x.CreatedAppointments)
                 .HasForeignKey(x => x.CreatedByUserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AppointmentService>(e =>
            {
                e.Property(x => x.PriceSnapshot).HasPrecision(10, 2);
                e.Property(x => x.LineTotal).HasPrecision(10, 2);
                e.Property(x => x.Note).HasMaxLength(500);

                e.HasOne(x => x.Appointment)
                 .WithMany(x => x.AppointmentServices)
                 .HasForeignKey(x => x.AppointmentId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Service)
                 .WithMany(x => x.AppointmentServices)
                 .HasForeignKey(x => x.ServiceId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.PerformedByEmployee)
                 .WithMany(x => x.PerformedServices)
                 .HasForeignKey(x => x.PerformedByEmployeeId)
                 .OnDelete(DeleteBehavior.SetNull);
            });

            modelBuilder.Entity<AppointmentMaterial>(e =>
            {
                e.Property(x => x.UnitCostSnapshot).HasPrecision(10, 4);
                e.Property(x => x.ClientPriceSnapshot).HasPrecision(10, 2);
                e.Property(x => x.LineTotal).HasPrecision(10, 2);

                e.HasOne(x => x.Appointment)
                 .WithMany(x => x.AppointmentMaterials)
                 .HasForeignKey(x => x.AppointmentId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.MaterialBatch)
                 .WithMany(x => x.AppointmentMaterials)
                 .HasForeignKey(x => x.MaterialBatchId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<AppointmentDiagnosis>(e =>
            {
                e.Property(x => x.DoctorNote).HasMaxLength(500);

                e.HasOne(x => x.Appointment)
                 .WithMany(x => x.AppointmentDiagnoses)
                 .HasForeignKey(x => x.AppointmentId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.Diagnosis)
                 .WithMany(x => x.AppointmentDiagnoses)
                 .HasForeignKey(x => x.DiagnosisId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<Payment>(e =>
            {
                e.Property(x => x.Amount).HasPrecision(10, 2);
                e.Property(x => x.PaymentMethod).HasMaxLength(20);
                e.Property(x => x.ReceiptNumber).HasMaxLength(20);

                e.HasOne(x => x.Appointment)
                 .WithMany(x => x.Payments)
                 .HasForeignKey(x => x.AppointmentId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.ProcessedByUser)
                 .WithMany(x => x.ProcessedPayments)
                 .HasForeignKey(x => x.ProcessedByUserId)
                 .OnDelete(DeleteBehavior.Restrict);
            });
        }
    }
}
