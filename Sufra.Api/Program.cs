using System.Reflection;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

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
// 🔐 إعداد الـ JWT Authentication
// ============================================================

var jwtKey = builder.Configuration["Jwt:Key"] ?? "SUFRA_SECRET_KEY_2025_!CHANGE_THIS!";
var keyBytes = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false; // في التطوير يمكن تعطيله
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = false,
        ValidateAudience = false,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ClockSkew = TimeSpan.Zero
    };
});

builder.Services.AddAuthorization();

// ============================================================
// 🌐 إعداد الـ Controllers و JSON
// ============================================================
builder.Services.AddControllers()
    .AddJsonOptions(x =>
    {
        x.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
        x.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        x.JsonSerializerOptions.PropertyNamingPolicy = null;
        x.JsonSerializerOptions.WriteIndented = true;
    });

// ============================================================
// 📘 إعداد Swagger مع دعم JWT
// ============================================================
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Sufra API",
        Version = "v1",
        Description = "🚀 واجهة برمجة تطبيقات نظام سُفرة (MVP)\n\nتشمل إدارة الطلاب، الطلبات، الاشتراكات، والتوصيل.",
        Contact = new OpenApiContact
        {
            Name = "فريق تطوير سُفرة",
            Email = "support@sufra.sa"
        }
    });

    // 🧩 تحميل تعليقات XML (للتوثيق التلقائي)
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
        options.IncludeXmlComments(xmlPath);

    // 🧱 دعم إدخال التوكن في Swagger UI
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "أدخل التوكن هنا بصيغة: **Bearer {your token}**",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });

    // 🧩 تجنب تضارب الأسماء في DTOs
    options.CustomSchemaIds(type => type.FullName);
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
        options.RoutePrefix = "docs"; // يمكن الوصول عبر /docs
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

// ✅ تفعيل المصادقة والتفويض
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ============================================================
// 🔧 Endpoint اختباري
// ============================================================
app.MapGet("/ping", () => Results.Ok("✅ Sufra API is running successfully!"));

// ============================================================
// 🚀 تشغيل التطبيق
// ============================================================
app.Run();

// ------------------------------------------------------------
// ✅ سجل WeatherForecast (اختياري)
record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
