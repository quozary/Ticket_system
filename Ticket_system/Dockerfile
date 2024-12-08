# Этап сборки
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

# Устанавливаем рабочую директорию
WORKDIR /src

# Копируем файл проекта и восстанавливаем зависимости
COPY ["Ticket_system/Ticket_system.csproj", "Ticket_system/"]
RUN dotnet restore "Ticket_system/Ticket_system.csproj"

# Копируем все остальные файлы в контейнер
COPY . .

# Выполняем сборку и публикацию
RUN dotnet publish "Ticket_system/Ticket_system.csproj" -c Release -o /app

# Этап выполнения
FROM mcr.microsoft.com/dotnet/aspnet:8.0

# Устанавливаем рабочую директорию
WORKDIR /app

# Копируем собранные файлы из этапа сборки
COPY --from=build /app .

# Указываем команду для запуска приложения
ENTRYPOINT ["dotnet", "Ticket_system.dll"]
