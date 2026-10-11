package main

import (
	"crypto/sha256"
	"crypto/subtle"
	"net/http"
)

// requireAuth wraps a handler so every endpoint needs
// "Authorization: Bearer $BRIDGE_TOKEN". Both sides are hashed first so the
// comparison takes constant time regardless of the presented length.
func requireAuth(token string, next http.Handler) http.Handler {
	expected := sha256.Sum256([]byte("Bearer " + token))
	return http.HandlerFunc(func(w http.ResponseWriter, r *http.Request) {
		provided := sha256.Sum256([]byte(r.Header.Get("Authorization")))
		if subtle.ConstantTimeCompare(provided[:], expected[:]) != 1 {
			writeJSON(w, http.StatusUnauthorized, map[string]string{"error": "unauthorized"})
			return
		}
		next.ServeHTTP(w, r)
	})
}
