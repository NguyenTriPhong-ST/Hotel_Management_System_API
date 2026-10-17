
using HotelManagementSystem.Data;
using HotelManagementSystem.Data.Seed;
using HotelManagementSystem.DTOs.ServiceUsages;
using HotelManagementSystem.DTOs.Stays;
using HotelManagementSystem.Repositories.Implementations;
using HotelManagementSystem.Repositories.Interfaces;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.ModelBuilder;

namespace HotelManagementSystem;

// =====================================================
// Developer: NhaNTCE181298
// Cấu hình Database, OData, Swagger, DI và dữ liệu mẫu.
// UC: DB-01, DB-02, DB-03, F07, F09, F11
// =====================================================

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // =================================================
        // Developer: NhaNTCE181298
        // OData: UC-F07-02, UC-F07-07, UC-F09-02
        var odataBuilder = new ODataConventionModelBuilder();

        odataBuilder.EntitySet<StayListDto>("StayViews");
        odataBuilder.EntitySet<ServiceUsageListDto>(
            "ServiceUsageViews");

        builder.Services.AddControllers()
            .AddOData(options =>
            {
                options
                    .Select()
                    .Filter()
                    .OrderBy()
                    .Count()
                    .SetMaxTop(100)
                    .AddRouteComponents(
                        "odata",
                        odataBuilder.GetEdmModel());
            });
        // =================================================



        // =================================================
        // Developer: NhaNTCE181298
        // Database: DB-01, DB-02 - EF Core & SQL Server
        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString(
                    "DefaultConnection")));
        // =================================================


        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // =================================================
        // Developer: NhaNTCE181298
        // Dependency Injection: F07, F09, F11
 

        // Repositories
        builder.Services.AddScoped<IStayRepository, StayRepository>();
        builder.Services.AddScoped<IInvoiceRepository, InvoiceRepository>();
        builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
        builder.Services.AddScoped<IServiceUsageRepository, ServiceUsageRepository>();
        builder.Services.AddScoped<IReportRepository, ReportRepository>();

        // Services
        builder.Services.AddScoped<StayService>();
        builder.Services.AddScoped<InvoiceService>();
        builder.Services.AddScoped<PaymentService>();
        builder.Services.AddScoped<ServiceUsageService>();
        builder.Services.AddScoped<ReportService>();
        // =================================================

        var app = builder.Build();

        // =================================================
        // Developer: NhaNTCE181298
        // DB-03: Chỉ seed dữ liệu mẫu khi có --seed-demo

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI();

            if (args.Contains(
                "--seed-demo",
                StringComparer.OrdinalIgnoreCase))
            {
                using var scope = app.Services.CreateScope();

                var db = scope.ServiceProvider
                    .GetRequiredService<AppDbContext>();

                await DevelopmentDataSeeder.SeedAsync(db);
            }
        }
        // =================================================


        app.UseHttpsRedirection();

        // JWT Authentication sẽ tích hợp sau (F01).
        app.UseAuthorization();

        app.MapControllers();

        app.Run();
    }
}
