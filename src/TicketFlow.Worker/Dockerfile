FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
COPY ["TicketFlow.sln", "./"]
COPY ["src/TicketFlow.Worker/TicketFlow.Worker.csproj", "src/TicketFlow.Worker/"]
COPY ["src/TicketFlow.Application/TicketFlow.Application.csproj", "src/TicketFlow.Application/"]
COPY ["src/TicketFlow.Domain/TicketFlow.Domain.csproj", "src/TicketFlow.Domain/"]
COPY ["src/TicketFlow.Infrastructure/TicketFlow.Infrastructure.csproj", "src/TicketFlow.Infrastructure/"]
RUN dotnet restore "src/TicketFlow.Worker/TicketFlow.Worker.csproj"
COPY . .
RUN dotnet publish "src/TicketFlow.Worker/TicketFlow.Worker.csproj" --configuration Release --output /app/publish --no-restore

FROM runtime AS final
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TicketFlow.Worker.dll"]
