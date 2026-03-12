using MediCore.API.Infrastructure.Database.Context;
using MediCore.API.Modules.Auth.Models;
using MediCore.API.Modules.Doctor.Models;
using MediCore.API.Modules.Laboratory.Models;
using MediCore.API.Modules.OPD.Models;
using MediCore.API.Modules.Pharmacy.Models;
using Microsoft.EntityFrameworkCore;

namespace MediCore.API.Infrastructure.Database
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(MediCoreDbContext context)
        {
            if (await context.Users.CountAsync() > 10) return; // Already seeded

            // 0. Seed Lab Tests and Medicines
            if (!await context.LabTestMasters.AnyAsync())
            {
                var labTests = new List<LabTestMaster>
                {
                    new LabTestMaster { TestName = "Complete Blood Count (CBC)", NormalRange = "Varies", Price = 400 },
                    new LabTestMaster { TestName = "Lipid Profile", NormalRange = "Cholesterol < 200 mg/dL", Price = 800 },
                    new LabTestMaster { TestName = "Liver Function Test (LFT)", NormalRange = "SGOT: 5-40 U/L", Price = 1000 },
                    new LabTestMaster { TestName = "Kidney Function Test (KFT)", NormalRange = "Creatinine: 0.6-1.2 mg/dL", Price = 900 },
                    new LabTestMaster { TestName = "Thyroid Profile (T3, T4, TSH)", NormalRange = "TSH: 0.4-4.0 mIU/L", Price = 1200 },
                    new LabTestMaster { TestName = "Fasting Blood Sugar (FBS)", NormalRange = "70-100 mg/dL", Price = 150 },
                    new LabTestMaster { TestName = "HbA1c", NormalRange = "4.0-5.6%", Price = 600 },
                    new LabTestMaster { TestName = "Vitamin B12", NormalRange = "200-900 pg/mL", Price = 1100 },
                    new LabTestMaster { TestName = "Vitamin D (25-OH)", NormalRange = "30-100 ng/mL", Price = 1400 },
                    new LabTestMaster { TestName = "Urine Routine & Microscopy", NormalRange = "Pus cells: 0-5 /HPF", Price = 250 },
                    new LabTestMaster { TestName = "C-Reactive Protein (CRP)", NormalRange = "< 10 mg/L", Price = 500 },
                    new LabTestMaster { TestName = "Electrocardiogram (ECG)", NormalRange = "Normal Sinus Rhythm", Price = 400 },
                    new LabTestMaster { TestName = "Chest X-Ray (PA View)", NormalRange = "Normal Study", Price = 600 },
                    new LabTestMaster { TestName = "Ultrasound Whole Abdomen", NormalRange = "NAD", Price = 1500 },
                    new LabTestMaster { TestName = "Dengue NS1 Antigen", NormalRange = "Negative", Price = 700 },
                    new LabTestMaster { TestName = "Malaria Parasite (MP) Smear", NormalRange = "Not seen", Price = 200 },
                    new LabTestMaster { TestName = "Typhoid Widal Test", NormalRange = "< 1:80", Price = 300 },
                    new LabTestMaster { TestName = "Serum Iron Studies", NormalRange = "Iron: 60-170 mcg/dL", Price = 950 },
                    new LabTestMaster { TestName = "Uric Acid", NormalRange = "3.4-7.0 mg/dL", Price = 250 },
                    new LabTestMaster { TestName = "Calcium (Total)", NormalRange = "8.6-10.3 mg/dL", Price = 350 }
                };
                context.LabTestMasters.AddRange(labTests);
            }

            if (!await context.Medicines.AnyAsync())
            {
                var meds = new List<Medicine>();
                string[] categories = { "Tablet", "Capsule", "Syrup", "Injection", "Ointment" };
                string[] prefixes = { "Para", "Amo", "Ceti", "Panto", "Vitam", "Ibu", "Azi", "Dicl", "Keto", "Ome" };
                string[] suffixes = { "cetamol", "xicillin", "rizine", "prazole", "in C", "profen", "thromycin", "ofenac", "rolac", "prazole" };
                
                for(int i=0; i<50; i++)
                {
                    meds.Add(new Medicine {
                        Name = $"{prefixes[i%prefixes.Length]}{suffixes[(i/5)%suffixes.Length]} {250 + (i*50)}mg",
                        GenericName = $"Generic Component {i}",
                        Category = categories[i % categories.Length],
                        Manufacturer = $"PharmaCo {(i % 5) + 1}",
                        Price = (decimal)(10.0 + (i * 2.5)),
                        StockQuantity = 100 + (i * 10),
                        LowStockThreshold = 50,
                        ExpiryDate = DateTime.UtcNow.AddMonths(12 + (i % 24))
                    });
                }
                context.Medicines.AddRange(meds);
            }
            
            await context.SaveChangesAsync();

            // 1. Seed Doctors
            var doctorNames = new[] { "Dr. Rajesh Khanna", "Dr. Shalini Singh", "Dr. Amit Verma", "Dr. Priya Reddy", "Dr. Vikram Seth" };
            var specializations = new[] { "Cardiology", "Neurology", "General Medicine", "Pediatrics", "Orthopedics" };
            var depts = await context.Departments.ToListAsync();

            for (int i = 0; i < doctorNames.Length; i++)
            {
                var user = new User
                {
                    FullName = doctorNames[i],
                    Email = $"doctor{i + 1}@medicore.com",
                    PhoneNumber = $"987654321{i}",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass@123"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UserRoles = new List<UserRole> { new UserRole { RoleId = 4 } }
                };
                context.Users.Add(user);
                await context.SaveChangesAsync();

                var doctor = new DoctorProfile
                {
                    UserId = user.Id,
                    DepartmentId = depts[i % depts.Count].Id,
                    Specialization = specializations[i],
                    Qualification = "MD, MBBS",
                    ExperienceYears = 10 + i,
                    ConsultationFee = 500 + (i * 100),
                    AvailableDays = "Mon,Tue,Wed,Thu,Fri,Sat",
                    MorningStart = new TimeSpan(9, 0, 0),
                    MorningEnd = new TimeSpan(13, 0, 0),
                    HasEveningShift = true,
                    EveningStart = new TimeSpan(17, 0, 0),
                    EveningEnd = new TimeSpan(21, 0, 0),
                    SlotDurationMinutes = 15,
                    IsActive = true
                };
                context.DoctorProfiles.Add(doctor);
            }

            // 2. Seed Patients
            var patientNames = new[] { "Rahul Kumar", "Sita Sharma", "John Doe", "Anita Gupta" };
            for (int i = 0; i < patientNames.Length; i++)
            {
                var user = new User
                {
                    FullName = patientNames[i],
                    Email = $"patient{i + 1}@example.com",
                    PhoneNumber = $"887766554{i}",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("Pass@123"),
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UserRoles = new List<UserRole> { new UserRole { RoleId = 10 } }
                };
                context.Users.Add(user);
            }
            await context.SaveChangesAsync();

            // 3. Seed Appointments (Extensive History + Future)
            var doctors = await context.DoctorProfiles.ToListAsync();
            var patients = await context.Users.Where(u => u.UserRoles.Any(ur => ur.RoleId == 10)).ToListAsync();
            var today = DateTime.UtcNow.Date;

            foreach (var patient in patients)
            {
                // Give each patient 10 past appointments spanning the last 6 months
                for(int i=1; i<=10; i++) 
                {
                    var pastDate = today.AddDays(- (i * 15));
                    var doc = doctors[i % doctors.Count];
                    
                    var prevAppt = new Appointment
                    {
                        TokenNumber = $"OPDPST{pastDate:yyyyMMdd}{patient.Id}{i}",
                        PatientUserId = patient.Id,
                        DoctorProfileId = doc.Id,
                        DepartmentId = doc.DepartmentId,
                        AppointmentDate = pastDate,
                        TimeSlot = new TimeSpan(10, 0, 0),
                        Status = "Completed",
                        VisitType = "Consultation",
                        ConsultationFee = doc.ConsultationFee,
                        PaymentStatus = "Paid",
                        CreatedAt = pastDate.AddDays(-2),
                        ConsultationStartedAt = pastDate.AddHours(10),
                        CompletedAt = pastDate.AddHours(10).AddMinutes(15)
                    };
                    context.Appointments.Add(prevAppt);
                }

                // Today's Appointment
                var todayDoc = doctors[0];
                var todayAppt = new Appointment
                {
                    TokenNumber = $"OPD{today:yyyyMMdd}00{patient.Id}",
                    PatientUserId = patient.Id,
                    DoctorProfileId = todayDoc.Id,
                    DepartmentId = todayDoc.DepartmentId,
                    AppointmentDate = today,
                    TimeSlot = new TimeSpan(11, 0, 0),
                    Status = "Scheduled",
                    VisitType = "Consultation",
                    ConsultationFee = todayDoc.ConsultationFee,
                    PaymentStatus = "Pending",
                    CreatedAt = today.AddDays(-1)
                };
                context.Appointments.Add(todayAppt);

                // Future Appointment (Tomorrow)
                var tomDoc = doctors[1];
                var futureAppt = new Appointment
                {
                    TokenNumber = $"OPDFUT{today.AddDays(1):yyyyMMdd}00{patient.Id}",
                    PatientUserId = patient.Id,
                    DoctorProfileId = tomDoc.Id,
                    DepartmentId = tomDoc.DepartmentId,
                    AppointmentDate = today.AddDays(1),
                    TimeSlot = new TimeSpan(14, 0, 0),
                    Status = "Scheduled",
                    VisitType = "FollowUp",
                    ConsultationFee = tomDoc.ConsultationFee,
                    PaymentStatus = "Pending",
                    CreatedAt = today
                };
                context.Appointments.Add(futureAppt);
            }

            await context.SaveChangesAsync();
        }
    }
}
