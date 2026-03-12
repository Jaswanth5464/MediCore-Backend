using MediCore.API.Infrastructure.Database.Context;
using MediCore.API.Migrations;
using MediCore.API.Modules.Auth.Models;
using MediCore.API.Modules.Doctor.Models;
using MediCore.API.Modules.Finance.Models;
using MediCore.API.Modules.Laboratory.Models;
using MediCore.API.Modules.OPD.Models;
using MediCore.API.Modules.Patient.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;

namespace MediCore.API.Infrastructure.Database.Context
{
    public class MediCoreDbContext : DbContext
    {
        public MediCoreDbContext(DbContextOptions<MediCoreDbContext> options)
            : base(options)
        {
        }

        // Auth Tables
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }

        public DbSet<Department> Departments { get; set; }
        public DbSet<DoctorProfile> DoctorProfiles { get; set; }

        public DbSet<AppointmentRequest> AppointmentRequests { get; set; }
        public DbSet<Appointment> Appointments { get; set; }
        public DbSet<OtpRecord> OtpRecords { get; set; }
        public DbSet<Bill> Bills { get; set; }
        public DbSet<Vitals> Vitals { get; set; }
        public DbSet<Prescription> Prescriptions { get; set; }
        public DbSet<LabOrder> LabOrders { get; set; }
        public DbSet<LabTestMaster> LabTestMasters { get; set; }
        public DbSet<DoctorLeave> DoctorLeaves { get; set; }
        public DbSet<PatientProfile> PatientProfiles { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<MediCore.API.Modules.Pharmacy.Models.Medicine> Medicines { get; set; }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfigurationsFromAssembly(
                typeof(MediCoreDbContext).Assembly);

            // Configure Many-to-Many UserRoles
            modelBuilder.Entity<UserRole>()
                .HasKey(ur => new { ur.UserId, ur.RoleId });

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);

            // Prevent cascade cycles for Appointments
            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.PatientUser)
                .WithMany()
                .HasForeignKey(a => a.PatientUserId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Appointment>()
                .HasOne(a => a.DoctorProfile)
                .WithMany()
                .HasForeignKey(a => a.DoctorProfileId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Department>().HasData(
    new Department { Id = 1, Name = "General Medicine", Description = "General health consultations", Icon = "🏥", FloorNumber = 1 },
    new Department { Id = 2, Name = "Cardiology", Description = "Heart care", Icon = "❤️", FloorNumber = 2 },
    new Department { Id = 3, Name = "Neurology", Description = "Brain care", Icon = "🧠", FloorNumber = 2 },
    new Department { Id = 4, Name = "Orthopedics", Description = "Bone care", Icon = "🦴", FloorNumber = 3 },
    new Department { Id = 5, Name = "Pediatrics", Description = "Children care", Icon = "👶", FloorNumber = 1 },
    new Department { Id = 6, Name = "Gynecology", Description = "Women health", Icon = "🌸", FloorNumber = 4 },
    new Department { Id = 7, Name = "Dermatology", Description = "Skin care", Icon = "✨", FloorNumber = 2 },
    new Department { Id = 8, Name = "Ophthalmology", Description = "Eye care", Icon = "👁️", FloorNumber = 1 },
    new Department { Id = 9, Name = "ENT", Description = "Ear nose throat", Icon = "👂", FloorNumber = 3 },
    new Department { Id = 10, Name = "Psychiatry", Description = "Mental health", Icon = "🧘", FloorNumber = 4 }
);
            // Seed default roles into database automatically
            modelBuilder.Entity<Role>().HasData(
                new Role { Id = 1, Name = "SuperAdmin", Description = "Full system access" },
                new Role { Id = 2, Name = "HospitalAdmin", Description = "Hospital management access" },
                new Role { Id = 3, Name = "Receptionist", Description = "Front desk operations" },
                new Role { Id = 4, Name = "Doctor", Description = "Medical consultation access" },
                new Role { Id = 5, Name = "Nurse", Description = "Ward management access" },
                new Role { Id = 6, Name = "Pharmacist", Description = "Pharmacy management access" },
                new Role { Id = 7, Name = "LabTechnician", Description = "Laboratory access" },
                new Role { Id = 8, Name = "FinanceStaff", Description = "Billing and finance access" },
                new Role { Id = 9, Name = "Mentor", Description = "Patient mentoring access" },
                new Role { Id = 10, Name = "Patient", Description = "Patient portal access" }
            );

            base.OnModelCreating(modelBuilder);
        }
    }
}
//```

//---

//## Step 6 — Create the Database

//Go to** Package Manager Console** and run these two commands one by one:
//```
//Add - Migration AuthTables
//```

//Wait for it to finish then run:
//```
//Update - Database
//```

//---

//## What Just Happened
//```
//Add - Migration    →    EF Core reads your models
//                      Generates SQL to create tables
//                      Creates a file in Migrations folder

//Update-Database  →    Runs that SQL against SQL Server
//                      Creates Users, Roles, RefreshTokens tables
//                      Seeds the 10 default roles automatically
//```

//---

//Go to **SQL Server Object Explorer** in Visual Studio, expand your SQL Server, expand MediCoreDB, expand Tables. You should see:
//```
//dbo.Roles
//dbo.Users
//dbo.RefreshTokens