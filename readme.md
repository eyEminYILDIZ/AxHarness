# AxHarness

A basic coding agent harness.

| **Category**        | **Details**                   |
|---------------------|-------------------------------|
| LLM Provider        | **OpenRouter**                |
| Language            | **C#**                        |
| Library / Framework | **Microsoft Agent Framework** |

## NodeJS Example

A basic coding agent harness for interacting with the OpenRouter API using NVIDIA's Nemotron model.

### Prerequisites

- Node.js (v18 or higher)
- npm or yarn

### Setup

1. Install dependencies:

```bash
npm install
```

2. Create a `.env` file in the `nodejs/` directory:

```env
OPENROUTER_API_KEY=your_api_key_here
```

### Running the Project

```bash
npm start
```

Or directly with ts-node:

```bash
npx ts-node nodejs/index.ts
```

The project will make a chat completion request to OpenRouter using the `nvidia/nemotron-3.5-lightning:free` model.

### Environment Variables

- `OPENROUTER_API_KEY` - Your OpenRouter API key (required)


## .NET Harness (C# / Microsoft Agent Framework)

A console coding agent (Linux only) built on Microsoft Agent Framework with OpenRouter (OpenAI-compatible API).

- Tools: `read_file`, `write_file`, `list_dir`, `run_command` (bash). File tools are confined to the root directory (the directory the app was started in). `write_file` / `run_command` ask for approval (`yes` / `no`) unless `--yes` is passed. Note: shell commands are only limited by their working directory, so the approval prompt is the real guard.
- Prompt: built-in static prompt + `Agents.md` from the root directory (if present).
- Sessions are logged as JSONL in `<root>/.axharness/sessions/`; resume with `--resume <id|latest>`.

### Setup

```bash
dotnet user-secrets set "OpenRouter:ApiKey" "<your key>" --project src/AxHarness
```
(`OPENROUTER_API_KEY` env var works as a fallback.)

### Run

```bash
cd <your project>
dotnet run --project <path-to-AxHarness>/src/AxHarness -- [--model <id>] [--yes] [--resume latest]
```

Try it on the (gitignored) playground: `cd examples/example_dotnet_1 && dotnet run --project ../../src/AxHarness`.
REPL commands: `/help`, `/sessions`, `/clear`, `/exit`. Tests: `dotnet test`.
