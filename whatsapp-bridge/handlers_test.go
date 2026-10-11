package main

import (
	"context"
	"errors"
	"net/http"
	"net/http/httptest"
	"strings"
	"testing"
)

type fakeClient struct {
	connected bool
	loggedIn  bool
	qr        string
	groups    []GroupInfo
	groupsErr error
	sentJID   string
	sentText  string
	sendID    string
	sendErr   error
}

func (f *fakeClient) Connected() bool    { return f.connected }
func (f *fakeClient) LoggedIn() bool     { return f.loggedIn }
func (f *fakeClient) PairQRCode() string { return f.qr }
func (f *fakeClient) Groups(context.Context) ([]GroupInfo, error) {
	return f.groups, f.groupsErr
}
func (f *fakeClient) SendGroupText(_ context.Context, jid, text string) (string, error) {
	f.sentJID, f.sentText = jid, text
	return f.sendID, f.sendErr
}

const testToken = "test-token"

func testMux(cli Client) http.Handler {
	return newMux(cli, testToken, map[string]struct{}{
		"1203630@g.us": {},
	})
}

func doRequest(mux http.Handler, method, path, body, auth string) *httptest.ResponseRecorder {
	var req *http.Request
	if body != "" {
		req = httptest.NewRequest(method, path, strings.NewReader(body))
	} else {
		req = httptest.NewRequest(method, path, nil)
	}
	if auth != "" {
		req.Header.Set("Authorization", auth)
	}
	rec := httptest.NewRecorder()
	mux.ServeHTTP(rec, req)
	return rec
}

// ── auth ─────────────────────────────────────────────────────────────

func TestMissingToken_Returns401(t *testing.T) {
	mux := testMux(&fakeClient{})
	rec := doRequest(mux, "GET", "/health", "", "")
	if rec.Code != http.StatusUnauthorized {
		t.Fatalf("expected 401, got %d", rec.Code)
	}
}

func TestWrongToken_Returns401(t *testing.T) {
	mux := testMux(&fakeClient{})
	for _, path := range []string{"/health", "/pair", "/groups"} {
		rec := doRequest(mux, "GET", path, "", "Bearer wrong-token")
		if rec.Code != http.StatusUnauthorized {
			t.Fatalf("%s: expected 401, got %d", path, rec.Code)
		}
	}
	rec := doRequest(mux, "POST", "/send/group-text", `{"jid":"1203630@g.us","text":"x"}`, "Bearer wrong")
	if rec.Code != http.StatusUnauthorized {
		t.Fatalf("send: expected 401, got %d", rec.Code)
	}
}

// ── /health ──────────────────────────────────────────────────────────

func TestHealth_ReportsClientState(t *testing.T) {
	mux := testMux(&fakeClient{connected: true, loggedIn: true})
	rec := doRequest(mux, "GET", "/health", "", "Bearer "+testToken)
	if rec.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", rec.Code)
	}
	if !strings.Contains(rec.Body.String(), `"connected":true`) ||
		!strings.Contains(rec.Body.String(), `"loggedIn":true`) {
		t.Fatalf("unexpected body: %s", rec.Body.String())
	}
}

// ── /pair ────────────────────────────────────────────────────────────

func TestPair_WhenLoggedIn_Returns409(t *testing.T) {
	mux := testMux(&fakeClient{loggedIn: true})
	rec := doRequest(mux, "GET", "/pair", "", "Bearer "+testToken)
	if rec.Code != http.StatusConflict {
		t.Fatalf("expected 409, got %d", rec.Code)
	}
}

func TestPair_NoPendingCode_Returns503(t *testing.T) {
	mux := testMux(&fakeClient{loggedIn: false, qr: ""})
	rec := doRequest(mux, "GET", "/pair", "", "Bearer "+testToken)
	if rec.Code != http.StatusServiceUnavailable {
		t.Fatalf("expected 503, got %d", rec.Code)
	}
}

func TestPair_WithCode_ReturnsPNG(t *testing.T) {
	mux := testMux(&fakeClient{loggedIn: false, qr: "2@dummyqr"})
	rec := doRequest(mux, "GET", "/pair", "", "Bearer "+testToken)
	if rec.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", rec.Code)
	}
	if ct := rec.Header().Get("Content-Type"); ct != "image/png" {
		t.Fatalf("expected image/png, got %s", ct)
	}
	if len(rec.Body.Bytes()) < 8 || string(rec.Body.Bytes()[1:4]) != "PNG" {
		t.Fatal("response is not a PNG")
	}
}

// ── /groups ──────────────────────────────────────────────────────────

func TestGroups_ReturnsList(t *testing.T) {
	mux := testMux(&fakeClient{groups: []GroupInfo{
		{JID: "1203630@g.us", Name: "Pelada"},
		{JID: "777@g.us", Name: "Poker"},
	}})
	rec := doRequest(mux, "GET", "/groups", "", "Bearer "+testToken)
	if rec.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", rec.Code)
	}
	body := rec.Body.String()
	if !strings.Contains(body, "1203630@g.us") || !strings.Contains(body, "Pelada") {
		t.Fatalf("unexpected body: %s", body)
	}
}

