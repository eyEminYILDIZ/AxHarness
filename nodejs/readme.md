# AxHarness – NodeJS Example

A basic coding agent harness for interacting with the OpenRouter API using NVIDIA's Nemotron model.

## Prerequisites

- Node.js (v18 or higher)
- npm or yarn

## Setup

1. Install dependencies:

```bash
npm install
```

2. Create a `.env` file in the `nodejs/` directory:

```env
OPENROUTER_API_KEY=your_api_key_here
```

## Running the Project

```bash
npm start
```

Or directly with ts-node:

```bash
npx ts-node nodejs/index.ts
```

The project will make a chat completion request to OpenRouter using the `nvidia/nemotron-3.5-super-vl:free` model.

## Environment Variables

- `OPENROUTER_API_KEY` - Your OpenRouter API key (required)
