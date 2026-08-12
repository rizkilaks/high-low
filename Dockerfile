# Stage 1: frontend build (wwwroot/js + wwwroot/lib are gitignored build artifacts)
FROM node:22-alpine AS frontend
WORKDIR /build
COPY package.json package-lock.json tsconfig.json ./
COPY scripts/ scripts/
COPY src/HighLow/wwwroot/ts src/HighLow/wwwroot/ts
RUN npm ci && npm run build

# Stage 2: publish
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY src/HighLow/HighLow.csproj src/HighLow/
RUN dotnet restore src/HighLow/HighLow.csproj
COPY src/HighLow/ src/HighLow/
COPY --from=frontend /build/src/HighLow/wwwroot/js src/HighLow/wwwroot/js
COPY --from=frontend /build/src/HighLow/wwwroot/lib src/HighLow/wwwroot/lib
RUN rm -rf src/HighLow/wwwroot/ts
RUN dotnet publish src/HighLow/HighLow.csproj -c Release --no-restore -o /out

# Stage 3: runtime
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /out .
RUN chown -R app:app /app
USER app
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "HighLow.dll"]
