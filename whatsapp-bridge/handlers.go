package main

import (
	"encoding/json"
	"net/http"
	"strings"

	qrcode "github.com/skip2/go-qrcode"
)

// newMux builds the HTTP surface. Every route sits behind the Bearer token;
// allowed maps JID -> struct{} for the send allowlist.
func newMux(cli Client, token string, allowed map[string]struct{}) http.Handler {
	mux := http.NewServeMux()

	mux.Handle("GET /health", http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		writeJSON(w, http.StatusOK, map[string]bool{
			"connected": cli.Connected(),
			"loggedIn":  cli.LoggedIn(),
		})
	}))

	mux.Handle("GET /pair", http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		if cli.LoggedIn() {
			writeJSON(w, http.StatusConflict, map[string]string{"error": "already paired"})
			return
		}
		code := cli.PairQRCode()
		if code == "" {
			writeJSON(w, http.StatusServiceUnavailable, map[string]string{"error": "no qr code pending"})
			return
		}
		png, err := qrcode.Encode(code, qrcode.Medium, 512)
		if err != nil {
			writeJSON(w, http.StatusInternalServerError, map[string]string{"error": "qr render failed"})
			return
		}
		w.Header().Set("Content-Type", "image/png")
		w.WriteHeader(http.StatusOK)
		_, _ = w.Write(png)
	}))

	mux.Handle("GET /groups", http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		groups, err := cli.Groups(r.Context())
		if err != nil {
			writeJSON(w, http.StatusInternalServerError, map[string]string{"error": "could not list groups"})
			return
		}
		writeJSON(w, http.StatusOK, groups)
	}))

	mux.Handle("POST /send/group-text", http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		var body struct {
			JID  string `json:"jid"`
			Text string `json:"text"`
		}
		if err := json.NewDecoder(r.Body).Decode(&body); err != nil {
			writeJSON(w, http.StatusBadRequest, map[string]string{"error": "invalid json"})
			return
		}
		if !validGroupJID(body.JID) {
			writeJSON(w, http.StatusBadRequest, map[string]string{"error": "invalid group jid"})
			return
		}
		if _, ok := allowed[body.JID]; !ok {
			writeJSON(w, http.StatusForbidden, map[string]string{"error": "jid not allowed"})
			return
		}
		if strings.TrimSpace(body.Text) == "" {
			writeJSON(w, http.StatusBadRequest, map[string]string{"error": "empty text"})
			return
		}
		id, err := cli.SendGroupText(r.Context(), body.JID, body.Text)
		if err != nil {
			// do not leak the error body — it may echo the message text
			writeJSON(w, http.StatusBadGateway, map[string]string{"error": "send failed"})
			return
		}
		writeJSON(w, http.StatusOK, map[string]string{"messageId": id})
	}))

	return requireAuth(token, mux)
}

func writeJSON(w http.ResponseWriter, status int, v any) {
	w.Header().Set("Content-Type", "application/json")
	w.WriteHeader(status)
	_ = json.NewEncoder(w).Encode(v)
}
