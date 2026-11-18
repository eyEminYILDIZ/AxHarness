# Free OpenRouter models

Generated 2026-11-18 from `https://openrouter.ai/api/v1/models` (models whose id ends with `:free`, 14 total). Free models change often and are rate-limited, so re-check the live list before relying on one.

Current model in [index.ts](index.ts): `google/gemma-4-31b-it:free`

## Comparison

| Model                                                | Context | Max output | Input                        | Tools | Reasoning | Structured output | JSON mode | Reasoning mode |
| ---------------------------------------------------- | ------: | ---------: | ---------------------------- | :---: | :-------: | :---------------: | :-------: | -------------- |
| `inclusionai/ling-3.2-flash:free`                    |      1M |       262K | text · image                 |  ✅   |    ✅     |        ✅         |    ✅     | optional       |
| `thinkingmachines/inkling-2-small:free`              |      1M |       131K | text · image                 |  ✅   |    ✅     |        ❌         |    ❌     | 🔒 mandatory   |
| `particle-ai/particle-1:free`                        |      1M |       131K | text · image                 |  ✅   |    ✅     |        ✅         |    ✅     | optional       |
| `baidu/ernie-5.5-flash-preview:free`                 |      1M |       922K | text                         |  ✅   |    ✅     |        ✅         |    ✅     | 🔒 mandatory   |
| `nvidia/nemotron-3.5-super-vl:free`                  |      1M |       131K | text · image · video         |  ✅   |    ✅     |        ✅         |    ✅     | optional       |
| `nvidia/nemotron-3.5-lightning:free`                 |      1M |        66K | text                         |  ✅   |    ✅     |        ❌         |    ❌     | optional       |
| `nvidia/nemotron-3-ultra-550b-a55b:free`             |      1M |        66K | text                         |  ✅   |    ✅     |        ❌         |    ❌     | optional       |
| `dots-studio/dots-3-note-preview:free`               |    512K |       461K | text · image                 |  ✅   |    ✅     |        ✅         |    ✅     | optional       |
| `liquid/lfm-3-1.7b:free`                             |    262K |         8K | text                         |  ✅   |    ✅     |        ✅         |    ✅     | 🔒 mandatory   |
| `recursive/flint-v1.0:free`                          |    262K |       236K | text                         |  ✅   |    ✅     |        ❌         |    ✅     | 🔒 mandatory   |
| `google/gemma-4-26b-a4b-it:free`                     |    262K |        33K | image · text · video         |  ✅   |    ✅     |        ❌         |    ✅     | optional       |
| **`google/gemma-4-31b-it:free`** ⭐                  |    262K |        33K | image · text · video         |  ✅   |    ✅     |        ❌         |    ✅     | optional       |
| `cohere/north-mini-code:free`                        |    256K |        64K | text                         |  ✅   |    ✅     |        ❌         |    ❌     | optional       |
| `nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free` |    256K |        66K | text · audio · image · video |  ✅   |    ✅     |        ❌         |    ❌     | optional       |

**Columns:** *Tools* = `tools`/`tool_choice` in supported parameters. *Reasoning* = `reasoning` parameter supported. *Structured output* = `structured_outputs`. *JSON mode* = `response_format`. *Reasoning mode* = `reasoning.mandatory` (🔒 mandatory = cannot be disabled, otherwise optional). ⭐ = current model in `index.ts`.

## Details

### inclusionAI: Ling 3.2 Flash (free)

- **ID:** `inclusionai/ling-3.2-flash:free`
- **Modality:** text+image->text
- **Context:** 1,048,576 tokens; max completion 262,144
- **Tool calling:** yes (tool_choice supported)
- **Reasoning:** yes — default enabled, optional
- **Moderated:** no
- **Knowledge cutoff:** not listed
- **Expires:** not listed
- **Parameters:** frequency_penalty, include_reasoning, max_tokens, presence_penalty, reasoning, response_format, seed, stop, structured_outputs, temperature, tool_choice, tools, top_k, top_p
- **Description:** Ling 3.2 Flash is a hybrid reasoning model from inclusionAI that accepts text and image input, with a 1M-token context window and up to 200 images per request. Thinking can...

### Thinking Machines: Inkling 2 Small (free)

- **ID:** `thinkingmachines/inkling-2-small:free`
- **Modality:** text+image->text
- **Context:** 1,048,576 tokens; max completion 131,072
- **Tool calling:** yes
- **Reasoning:** yes — default enabled, mandatory, reasoning_effort supported
- **Moderated:** no
- **Knowledge cutoff:** not listed
- **Expires:** 2026-11-19
- **Parameters:** include_reasoning, max_tokens, reasoning, reasoning_effort, seed, stop, temperature, tools, top_p
- **Description:** Inkling 2 Small is an open-weight multimodal mixture-of-experts reasoning model from Thinking Machines Lab, with 17B active parameters out of 594B total. It is the first model in the second...

