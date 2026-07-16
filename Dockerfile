FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["Servidor/Servidor.csproj", "Servidor/"]
RUN dotnet restore "Servidor/Servidor.csproj"

COPY . .
WORKDIR "/src/Servidor"

RUN dotnet build "Servidor.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Servidor.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS final
WORKDIR /app
EXPOSE 8080
EXPOSE 8081

COPY Servidor/ /app/Servidor/
COPY --from=publish /app/publish /app/publish

WORKDIR /app/Servidor
RUN dotnet restore "Servidor.csproj" && dotnet tool restore
ENV PATH="${PATH}:/root/.dotnet/tools"

ENTRYPOINT ["dotnet", "/app/publish/Servidor.dll"]