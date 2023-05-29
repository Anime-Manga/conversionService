#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/runtime:5.0 AS base

RUN apt update
RUN apt install -y ffmpeg

WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:5.0 AS build
WORKDIR /src

COPY ["src/Cesxhin.AnimeManga.ConversionService/", "./Cesxhin.AnimeManga.ConversionService/"]
COPY ["src/Cesxhin.AnimeManga.Application/", "./Cesxhin.AnimeManga.Application/"]
COPY ["src/references/Cesxhin.AnimeManga.Modules/", "./references/Cesxhin.AnimeManga.Modules/"]
COPY ["src/references/Cesxhin.AnimeManga.Domain/", "./references/Cesxhin.AnimeManga.Domain/"]

RUN dotnet restore "./Cesxhin.AnimeManga.ConversionService/Cesxhin.AnimeManga.ConversionService.csproj"
COPY . .
WORKDIR "./Cesxhin.AnimeManga.ConversionService"

RUN dotnet build "Cesxhin.AnimeManga.ConversionService.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Cesxhin.AnimeManga.ConversionService.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Cesxhin.AnimeManga.ConversionService.dll"]