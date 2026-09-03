FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src
COPY ["src/BlogApplication.Web.Host/BlogApplication.Web.Host.csproj", "src/BlogApplication.Web.Host/"]
COPY ["src/BlogApplication.Web.Core/BlogApplication.Web.Core.csproj", "src/BlogApplication.Web.Core/"]
COPY ["src/BlogApplication.Application/BlogApplication.Application.csproj", "src/BlogApplication.Application/"]
COPY ["src/BlogApplication.Core/BlogApplication.Core.csproj", "src/BlogApplication.Core/"]
COPY ["src/BlogApplication.EntityFrameworkCore/BlogApplication.EntityFrameworkCore.csproj", "src/BlogApplication.EntityFrameworkCore/"]
WORKDIR "/src/src/BlogApplication.Web.Host"
RUN dotnet restore 

WORKDIR /src
COPY ["src/BlogApplication.Web.Host", "src/BlogApplication.Web.Host"]
COPY ["src/BlogApplication.Web.Core", "src/BlogApplication.Web.Core"]
COPY ["src/BlogApplication.Application", "src/BlogApplication.Application"]
COPY ["src/BlogApplication.Core", "src/BlogApplication.Core"]
COPY ["src/BlogApplication.EntityFrameworkCore", "src/BlogApplication.EntityFrameworkCore"]
WORKDIR "/src/src/BlogApplication.Web.Host"
RUN dotnet publish -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
EXPOSE 80
WORKDIR /app
COPY --from=build /publish .
ENTRYPOINT ["dotnet", "BlogApplication.Web.Host.dll"]
