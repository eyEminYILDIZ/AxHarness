# AxHarness

AxHarness is a cli coding agent harness. The console coding agent (Linux, macOS and Windows) is built on Microsoft Agent Framework with OpenRouter (OpenAI-compatible API).

| **Category**        | **Details**                   |
| ------------------- | ----------------------------- |
| LLM Provider        | **OpenRouter**                |
| Language            | **C#**                        |
| Library / Framework | **Microsoft Agent Framework** |

- Tools: `read_file`, `write_file`, `list_dir`, `run_command` (bash/sh on Linux and macOS; PowerShell, else cmd, on Windows). File tools are confined to the root directory (the directory the app was started in). `write_file` / `run_command` ask for approval (`yes` / `no`) unless `--yes` is passed. Note: shell commands are only limited by their working directory, so the approval prompt is the real guard.
- Prompt: built-in static prompt + `Agents.md` from the root directory (if present).
- Sessions live in `<root>/.axharness/sessions/<session-id>/`: `session.jsonl` (the log) and `attachments/` (copies of every `@path` file, one snapshot per turn). Resume with `--resume <id|latest>`.

## Setup

```bash
dotnet user-secrets set "OpenRouter:ApiKey" "<your key>" --project src/AxHarness
```

(`OPENROUTER_API_KEY` env var works as a fallback. User secrets live in `~/.microsoft/usersecrets` on Linux/macOS and `%APPDATA%\Microsoft\UserSecrets` on Windows.)

On Windows use PowerShell or cmd for the commands below (replace `cd`/paths as needed). The shell used by `run_command` is picked automatically (`pwsh`, then `powershell`, then `cmd` on Windows; `bash`, then `sh` elsewhere). Override it with `AXHARNESS_SHELL=bash|sh|pwsh|powershell|cmd`, e.g. to use Git Bash on Windows.

## Run

```bash
cd <your project>
dotnet run --project <path-to-AxHarness>/src/AxHarness -- [--model <id>] [--yes] [--resume latest]
```

Try it on the (gitignored) playground: `cd examples/example_dotnet_1 && dotnet run --project ../../src/AxHarness`.
REPL commands: `/help`, `/sessions`, `/usage`, `/clear`, `/exit`. Attach files or images with `@path` (e.g. `fix the bugs in @src/Foo.cs`; `@"a b.txt"` for spaces, `\@` for a literal `@`). Tests: `dotnet test`.

## Features

- [x] F001 - Text based chatting
- [x] F002 - Tool calling (`read_file`, `write_file`, `list_dir`, `run_command`)
- [x] F003 - Prompt file support (Agents.md)
- [x] F004 - Session management (logging and resuming)
- [x] F005 - Approval prompts for potentially dangerous actions (`write_file` and `run_command`)
- [x] F006 - REPL support (`/help`, `/sessions`, `/clear`, `/exit`)
- [x] F007 - Test support (`dotnet test`)
- [x] F008 - File input support (`@path` in a message attaches the file's contents)
- [x] F009 - Image input support (`@path` to a png/jpeg/gif/webp image sends it to the model; max 4 images, 5 MB each, 10 MB total; the model must support vision)
- [x] F010 - Cross-platform support (Linux, macOS, Windows)
- [x] F011 - Token and Context statistics (after every response: context used / limit, turn and session tokens; `/usage` for the session summary; limit comes from OpenRouter's model list, override with `--context-limit <n>`; falls back to an assumed 65,536 if the model is not listed; restored on `--resume`)