### Particle AI: Particle-1 (free)

- **ID:** `particle-ai/particle-1:free`
- **Modality:** text+image->text
- **Context:** 1,048,576 tokens; max completion 131,072
- **Tool calling:** yes (tool_choice supported)
- **Reasoning:** yes — default enabled, optional, reasoning_effort supported
- **Moderated:** no
- **Knowledge cutoff:** not listed
- **Expires:** not listed
- **Parameters:** frequency_penalty, include_reasoning, logprobs, max_tokens, min_p, presence_penalty, reasoning, reasoning_effort, repetition_penalty, response_format, seed, stop, structured_outputs, temperature, tool_choice, tools, top_k, top_logprobs, top_p
- **Description:** Particle-1 is a token-efficient agentic coding model from Particle AI, post-trained on DeepSeek V4.1 Flash. It is suited for software engineering work such as terminal workflows, codebase changes, and data...

### Baidu: ERNIE-5.5-Flash-Preview (free)

- **ID:** `baidu/ernie-5.5-flash-preview:free`
- **Modality:** text->text
- **Context:** 1,024,000 tokens; max completion 921,600
- **Tool calling:** yes (tool_choice supported)
- **Reasoning:** yes — default enabled, mandatory, reasoning_effort supported
- **Moderated:** no
- **Knowledge cutoff:** not listed
- **Expires:** not listed
- **Parameters:** include_reasoning, max_tokens, reasoning, reasoning_effort, response_format, structured_outputs, temperature, tool_choice, tools, top_p
- **Description:** ERNIE 5.5 Flash Preview is the latest foundation model in the ERNIE family, with comprehensive improvements in coding, agentic capabilities, knowledge work, and deep research. It has 280 billion total...

### NVIDIA: Nemotron 3.5 Super VL (free)

- **ID:** `nvidia/nemotron-3.5-super-vl:free`
- **Modality:** text+image+video->text
- **Context:** 1,000,000 tokens; max completion 131,072
- **Tool calling:** yes (tool_choice supported)
- **Reasoning:** yes — default disabled, optional
- **Moderated:** no
- **Knowledge cutoff:** not listed
- **Expires:** not listed
- **Parameters:** include_reasoning, max_tokens, reasoning, response_format, seed, structured_outputs, temperature, tool_choice, tools, top_p
- **Description:** NVIDIA Nemotron 3.5 Super VL is an open multimodal mixture-of-experts model from NVIDIA, with 12B active parameters out of 120B total. It takes text, images, documents, GUI screenshots, and video...

### NVIDIA: Nemotron 3.5 Lightning (free)

- **ID:** `nvidia/nemotron-3.5-lightning:free`
- **Modality:** text->text
- **Context:** 1,000,000 tokens; max completion 65,536
- **Tool calling:** yes (tool_choice supported)
- **Reasoning:** yes — default disabled, optional
- **Moderated:** no
- **Knowledge cutoff:** not listed
- **Expires:** not listed
- **Parameters:** include_reasoning, max_tokens, reasoning, seed, temperature, tool_choice, tools, top_p
- **Description:** NVIDIA Nemotron 3.5 Lightning is an open mixture-of-experts model from NVIDIA, with 3B active parameters out of 30B total. It is suited for high-throughput agentic workloads and specialized tasks that...

### NVIDIA: Nemotron 3 Ultra (free)

- **ID:** `nvidia/nemotron-3-ultra-550b-a55b:free`
- **Modality:** text->text
- **Context:** 1,000,000 tokens; max completion 65,536
- **Tool calling:** yes (tool_choice supported)
- **Reasoning:** yes — default enabled, optional, reasoning_effort supported
- **Moderated:** no
- **Knowledge cutoff:** not listed
- **Expires:** not listed
- **Parameters:** include_reasoning, max_tokens, reasoning, reasoning_effort, seed, temperature, tool_choice, tools, top_p
- **Description:** NVIDIA Nemotron 3 Ultra is an open frontier-reasoning and orchestration model from NVIDIA, with 55B active parameters out of 550B total (MoE). Built on a hybrid Transformer-Mamba mixture-of-experts architecture, it...

### Dots Studio: Dots3-Note Preview (free)

- **ID:** `dots-studio/dots-3-note-preview:free`
- **Modality:** text+image->text
- **Context:** 512,000 tokens; max completion 460,800
- **Tool calling:** yes (tool_choice supported)
- **Reasoning:** yes — default disabled, optional
- **Moderated:** no
- **Knowledge cutoff:** not listed
- **Expires:** 2026-12-31
- **Parameters:** include_reasoning, max_tokens, reasoning, response_format, structured_outputs, temperature, tool_choice, tools, top_p
- **Description:** Dots3-Note Preview is an open-weight mixture-of-experts model from Dots Studio, with 16B active parameters out of 280B total. It is the lightest model in the Dots 3 family and is...

