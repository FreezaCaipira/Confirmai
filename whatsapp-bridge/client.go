package main

import (
	"context"
	"sync/atomic"

	"go.mau.fi/whatsmeow"
	waE2E "go.mau.fi/whatsmeow/proto/waE2E"
	"go.mau.fi/whatsmeow/types"
	"go.mau.fi/whatsmeow/types/events"
	"google.golang.org/protobuf/proto"
)

// GroupInfo is a WhatsApp group the session belongs to.
type GroupInfo struct {
	JID  string `json:"jid"`
	Name string `json:"name"`
}

// Client abstracts the WhatsApp session so HTTP handlers stay testable.
type Client interface {
	Connected() bool
	LoggedIn() bool
	Groups(ctx context.Context) ([]GroupInfo, error)
	SendGroupText(ctx context.Context, jid, text string) (messageID string, err error)
	// PairQRCode returns the latest QR payload for pairing, or "" when none is
	// pending (already paired, or not yet generated).
	PairQRCode() string
}

type whatsClient struct {
	cli      *whatsmeow.Client
	latestQR atomic.Value // string
}

func newWhatsClient(cli *whatsmeow.Client) *whatsClient {
	wc := &whatsClient{cli: cli}
	cli.AddEventHandler(func(evt interface{}) {
		switch evt.(type) {
		case *events.Connected, *events.OfflineSyncCompleted:
			// connection state is read live via cli.IsConnected(); nothing to cache
		case *events.PairSuccess, *events.LoggedOut:
			wc.latestQR.Store("")
		}
	})
	return wc
}

// watchQR consumes the QR channel returned by client.GetQRChannel and keeps
// only the most recent code; older ones expire quickly.
func (wc *whatsClient) watchQR(qrChan <-chan whatsmeow.QRChannelItem) {
	for item := range qrChan {
		switch item.Event {
		case whatsmeow.QRChannelSuccess.Event, whatsmeow.QRChannelTimeout.Event:
			wc.latestQR.Store("")
		default:
			if item.Code != "" {
				wc.latestQR.Store(item.Code)
			}
		}
	}
}

func (wc *whatsClient) Connected() bool { return wc.cli.IsConnected() }
func (wc *whatsClient) LoggedIn() bool  { return wc.cli.IsLoggedIn() }

func (wc *whatsClient) PairQRCode() string {
	v, _ := wc.latestQR.Load().(string)
	return v
}

func (wc *whatsClient) Groups(ctx context.Context) ([]GroupInfo, error) {
	groups, err := wc.cli.GetJoinedGroups(ctx)
	if err != nil {
		return nil, err
	}
	out := make([]GroupInfo, 0, len(groups))
	for _, g := range groups {
		out = append(out, GroupInfo{JID: g.JID.String(), Name: g.Name})
	}
	return out, nil
}

func (wc *whatsClient) SendGroupText(ctx context.Context, jidStr, text string) (string, error) {
	jid, err := types.ParseJID(jidStr)
	if err != nil {
		return "", err
	}
	resp, err := wc.cli.SendMessage(ctx, jid, &waE2E.Message{Conversation: proto.String(text)})
	if err != nil {
		return "", err
	}
	return string(resp.ID), nil
}
