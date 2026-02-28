# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Build Commands

```bash
dotnet build VoxMeet.sln                          # Build solution
dotnet build VoxMeet/VoxMeet.csproj                # Build project only
dotnet run --project VoxMeet/VoxMeet.csproj        # Run the app
```

No test framework is configured. No linter is configured.

## Project Overview

VoxMeet is a WPF (.NET 8, `net8.0-windows`) desktop app that acts as a real-time AI interview assistant. It captures system audio or microphone input, transcribes speech via OpenAI Whisper, detects questions (by `?`), and sends them to ChatGPT for answers — all displayed in a transparent always-on-top overlay.

## Architecture

**Event-driven pipeline:**

```
AudioService (NAudio WASAPI) → AudioChunkReady event
  → TranscriptionService (Whisper API) → TranscriptionReceived event
    → Question buffer accumulates text until '?' detected
      → ChatGPTService (gpt-4o-mini) → AnswerReceived event
        → MainWindow displays via Dispatcher.Invoke
```

All three services emit events; MainWindow subscribes and marshals UI updates to the dispatcher thread. Audio is resampled to 16kHz/16-bit/mono PCM, buffered for N seconds, then wrapped as WAV in-memory before sending to Whisper.

## Key Files

- **Services/AudioService.cs** — WASAPI capture (loopback or mic), resampling via `WdlResamplingSampleProvider`, PCM buffering, WAV packaging. Handles multi-channel audio (stereo, surround).
- **Services/TranscriptionService.cs** — OpenAI `AudioClient("whisper-1")`. Emits `StatusChanged` for pipeline visibility.
- **Services/ChatGPTService.cs** — OpenAI `ChatClient("gpt-4o-mini")`. System prompt loaded from `AppSettings.SystemPrompt`.
- **Models/AppSettings.cs** — All settings as a POCO, persisted to `%AppData%/VoxMeet/settings.json` via `System.Text.Json`.
- **MainWindow.xaml/.cs** — Transparent overlay (`WindowStyle=None`, `AllowsTransparency=True`, `Topmost=True`). Uses `RichTextBox` with `FlowDocument` for color-coded output (log=dim, transcription=white, answer=cyan/bold). Implements `INotifyPropertyChanged` for `IsRunning` binding.
- **SettingsWindow.xaml/.cs** — Three tabs: General (API key, audio source/device, buffer), Appearance (background color/opacity, per-element font size and color), Prompt (editable system prompt).
- **App.xaml** — Global button style with rounded corners and hover/press opacity triggers. No `StartupUri`; MainWindow created in `OnStartup`.

## Dependencies

- **NAudio 2.2.1** — Audio capture and resampling
- **OpenAI 2.8.0** — Whisper transcription + Chat completion. Key API: `TranscribeAudioAsync(Stream, filename, options?, ct)` returns `AudioTranscription.Text`; `CompleteChatAsync(messages)` returns `ChatCompletion.Content[0].Text`.

## Conventions

- File-scoped namespaces (`namespace VoxMeet.Services;`)
- Code-behind pattern (no MVVM framework), services separated from UI
- Colors stored as hex strings in settings, parsed at runtime via `ColorConverter.ConvertFromString`
- Semantic versioning via commit prefixes: `feat:`/`feature:` (minor), `fix:` (patch), `major:`/`breaking:` (major)
