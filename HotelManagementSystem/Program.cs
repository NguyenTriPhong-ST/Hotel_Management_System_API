
using HotelManagementSystem.Data;
using HotelManagementSystem.Repositories.Implementations;
using HotelManagementSystem.Repositories.Interfaces;
using HotelManagementSystem.Services;
using HotelManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace HotelManagementSystem;

// =====================================================
// Developer: NhaNTCE181298
// Cấu hình Database, Swagger và Dependency Injection.
// Module: DB-01, DB-02, F07, F09, F11
// =====================================================

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // =================================================
        // Developer: NhaNTCE181298
        // ASP.NET Core Web API và Swagger
        // =================================================

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // =================================================
        // Developer: NhaNTCE181298
        // DB-01, DB-02: EF Core và SQL Server
        // =================================================

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString(
                    "DefaultConnection")));

        // =================================================
        // Developer: NhaNTCE181298
        // Dependency Injection: F07, F09, F11
        // =================================================

        // Repositories
        builder.Services.AddScoped<IStayRepository, StayRepository>();
        builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
        builder.Services.AddScoped<IServiceUsageRepository, ServiceUsageRepository>();
        builder.Services.AddScoped<IReportRepository, ReportRepository>();

        // Services
        builder.Services.AddScoped<IStayService, StayService>();
        builder.Services.AddScoped<IInvoiceService, InvoiceService>();
        builder.Services.AddScoped<IPaymentService, PaymentService>();
        builder.Services.AddScoped<IServiceUsageService, ServiceUsageService>();
        builder.Services.AddScoped<IReportService, ReportService>();

        var app = builder.Build();

        // =================================================
        // Developer: NhaNTCE181298
        // Swagger - Chỉ sử dụng trong Development
        // =================================================

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();
        }

        // =================================================
        // Developer: NhaNTCE181298
        // Middleware và API Routing
        // =================================================

        app.UseHttpsRedirection();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}
