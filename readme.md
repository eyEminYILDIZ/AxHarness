# AxHarness

AxHarness is a cli coding agent harness. The console coding agent (Linux only) is built on Microsoft Agent Framework with OpenRouter (OpenAI-compatible API).

| **Category**        | **Details**                   |
| ------------------- | ----------------------------- |
| LLM Provider        | **OpenRouter**                |
| Language            | **C#**                        |
| Library / Framework | **Microsoft Agent Framework** |

- Tools: `read_file`, `write_file`, `list_dir`, `run_command` (bash). File tools are confined to the root directory (the directory the app was started in). `write_file` / `run_command` ask for approval (`yes` / `no`) unless `--yes` is passed. Note: shell commands are only limited by their working directory, so the approval prompt is the real guard.
- Prompt: built-in static prompt + `Agents.md` from the root directory (if present).
- Sessions are logged as JSONL in `<root>/.axharness/sessions/`; resume with `--resume <id|latest>`.

## Setup

```bash
dotnet user-secrets set "OpenRouter:ApiKey" "<your key>" --project src/AxHarness
```

(`OPENROUTER_API_KEY` env var works as a fallback.)

## Run

```bash
cd <your project>
dotnet run --project <path-to-AxHarness>/src/AxHarness -- [--model <id>] [--yes] [--resume latest]
```

Try it on the (gitignored) playground: `cd examples/example_dotnet_1 && dotnet run --project ../../src/AxHarness`.
REPL commands: `/help`, `/sessions`, `/clear`, `/exit`. Tests: `dotnet test`.

## Features

- [x] Text based chatting
- [x] Tool calling (`read_file`, `write_file`, `list_dir`, `run_command`)
- [x] Prompt file support (Agents.md)
- [x] Session management (logging and resuming)
- [x] Approval prompts for potentially dangerous actions (`write_file` and `run_command`)
- [x] REPL support (`/help`, `/sessions`, `/clear`, `/exit`)
- [x] Test support (`dotnet test`)
- [ ] Image input support
