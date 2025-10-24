using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Sufra.Infrastructure.Persistence;
using Sufra.Application.Mapping;
using Sufra.Application.Interfaces;
using Sufra.Infrastructure.Services;
using Sufra.Application.Services;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// 🧩 إعداد قاعدة البيانات
// ============================================================
builder.Services.AddDbContext<SufraDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

Console.WriteLine($"🔗 DB Connection: {builder.Configuration.GetConnectionString("DefaultConnection")}");

// ============================================================
// ⚙️ تسجيل الخدمات (Dependency Injection)
// ============================================================

// 🧠 AutoMapper
builder.Services.AddAutoMapper(typeof(MappingProfile));

// 🧩 الخدمات الأساسية
builder.Services.AddScoped<IStudentService, StudentService>();
builder.Services.AddScoped<ISubscriptionService, SubscriptionService>();
builder.Services.AddScoped<IMealRequestService, MealRequestService>();
builder.Services.AddScoped<IBatchService, BatchService>();
builder.Services.AddScoped<ICourierService, CourierService>();
builder.Services.AddScoped<IDeliveryService, DeliveryService>();
builder.Services.AddScoped<IStudentHousingService, StudentHousingService>();
builder.Services.AddScoped<IZoneService, ZoneService>();
builder.Services.AddScoped<INotificationService, NotificationService>();


// ============================================================
// 🌐 إعداد الـ Controllers و JSON
// ============================================================
builder.Services.AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        x.JsonSerializerOptions.PropertyNamingPolicy = null; // 👈 لحفظ أسماء الحقول الأصلية (مثل RoleName و UniversityId)
        x.JsonSerializerOptions.WriteIndented = true;
    });

// ============================================================
// 📘 إعداد Swagger
// ============================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
    {
        Title = "Sufra API",
        Version = "v1",
        Description = "🚀 واجهة برمجة تطبيقات نظام سُفرة (MVP)\n\nتشمل إدارة الطلاب، الطلبات، الاشتراكات، والتوصيل.",
        Contact = new Microsoft.OpenApi.Models.OpenApiContact
        {
            Name = "فريق تطوير سُفرة",
            Email = "support@sufra.sa"
        }
    });

    // 🧩 تحميل تعليقات XML (من المشروع الحالي)
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);
});

// ============================================================
// 🧱 بناء التطبيق
// ============================================================
var app = builder.Build();

// ============================================================
// 🔍 تفعيل Swagger أثناء التطوير فقط
// ============================================================
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "📘 Sufra API Docs";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Sufra API v1");
    });
}

// ============================================================
// 🔐 الإعدادات العامة للتطبيق
// ============================================================
app.UseHttpsRedirection();

app.UseCors(policy =>
    policy.AllowAnyOrigin()
          .AllowAnyMethod()
          .AllowAnyHeader());

app.MapControllers();

// ============================================================
// 🔧 Endpoint اختباري (اختياري)
app.MapGet("/ping", () => Results.Ok("✅ Sufra API is running successfully!"));

app.Run();

// ------------------------------------------------------------
// ✅ سجل WeatherForecast (اختباري)
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
