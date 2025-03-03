# Etapa 1: Construcción de la aplicación
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copiar la solución y las carpetas de proyecto correctamente
COPY ./RetoTecnico.sln ./
COPY ./src ./src

# Moverse a la carpeta del servicio
WORKDIR /RetoTecnico/src/AntiFraudService/AntiFraudService.API

# Restaurar dependencias
RUN dotnet restore "/RetoTecnico/RetoTecnico.sln"

# Construcción de la aplicación
RUN dotnet build -c Release -o /app/build

# Etapa 2: Publicación
FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

# Etapa 3: Imagen final de ejecución
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
COPY --from=publish /app/publish .

EXPOSE 5002
ENTRYPOINT ["dotnet", "AntiFraudService.API.dll"]


