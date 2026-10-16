
using HotelManagementSystem.Data;
using HotelManagementSystem.Data.Seed;
using HotelManagementSystem.DTOs.ServiceUsages;
using HotelManagementSystem.DTOs.Stays;
using HotelManagementSystem.Services;
using Microsoft.AspNetCore.OData;
using Microsoft.EntityFrameworkCore;
using Microsoft.OData.ModelBuilder;

namespace HotelManagementSystem;

// =====================================================
// Developer: NhaNTCE181298
// UC: DB-03, F07, F09, F11
// Mô tả: Cấu hình ASP.NET Core Web API,
// SQL Server, Swagger, OData và dữ liệu mẫu.
// =====================================================

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // =====================================================
        // Developer: NhaNTCE181298
        // UC: UC-F07-02 - View Stays
        // UC: UC-F07-07 - Search & Filter Stays
        // UC: UC-F09-02 - View Service Usages
        //
        // OData:
        // - $filter: Lọc dữ liệu
        // - $orderby: Sắp xếp
        // - $top: Giới hạn số bản ghi
        // - $skip: Phân trang
        // - $select: Chọn trường dữ liệu
        // - $count: Đếm tổng số bản ghi
        // =====================================================

        var odataBuilder = new ODataConventionModelBuilder();

        // UC-F07-02, UC-F07-07:
        // Đăng ký EDM Model cho Stay.
        odataBuilder.EntitySet<StayListDto>("StayViews");

        // UC-F09-02:
        // Đăng ký EDM Model cho ServiceUsage.
        odataBuilder.EntitySet<ServiceUsageListDto>(
            "ServiceUsageViews");

        // Cấu hình Controller và OData.
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
                        odataBuilder.GetEdmModel()
                    );
            });

        // =====================================================
        // Developer: NhaNTCE181298
        // UC: DB-01, DB-02
        // Cấu hình Entity Framework Core và SQL Server.
        // =====================================================

        builder.Services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                builder.Configuration.GetConnectionString(
                    "DefaultConnection")
            )
        );

        // =====================================================
        // Developer: NhaNTCE181298
        // Swagger: Kiểm thử Web API.
        // =====================================================

        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen();

        // =====================================================
        // Developer: NhaNTCE181298
        // UC-F07: Stay Management.
        // UC-F09: Service Usage Management.
        // =====================================================


        // =====================================================
        // Developer: NhaNTCE181298
        // Đăng ký Dependency Injection cho các Use Case.
        // =====================================================

        // UC-F07-01 đến UC-F07-07
        builder.Services.AddScoped<StayService>();

        // UC-F09-01 đến UC-F09-06
        builder.Services.AddScoped<ServiceUsageService>();

        // UC-F07-04: Calculate & Finalize Invoice
        builder.Services.AddScoped<InvoiceService>();

        // UC-F07-05: Internal Payment
        builder.Services.AddScoped<PaymentService>();

        builder.Services.AddScoped<InvoiceService>();

        // =====================================================
        // Developer: NhaNTCE181298
        // UC-F11-01 đến UC-F11-04: Operational Reporting
        // =====================================================
        builder.Services.AddScoped<ReportService>();

        // =====================================================
        // Khởi tạo ứng dụng.
        // =====================================================

        var app = builder.Build();

        // =====================================================
        // Developer: NhaNTCE181298
        // UC: DB-03 - Development Data Seeder
        //
        // Chỉ tạo dữ liệu mẫu khi chạy:
        // dotnet run -- --seed-demo
        //
        // Không tự động seed mỗi lần chạy Backend.
        // =====================================================

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

        // =====================================================
        // Middleware.
        // =====================================================

        app.UseHttpsRedirection();

        // JWT Authentication sẽ được tích hợp
        // khi thành viên phụ trách UC-F01 hoàn thành.
        app.UseAuthorization();

        // Ánh xạ tất cả API Controllers.
        app.MapControllers();

        app.Run();
    }
}
