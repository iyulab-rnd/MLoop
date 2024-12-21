FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS runtime
WORKDIR /app
EXPOSE 80
EXPOSE 443
# 환경 변수 설정
ENV ASPNETCORE_URLS="http://+:80"
ENV Storage__BasePath="/var/data/mloop"
ENV WebRootPath="/var/data/wwwroot"

COPY publish ./
ENTRYPOINT ["dotnet", "MLoop.Api.dll"]

# 볼륨 마운트 지점 선언
VOLUME ["/var/data"]