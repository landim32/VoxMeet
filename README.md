# VoxMeet - Real-Time AI Interview Assistant

![.NET](https://img.shields.io/badge/.NET-8.0-blue)
![WPF](https://img.shields.io/badge/WPF-Desktop-blueviolet)
![License](https://img.shields.io/badge/License-MIT-green)
![OpenAI](https://img.shields.io/badge/OpenAI-Whisper%20%2B%20GPT--4o--mini-orange)
![Version](https://img.shields.io/badge/dynamic/xml?url=https%3A%2F%2Fraw.githubusercontent.com%2Flandim32%2FVoxMeet%2Fmain%2FVoxMeet%2FVoxMeet.csproj&query=%2F%2FVersion&label=Version&color=brightgreen)

## Overview

**VoxMeet** is a WPF desktop application that acts as a real-time AI interview assistant. It captures system audio or microphone input, transcribes speech using OpenAI Whisper, detects questions, and sends them to ChatGPT for instant answers — all displayed in a transparent, always-on-top overlay. Built with **.NET 8**, **NAudio**, and the **OpenAI SDK**.

Designed for technical interviews and meetings, VoxMeet runs as a sleek, semi-transparent overlay that stays on top of all windows, providing real-time transcription and AI-powered answers without disrupting your workflow. The application title bar displays the current version (e.g., **VoxMeet v0.2.3**), which is automatically managed by the CI/CD pipeline.

---

## 🚀 Features

- 🎙️ **Dual Audio Capture** - Capture system audio (loopback) or microphone input via WASAPI
- 📝 **Real-Time Transcription** - Speech-to-text powered by OpenAI Whisper API
- 🤖 **AI-Powered Answers** - Automatic question detection and answers via GPT-4o-mini
- 🪟 **Transparent Overlay** - Always-on-top, semi-transparent window with click-through output area
- 🎨 **Customizable Appearance** - Configurable colors, font sizes, and background opacity
- ⚙️ **Configurable System Prompt** - Editable system prompt to tailor AI responses to your scenario
- 🔊 **Multi-Channel Audio Support** - Handles mono, stereo, and surround sound audio sources
- 🔇 **Silence Detection** - Automatically sends buffered text as a question when silence is detected
- 🧪 **Simulation Mode** - Test the full pipeline without an API key or audio device
- 💾 **Persistent Settings** - All preferences saved locally to `%AppData%/VoxMeet/settings.json`
- 🏷️ **Automatic Versioning** - Version displayed in the title bar, managed via GitVersion and CI/CD

---

## 🛠️ Technologies Used

### Core Framework
- **.NET 8 (WPF)** - Windows desktop application framework with XAML-based UI

### Audio Processing
- **NAudio 2.2.1** - WASAPI audio capture, resampling (16kHz/16-bit/mono PCM), and WAV packaging

### AI Services
- **OpenAI SDK 2.8.0** - Whisper transcription (`whisper-1`) and Chat completion (`gpt-4o-mini`)

### DevOps
- **GitHub Actions** - Automated semantic versioning, version stamping, and release creation
- **GitVersion 5.x** - Semantic version calculation from commit message prefixes

---

## 📁 Project Structure

```
VoxMeet/
├── .github/
│   └── workflows/
│       ├── create-release.yml       # Automated GitHub release creation
│       └── version-tag.yml          # Semantic version tagging + project file update
├── VoxMeet/
│   ├── Converters/
│   │   └── BoolToTextConverter.cs   # Bool-to-Start/Stop text converter
│   ├── Models/
│   │   └── AppSettings.cs           # Settings POCO, persisted to JSON
│   ├── Services/
│   │   ├── AudioService.cs          # WASAPI capture, resampling, buffering
│   │   ├── ChatGPTService.cs        # GPT-4o-mini chat completion
│   │   ├── SimulationService.cs     # Simulated pipeline for testing without API
│   │   └── TranscriptionService.cs  # Whisper speech-to-text
│   ├── App.xaml                     # Global styles, no StartupUri
│   ├── App.xaml.cs                  # Manual MainWindow startup
│   ├── MainWindow.xaml              # Transparent overlay UI
│   ├── MainWindow.xaml.cs           # Event subscriptions, UI updates
│   ├── SettingsWindow.xaml          # Three-tab settings dialog
│   ├── SettingsWindow.xaml.cs       # Settings load/save logic
│   └── VoxMeet.csproj              # Project configuration (includes version)
├── .gitignore
├── GitVersion.yml                   # Semantic versioning rules
├── LICENSE                          # MIT License
├── README.md                        # This file
└── VoxMeet.sln                      # Solution file
```

---

## ⚙️ Prerequisites

- **Windows 10/11** (WPF is Windows-only)
- **.NET 8 SDK** - [Download](https://dotnet.microsoft.com/download/dotnet/8.0)
- **OpenAI API Key** - [Get one here](https://platform.openai.com/api-keys) (optional if using Simulation Mode)

---

## 🔧 Setup

### 1. Clone the Repository

```bash
git clone https://github.com/landim32/VoxMeet.git
cd VoxMeet
```

### 2. Build the Solution

```bash
dotnet build VoxMeet.sln
```

### 3. Run the Application

```bash
dotnet run --project VoxMeet/VoxMeet.csproj
```

### 4. Configure Your API Key

1. Click the **gear icon** (⚙️) in the overlay toolbar
2. Enter your **OpenAI API Key** in the General tab
3. Select your preferred **audio source** (System Audio or Microphone)
4. Adjust the **buffer duration** (3, 5, 8, or 10 seconds)
5. Click **Save**

> **Tip:** Enable **Simulation Mode** in Settings to test the full pipeline without an API key or audio device.

---

## 🏗️ Architecture

VoxMeet follows an **event-driven pipeline** pattern with code-behind (no MVVM framework):

```
AudioService (NAudio WASAPI) → AudioChunkReady event
  → TranscriptionService (Whisper API) → TranscriptionReceived event
    → Question buffer accumulates text until '?' detected
      → ChatGPTService (gpt-4o-mini) → AnswerReceived event
        → MainWindow displays via Dispatcher.Invoke
```

All three services emit events; `MainWindow` subscribes and marshals UI updates to the dispatcher thread. Audio is resampled to 16kHz/16-bit/mono PCM, buffered for N seconds, then wrapped as WAV in-memory before sending to Whisper.

### Version Display

The application reads its version from the assembly metadata at startup and displays it in the title bar (e.g., **VoxMeet v0.2.3**). The version is set in `VoxMeet.csproj` via the `<Version>`, `<AssemblyVersion>`, and `<FileVersion>` properties, which are automatically updated by the CI/CD pipeline on each push to `main`.

---

## 🎨 Customization

VoxMeet offers extensive appearance customization through the **Settings > Appearance** tab:

| Setting | Default | Description |
|---------|---------|-------------|
| Background Color | `#1E1E2E` | Overlay background color (hex) |
| Background Opacity | `80%` | Overlay transparency (10–100%) |
| Transcription Font Size | `17` | Size of transcribed text |
| Transcription Color | `#EEEEF0` | Color of transcribed text |
| Answer Font Size | `19` | Size of AI answer text |
| Answer Color | `#94E2D5` | Color of AI answer text (teal) |
| Log Font Size | `11` | Size of status log text |
| Log Color | `#6C7086` | Color of status log text (muted grey) |
| Show Log | `true` | Toggle status log visibility |

The **system prompt** can be customized in the **Settings > Prompt** tab to tailor AI responses for different scenarios.

### Behavior Settings

| Setting | Default | Description |
|---------|---------|-------------|
| Silence Detection | `true` | Automatically sends buffered text when silence is detected |
| Simulation Mode | `false` | Runs a simulated pipeline without API calls or audio capture |
| Whisper Model | `whisper-1` | OpenAI Whisper model used for transcription |

---

## 🔄 CI/CD

### GitHub Actions

**Workflow 1: Version and Tag** (`version-tag.yml`)
- Triggers on push to `main` or manual dispatch
- Uses GitVersion to calculate semantic version from commit prefixes
- Updates `<Version>`, `<AssemblyVersion>`, and `<FileVersion>` in `VoxMeet.csproj`
- Commits the version update with `[skip ci]` to avoid recursive triggers
- Creates and pushes a git tag (e.g., `v0.2.3`)

```
Push to main → GitVersion calculates version
  → Updates Version/AssemblyVersion/FileVersion in .csproj
  → Commits + pushes with [skip ci]
  → Creates git tag (e.g., v0.2.3)
  → App title bar displays "VoxMeet v0.2.3"
```

**Workflow 2: Create Release** (`create-release.yml`)
- Triggers after successful version tagging
- Creates a release branch (e.g., `releases/v0.2.0`)
- Creates a GitHub Release with auto-generated release notes for major/minor bumps
- Patch-only changes skip release creation

### Commit Message Conventions

| Prefix | Version Bump | Example |
|--------|-------------|---------|
| `feat:` / `feature:` | Minor | `feat: add dark mode support` |
| `fix:` | Patch | `fix: resolve audio dropout issue` |
| `major:` / `breaking:` | Major | `major: redesign settings system` |

---

## 🔍 Troubleshooting

### Common Issues

#### No audio is being captured

**Common causes:**
- Wrong audio source selected (Loopback vs Microphone)
- No audio playing on the system when using Loopback mode
- Audio device not available or disabled in Windows settings

**Solutions:**
- Open Settings and verify the correct audio mode and device are selected
- Ensure audio is actively playing when using System Audio (Loopback)
- Check Windows Sound settings to confirm your device is enabled

#### Transcription returns empty results

**Common causes:**
- Invalid or expired OpenAI API key
- Buffer duration too short for the speech being captured
- Very low audio volume

**Solutions:**
- Verify your API key in Settings > General
- Increase the buffer duration to 8 or 10 seconds
- Check your system audio levels

#### Overlay is not visible

**Common causes:**
- Background opacity set too low
- Window moved off-screen

**Solutions:**
- Restart the application (it positions at bottom-center of the screen by default)
- Adjust opacity in Settings > Appearance

#### Testing without API key or audio device

**Solution:**
- Enable **Simulation Mode** in Settings > General to run the full pipeline with simulated questions and answers

---

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

### Development Setup

1. Fork the repository
2. Create a feature branch (`git checkout -b feature/AmazingFeature`)
3. Make your changes
4. Build and verify (`dotnet build VoxMeet.sln`)
5. Commit your changes using the [commit conventions](#commit-message-conventions) (`git commit -m 'feat: add some AmazingFeature'`)
6. Push to the branch (`git push origin feature/AmazingFeature`)
7. Open a Pull Request

### Coding Standards

- File-scoped namespaces (`namespace VoxMeet.Services;`)
- Code-behind pattern with services separated from UI
- Colors stored as hex strings, parsed at runtime via `ColorConverter.ConvertFromString`

---

## 👨‍💻 Author

Developed by **[Rodrigo Landim Carneiro](https://github.com/landim32)**

---

## 📄 License

This project is licensed under the **MIT License** - see the [LICENSE](LICENSE) file for details.

---

## 🙏 Acknowledgments

- Built with [.NET 8](https://dotnet.microsoft.com/) and [WPF](https://learn.microsoft.com/en-us/dotnet/desktop/wpf/)
- Audio processing powered by [NAudio](https://github.com/naudio/NAudio)
- AI capabilities provided by [OpenAI](https://openai.com/) (Whisper + GPT-4o-mini)
- Semantic versioning powered by [GitVersion](https://gitversion.net/)

---

## 📞 Support

- **Issues**: [GitHub Issues](https://github.com/landim32/VoxMeet/issues)
- **Discussions**: [GitHub Discussions](https://github.com/landim32/VoxMeet/discussions)

---

**⭐ If you find this project useful, please consider giving it a star!**
