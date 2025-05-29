FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["PasteBinApi/PasteBinApi.csproj", "PasteBinApi/"]
RUN dotnet restore "PasteBinApi/PasteBinApi.csproj"
COPY . .
WORKDIR "/src/PasteBinApi"
RUN dotnet build "PasteBinApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "PasteBinApi.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "PasteBinApi.dll"]
