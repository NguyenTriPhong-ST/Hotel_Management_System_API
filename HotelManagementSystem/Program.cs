using HotelManagementSystem.Data;
using HotelManagementSystem.Repositories.Implementations;
using HotelManagementSystem.Repositories.Interfaces;
using HotelManagementSystem.Services.Implementations;
using HotelManagementSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Đăng ký Dependency Injection theo Layer-Based Architecture
// 1. Repositories (Data Access Layer)
builder.Services.AddScoped<IGuestRepository, GuestRepository>();
builder.Services.AddScoped<IHotelServiceRepository, HotelServiceRepository>();

// 2. Services (Business Logic Layer)
builder.Services.AddScoped<IGuestService, GuestService>();
builder.Services.AddScoped<IHotelServiceService, HotelServiceService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Tự động kiểm tra và nạp dữ liệu mẫu khi khởi chạy ứng dụng
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var context = services.GetRequiredService<AppDbContext>();
        await DbInitializer.SeedAsync(context);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Đã xảy ra lỗi trong quá trình khởi tạo dữ liệu mẫu (Data Seeding).");
    }
}

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
