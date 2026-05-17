FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 8080

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["TicketFlow.sln", "./"]
COPY ["src/TicketFlow.Api/TicketFlow.Api.csproj", "src/TicketFlow.Api/"]
COPY ["src/TicketFlow.Application/TicketFlow.Application.csproj", "src/TicketFlow.Application/"]
COPY ["src/TicketFlow.Domain/TicketFlow.Domain.csproj", "src/TicketFlow.Domain/"]
COPY ["src/TicketFlow.Infrastructure/TicketFlow.Infrastructure.csproj", "src/TicketFlow.Infrastructure/"]
RUN dotnet restore "src/TicketFlow.Api/TicketFlow.Api.csproj"
COPY . .
RUN dotnet publish "src/TicketFlow.Api/TicketFlow.Api.csproj" --configuration Release --output /app/publish --no-restore

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TicketFlow.Api.dll"]
