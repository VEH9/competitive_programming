FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src
COPY ["HomeWork1/HomeWork1.csproj", "HomeWork1/"]
RUN dotnet restore "HomeWork1/HomeWork1.csproj"
COPY . .
WORKDIR "/src/HomeWork1"
RUN dotnet build "HomeWork1.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "HomeWork1.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "HomeWork1.dll"]
