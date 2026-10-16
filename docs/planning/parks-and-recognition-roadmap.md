# Product and Research Roadmap

See [Backend](../backend/README.md) for current implementation.
This document contains future work and proposals; [development rules](../development.md) remain mandatory.

## Scope

### First Version

Main flow: park → photograph → species candidates → card/Wiki.

Additional flows:

- Park management and CSV imports.
- Connection requests and manual payment confirmation.
- Accounts and discussions.

Product rules:

- Parks work without a catalog or payment.
- Park operations are open in v1.
- Species cards/chats are shared by exact source/item/language identity.
- Inference runs locally with ready pretrained weights.
- Wiki remains external; local inference does not make the application offline.

### Outside the First Version

- Individual-animal tracking, health, enclosures, and tickets.
- Organizations and employees.
- Subscriptions, payment gateways, and owner-based authorization.
- Training, fine-tuning, LoRA, or independent weight modification/quantization.

Recognition history and visitor-photo persistence are not implemented.

## Integration Work

1. Compose the existing Wikipedia adapter; decide whether taxonomic filtering and Wikidata enrichment are needed.
2. Implement API composition, bearer validation, endpoints/errors, upload validation, and event transport.
3. Agree the .NET/Python wire protocol and connect one local model.
4. Build recognition UI, then park editing, CSV preview/apply, and connection tracking.
5. After schema agreement, create Initial, configure deployment, and verify end-to-end flows.
6. Add automated tests at the final stage, then further model adapters and research runs.

Implementation notes:

- Wiki integration must follow [API Info](https://www.mediawiki.org/wiki/API:Info)
  and [API Etiquette](https://www.mediawiki.org/wiki/API:Etiquette).
- Existing Auth security implementations need wiring, not replacement.
- CSV flow: decode → preview → correction/selection → apply. XLSX is a later extension.
- [Application](../backend/application.md) defines import behavior and partial-failure boundaries.
- Academic UML should reflect actual scenarios, relationships, request states, recognition sequence, and deployment.

## Python Service Proposal

### Structure and Responsibility

- Keep an entry point, configuration, startup model loading, and simple library adapters.
- Select profiles via configuration/restart.
- Avoid a dynamic installation framework or experiment web UI.
- Receive an image, language, and optional candidates.
- Do not access SQL Server or request Wiki.

### Recognition Contract

[Application](../backend/application.md#animals) defines recognition behavior.

| Result field | Purpose |
| --- | --- |
| Recognized / Uncertain / NoAnimal | Recognition outcome |
| Names and alternatives | Species suggestions |
| Optional known AnimalId | Match from the supplied context |
| ModelId / Revision / InferenceMilliseconds | Optional execution metadata |

Confidence scores and context hashes are not implemented.

- An empty catalog uses general recognition.
- Classifier vocabularies must not depend on visitor-created lists.
- An unmatched known species means uncertainty, not proof that no animal is present.

### Proposed Endpoints

These endpoints are not implemented.

| Endpoint | Purpose |
| --- | --- |
| POST /v1/recognize | Multipart image/metadata |
| GET /health/live | Process is running |
| GET /health/ready | Model readiness |
| GET /v1/model | Loaded-profile metadata |

Before integration, agree:

- Field names and status values.
- URL configuration, timeouts, and response limits.
- Invalid decoding and overload handling.
- Target hardware, CPU/GPU use, and acceptable model size.

Prepare dependencies/weights locally.
Do not silently switch to external inference APIs.

### Model Candidates

These are evaluation candidates, not selected models or measured ZooFinder results.

- [BioCLIP](https://huggingface.co/imageomics/bioclip).
- [BioCLIP 2](https://huggingface.co/imageomics/bioclip-2).
- [Qwen3-VL-2B-Instruct](https://huggingface.co/Qwen/Qwen3-VL-2B-Instruct).
- [SmolVLM2-2.2B-Instruct](https://huggingface.co/HuggingFaceTB/SmolVLM2-2.2B-Instruct).

Check model cards/licenses, processors, memory requirements, and local execution.
Pin versions after trials and choose the default profile using measurements.
No models have been run here yet.

## Research Plan

### Dataset

Compare general recognition and park context on a fixed dataset.

- Do not tailor candidate lists using the correct answer for each image.
- Include outside-catalog species, non-animal images, poor quality, and ambiguous cases.
- Keep annotations separate from visitor photos.
- Split related image series/individuals and near duplicates together to avoid leakage.
- Document known pretraining overlap.

### Measurements

| Dimension | Measurements |
| --- | --- |
| Quality | Top-1, Top-3 where ranked output exists, macro-F1, confusion matrix |
| Rejection | Coverage, accepted-answer quality, erroneous acceptance of outside-catalog species |
| Errors | Decode/inference/timeout errors, invalid output, unmatched names |
| Latency | Loading separately; warmed preprocessing/inference/postprocessing, median/p95 |
| Resources | Peak RAM/VRAM and throughput at defined load |
| Context | Quality/latency against catalog size and distractor species |

- Include errors/rejections in reports.
- Separate Wiki, HTTP/.NET, and model latency.
- Account for GPU synchronization.

### Reproducibility and Runner

Record:

- Model revision and libraries/processor.
- Hardware, device, and dtype.
- Prompts, parser, decoding, and preprocessing.
- Context and dataset manifest.

A future CLI runner should reuse adapters:

```text
Configuration + manifest → per-image JSONL → aggregate CSV → charts/errors
```

Use one profile per run.
No experiment UI or new main-database tables are required.
Runner/results are not implemented.

## Acceptance

- Shared cards/chats with distinct park descriptions.
- Duplicate-free registration and repeated import.
- Immediate park use without payment/catalog.
- Suspension/publication filtering.
- Valid request transitions and idempotent payment.
- Atomic link-batch failure.
- Understandable image/provider errors.
- No automatic outside-species insertion.
- Reproducible experiments preserving configuration and errors.
