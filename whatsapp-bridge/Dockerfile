# syntax=docker/dockerfile:1
FROM --platform=$BUILDPLATFORM golang:1.27 AS build
WORKDIR /src
COPY go.mod go.sum ./
RUN go mod download
COPY . .
ARG TARGETOS TARGETARCH
RUN CGO_ENABLED=0 GOOS=${TARGETOS:-linux} GOARCH=${TARGETARCH:-amd64} \
    go build -trimpath -ldflags="-s -w" -o /out/whatsapp-bridge .
RUN mkdir -p /out/data

# distroless: no shell, no package manager — nothing to attack but the binary
FROM gcr.io/distroless/static-debian12:nonroot
COPY --from=build /out/whatsapp-bridge /whatsapp-bridge
# /data must belong to nonroot before VOLUME: a fresh named volume inherits it
COPY --from=build --chown=nonroot:nonroot /out/data /data
ENV BRIDGE_DB_PATH=/data/session.db
VOLUME ["/data"]
USER nonroot
EXPOSE 8090
ENTRYPOINT ["/whatsapp-bridge"]