### LiquidAI: LFM3-1.7B (free)

- **ID:** `liquid/lfm-3-1.7b:free`
- **Modality:** text->text
- **Context:** 262,144 tokens; max completion 8,192
- **Tool calling:** yes (tool_choice supported)
- **Reasoning:** yes — default disabled, mandatory
- **Moderated:** no
- **Knowledge cutoff:** not listed
- **Expires:** not listed
- **Parameters:** frequency_penalty, include_reasoning, logprobs, max_completion_tokens, max_tokens, min_p, presence_penalty, reasoning, repetition_penalty, response_format, seed, stop, structured_outputs, temperature, tool_choice, tools, top_k, top_logprobs, top_p
- **Description:** A compact 1.7B-parameter text model with a 256K-token context, post-trained for agents, tool calling, and structured outputs. It reasons before it answers. Prompts and outputs may be retained and used...

### Recursive: Flint (free)

- **ID:** `recursive/flint-v1.0:free`
- **Modality:** text->text
- **Context:** 262,144 tokens; max completion 235,929
- **Tool calling:** yes
- **Reasoning:** yes — default disabled, mandatory
- **Moderated:** no
- **Knowledge cutoff:** not listed
- **Expires:** not listed
- **Parameters:** include_reasoning, logprobs, max_tokens, reasoning, response_format, seed, stop, temperature, tools, top_logprobs, top_p
- **Description:** Flint is an open-weight mixture-of-experts model from Recursive, with about 2.5B active parameters out of 40B total, built by Recursive's autoresearch system Eureka Machine for coding agents. It is suited...

### Google: Gemma 4 26B A4B  (free)

- **ID:** `google/gemma-4-26b-a4b-it:free`
- **Modality:** text+image+video->text
- **Context:** 262,144 tokens; max completion 32,768
- **Tool calling:** yes (tool_choice supported)
- **Reasoning:** yes — default disabled, optional
- **Moderated:** no
- **Knowledge cutoff:** not listed
- **Expires:** not listed
- **Parameters:** include_reasoning, max_tokens, reasoning, response_format, seed, temperature, tool_choice, tools, top_p
- **Description:** Gemma 4 26B A4B IT is an instruction-tuned Mixture-of-Experts (MoE) model from Google DeepMind. Despite 25.2B total parameters, only 3.8B activate per token during inference — delivering near-31B quality at...

### Google: Gemma 4 31B (free)

- **ID:** `google/gemma-4-31b-it:free`
- **Modality:** text+image+video->text
- **Context:** 262,144 tokens; max completion 32,768
- **Tool calling:** yes (tool_choice supported)
- **Reasoning:** yes — default disabled, optional
- **Moderated:** no
- **Knowledge cutoff:** not listed
- **Expires:** not listed
- **Parameters:** include_reasoning, max_tokens, reasoning, response_format, seed, temperature, tool_choice, tools, top_p
- **Description:** Gemma 4 31B Instruct is Google DeepMind's 30.7B dense multimodal model supporting text and image input with text output. Features a 256K token context window, configurable thinking/reasoning mode, native function...

### Cohere: North Mini Code (free)

- **ID:** `cohere/north-mini-code:free`
- **Modality:** text->text
- **Context:** 256,000 tokens; max completion 64,000
- **Tool calling:** yes (tool_choice supported)
- **Reasoning:** yes — default disabled, optional
- **Moderated:** yes
- **Knowledge cutoff:** not listed
- **Expires:** not listed
- **Parameters:** frequency_penalty, include_reasoning, max_tokens, presence_penalty, reasoning, seed, stop, temperature, tool_choice, tools, top_k, top_p
- **Description:** North Mini Code is Cohere's first agentic coding model and the debut of its North family. A sparse mixture-of-experts model with 30B total parameters and 3B active, it is optimized...

### NVIDIA: Nemotron 3 Nano Omni (free)

- **ID:** `nvidia/nemotron-3-nano-omni-30b-a3b-reasoning:free`
- **Modality:** text+image+audio+video->text
- **Context:** 256,000 tokens; max completion 65,536
- **Tool calling:** yes (tool_choice supported)
- **Reasoning:** yes — default enabled, optional
- **Moderated:** no
- **Knowledge cutoff:** not listed
- **Expires:** not listed
- **Parameters:** include_reasoning, max_tokens, reasoning, seed, temperature, tool_choice, tools, top_p
- **Description:** NVIDIA Nemotron™ 3 Nano Omni is a 30B-A3B open multimodal model designed to function as a perception and context sub-agent in enterprise agent systems. It accepts text, image, video, and...
