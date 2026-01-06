# Tuanjie Agent Client

Provides integration of any AI agent (Codely CLI, Gemini CLI, Claude Code, Codex CLI, etc.) with the Unity editor using Agent Client Protocol (ACP). Inspired by nuskey's UnityAgentClient package.

## Installation

Copy the `cn.tuanjie.codely.agent-client` folder to your Unity project's `Packages/` directory.

## Requirements

- Unity 2022.3 or later

## Setup

1. Configure the AI agent in `Project Settings > Unity Agent Client`:
   - Command: The executable name of your AI agent
   - Arguments: Command-line arguments (e.g., `acp`, `--experimental-acp`)
   - Environment Variables: API keys and other environment variables

2. Open the AI Agent window from `Window > Unity Agent Client > AI Agent`

## Core Components

- **AgentWindow.cs**: Main editor window for AI agent interaction
- **AgentSettingsProvider.cs**: Settings provider for project configuration
- **AgentSettings.cs**: Serializable settings data model
- **EditorMarkdownRenderer.cs**: Custom markdown renderer for Unity IMGUI
- **AgentClientProtocol/**: Vendored ACP protocol library

## License

MIT License