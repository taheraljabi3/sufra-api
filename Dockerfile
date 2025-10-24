# ================================
# 👇 Build Stage
# ================================
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# انسخ ملفات المشروع
COPY . .

# ادخل إلى مجلد الـ API الرئيسي
WORKDIR "/src/Sufra.Api"

# بناء المشروع في وضع Release
RUN dotnet restore
RUN dotnet publish -c Release -o /app/publish

# ================================
# 👇 Runtime Stage
# ================================
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .

# شغّل التطبيق
ENTRYPOINT ["dotnet", "Sufra.Api.dll"]