func TestGroups_ClientError_Returns500(t *testing.T) {
	mux := testMux(&fakeClient{groupsErr: errors.New("disconnected")})
	rec := doRequest(mux, "GET", "/groups", "", "Bearer "+testToken)
	if rec.Code != http.StatusInternalServerError {
		t.Fatalf("expected 500, got %d", rec.Code)
	}
}

// ── /send/group-text ─────────────────────────────────────────────────

func TestSend_InvalidJID_Returns400(t *testing.T) {
	mux := testMux(&fakeClient{})
	for _, jid := range []string{"", "not-a-jid", "123@s.whatsapp.net", "@g.us"} {
		rec := doRequest(mux, "POST", "/send/group-text",
			`{"jid":"`+jid+`","text":"oi"}`, "Bearer "+testToken)
		if rec.Code != http.StatusBadRequest {
			t.Fatalf("jid %q: expected 400, got %d", jid, rec.Code)
		}
	}
}

func TestSend_JIDOutsideAllowlist_Returns403(t *testing.T) {
	mux := testMux(&fakeClient{})
	rec := doRequest(mux, "POST", "/send/group-text",
		`{"jid":"999@g.us","text":"oi"}`, "Bearer "+testToken)
	if rec.Code != http.StatusForbidden {
		t.Fatalf("expected 403, got %d", rec.Code)
	}
}

func TestSend_EmptyText_Returns400(t *testing.T) {
	mux := testMux(&fakeClient{})
	rec := doRequest(mux, "POST", "/send/group-text",
		`{"jid":"1203630@g.us","text":"  "}`, "Bearer "+testToken)
	if rec.Code != http.StatusBadRequest {
		t.Fatalf("expected 400, got %d", rec.Code)
	}
}

func TestSend_Success_ReturnsMessageID(t *testing.T) {
	cli := &fakeClient{sendID: "3EB0abc123"}
	mux := testMux(cli)
	rec := doRequest(mux, "POST", "/send/group-text",
		`{"jid":"1203630@g.us","text":"oi grupo"}`, "Bearer "+testToken)
	if rec.Code != http.StatusOK {
		t.Fatalf("expected 200, got %d", rec.Code)
	}
	if !strings.Contains(rec.Body.String(), `"messageId":"3EB0abc123"`) {
		t.Fatalf("unexpected body: %s", rec.Body.String())
	}
	if cli.sentJID != "1203630@g.us" || cli.sentText != "oi grupo" {
		t.Fatalf("client got wrong args: %q %q", cli.sentJID, cli.sentText)
	}
}

func TestSend_ClientError_Returns502_WithoutEchoingText(t *testing.T) {
	mux := testMux(&fakeClient{sendErr: errors.New("socket: oi grupo leaked")})
	rec := doRequest(mux, "POST", "/send/group-text",
		`{"jid":"1203630@g.us","text":"oi grupo"}`, "Bearer "+testToken)
	if rec.Code != http.StatusBadGateway {
		t.Fatalf("expected 502, got %d", rec.Code)
	}
	if strings.Contains(rec.Body.String(), "oi grupo") || strings.Contains(rec.Body.String(), "leaked") {
		t.Fatalf("error response leaked upstream detail: %s", rec.Body.String())
	}
}

func TestSend_InvalidJSON_Returns400(t *testing.T) {
	mux := testMux(&fakeClient{})
	rec := doRequest(mux, "POST", "/send/group-text", "{nope", "Bearer "+testToken)
	if rec.Code != http.StatusBadRequest {
		t.Fatalf("expected 400, got %d", rec.Code)
	}
}

// ── config / jid validation ──────────────────────────────────────────

func TestValidGroupJID(t *testing.T) {
	valid := []string{"1203630@g.us", "a@g.us"}
	for _, jid := range valid {
		if !validGroupJID(jid) {
			t.Fatalf("%q should be valid", jid)
		}
	}
	invalid := []string{"", "@g.us", "x@y@g.us", "x g.us", "x@g.usx", "1@s.whatsapp.net"}
	for _, jid := range invalid {
		if validGroupJID(jid) {
			t.Fatalf("%q should be invalid", jid)
		}
	}
}

func TestSQLiteDSN(t *testing.T) {
	cases := map[string]string{
		"/data/session.db":                  "file:/data/session.db?_pragma=foreign_keys(1)&_pragma=busy_timeout(5000)",
		"session.db":                        "file:session.db?_pragma=foreign_keys(1)&_pragma=busy_timeout(5000)",
		"file:x.db?_pragma=foreign_keys(1)": "file:x.db?_pragma=foreign_keys(1)",
	}
	for in, want := range cases {
		if got := sqliteDSN(in); got != want {
			t.Errorf("sqliteDSN(%q) = %q, want %q", in, got, want)
		}
	}
}
