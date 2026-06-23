FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

COPY MiloBnb.slnx .
COPY src/Milo.Domain/Milo.Domain.csproj             src/Milo.Domain/
COPY src/Milo.Application/Milo.Application.csproj   src/Milo.Application/
COPY src/Milo.Infraestructure/Milo.Infraestructure.csproj src/Milo.Infraestructure/
COPY src/Milo.Api/Milo.Api.csproj                   src/Milo.Api/

RUN dotnet restore src/Milo.Api/Milo.Api.csproj

COPY . .

RUN dotnet publish src/Milo.Api/Milo.Api.csproj \
    -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app

RUN useradd -r -s /bin/false appuser
USER appuser

COPY --from=build /app/publish .

EXPOSE 8080
ENTRYPOINT ["dotnet", "Milo.Api.dll"]
