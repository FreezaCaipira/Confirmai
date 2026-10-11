package main

import (
	"fmt"
	"os"
	"strings"
)

// config is the bridge's runtime configuration. All values come from env so
// the container stays stateless apart from the session DB volume.
type config struct {
	Addr    string   // BRIDGE_ADDR — listen address (default :8090)
	Token   string   // BRIDGE_TOKEN — required; Bearer credential for every endpoint
	DBPath  string   // BRIDGE_DB_PATH — sqlite file persisted on a volume
	Allowed []string // BRIDGE_ALLOWED_GROUP_JIDS — comma-separated @g.us allowlist
}

func loadConfig() (config, error) {
	cfg := config{
		Addr:   envOr("BRIDGE_ADDR", ":8090"),
		Token:  os.Getenv("BRIDGE_TOKEN"),
		DBPath: envOr("BRIDGE_DB_PATH", "file:session.db?cache=shared&_pragma=foreign_keys(1)&_pragma=busy_timeout(5000)"),
	}
	if cfg.Token == "" {
		return config{}, fmt.Errorf("BRIDGE_TOKEN is required")
	}
	for _, jid := range strings.Split(os.Getenv("BRIDGE_ALLOWED_GROUP_JIDS"), ",") {
		jid = strings.TrimSpace(jid)
		if jid == "" {
			continue
		}
		if !validGroupJID(jid) {
			return config{}, fmt.Errorf("BRIDGE_ALLOWED_GROUP_JIDS contains invalid JID %q", jid)
		}
		cfg.Allowed = append(cfg.Allowed, jid)
	}
	if len(cfg.Allowed) == 0 {
		return config{}, fmt.Errorf("BRIDGE_ALLOWED_GROUP_JIDS must allow at least one group")
	}
	return cfg, nil
}

func envOr(key, fallback string) string {
	if v := os.Getenv(key); v != "" {
		return v
	}
	return fallback
}

// validGroupJID: WhatsApp groups end in @g.us with a non-empty local part.
func validGroupJID(jid string) bool {
	local, ok := strings.CutSuffix(jid, "@g.us")
	return ok && local != "" && !strings.ContainsAny(local, "@ \t")
}
