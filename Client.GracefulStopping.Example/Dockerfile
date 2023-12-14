FROM mcr.microsoft.com/dotnet/aspnet:7.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:7.0 AS build
WORKDIR /src
COPY ["Client.GracefulStopping.Example/Client.GracefulStopping.Example.csproj", "Client.GracefulStopping.Example/"]
RUN dotnet restore "Client.GracefulStopping.Example/Client.GracefulStopping.Example.csproj"
COPY . .
WORKDIR "/src/Client.GracefulStopping.Example"
RUN dotnet build "Client.GracefulStopping.Example.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Client.GracefulStopping.Example.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Client.GracefulStopping.Example.dll"]
