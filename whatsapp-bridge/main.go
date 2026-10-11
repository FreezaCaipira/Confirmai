package main

import (
	"context"
	"errors"
	"log"
	"net/http"
	"os"
	"os/signal"
	"syscall"
	"time"

	_ "modernc.org/sqlite"

	"go.mau.fi/whatsmeow"
	"go.mau.fi/whatsmeow/store/sqlstore"
	waLog "go.mau.fi/whatsmeow/util/log"
)

func main() {
	if err := run(); err != nil {
		log.Fatal(err)
	}
}

func run() error {
	cfg, err := loadConfig()
	if err != nil {
		return err
	}

	ctx, stop := signal.NotifyContext(context.Background(), os.Interrupt, syscall.SIGTERM)
	defer stop()

	dbLog := waLog.Stdout("DB", "WARN", true)
	container, err := sqlstore.New(ctx, "sqlite", cfg.DBPath, dbLog)
	if err != nil {
		return err
	}
	device, err := container.GetFirstDevice(ctx)
	if err != nil {
		return err
	}

	waClient := whatsmeow.NewClient(device, waLog.Stdout("Client", "WARN", true))
	client := newWhatsClient(waClient)

	var qrChan <-chan whatsmeow.QRChannelItem
	if !waClient.IsLoggedIn() {
		if qrChan, err = waClient.GetQRChannel(ctx); err != nil {
			return err
		}
	}
	if err := waClient.Connect(); err != nil {
		return err
	}
	if qrChan != nil {
		go client.watchQR(qrChan)
	}

	allowed := make(map[string]struct{}, len(cfg.Allowed))
	for _, jid := range cfg.Allowed {
		allowed[jid] = struct{}{}
	}

	srv := &http.Server{
		Addr:              cfg.Addr,
		Handler:           newMux(client, cfg.Token, allowed),
		ReadHeaderTimeout: 5 * time.Second,
	}

	go func() {
		<-ctx.Done()
		shutdownCtx, cancel := context.WithTimeout(context.Background(), 5*time.Second)
		defer cancel()
		_ = srv.Shutdown(shutdownCtx)
		waClient.Disconnect()
	}()

	// never log the token, JIDs list or message text
	log.Printf("whatsapp-bridge listening on %s", cfg.Addr)
	if err := srv.ListenAndServe(); err != nil && !errors.Is(err, http.ErrServerClosed) {
		return err
	}
	return nil
}
