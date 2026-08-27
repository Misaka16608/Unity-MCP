---
name: Runtime input automation for Unity games
description: Add MCP tools for interacting with a running Unity game through mouse and keyboard input
type: feature-request
---

# Runtime input automation for Unity games

## Problem

Unity MCP can inspect the Editor and capture Game View screenshots, but it cannot directly perform user-like interaction in a running game. This makes it difficult to verify UI flows that depend on real clicks, text entry, keyboard shortcuts, or focus changes.

## Proposed capability

Add opt-in MCP tools for Play Mode and connected runtime players:

- Click at Game View or runtime-window coordinates, with optional mouse button and click count.
- Move the pointer and optionally send pointer down/up events.
- Send key down/up/press events, including modifier keys.
- Type a text string with configurable interval and an option to clear the focused field first.
- Return the target window/game focus state and an action result.

## Safety and determinism

- Require an explicit target (Editor Game View or connected runtime instance).
- Reject actions when Play Mode/runtime control is not enabled.
- Keep coordinate-space and display scaling explicit in the schema.
- Make destructive or focus-changing actions opt-in and report the dispatched event.

## Acceptance criteria

1. An agent can open a running Unity UI and click a named or coordinate-resolved button.
2. An agent can focus an input field and type text, including non-ASCII text where supported by the target.
3. An agent can send keyboard shortcuts and receive a structured success/failure response.
4. The tools work in automated EditMode/PlayMode integration tests without requiring OS-global input by default.
5. Tool documentation clearly states coordinate systems, focus behavior, supported platforms, and limitations.

