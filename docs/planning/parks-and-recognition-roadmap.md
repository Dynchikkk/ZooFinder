# ZooFinder: Product, Local Recognition, and Model Research

This document describes the product and remaining work.
Implemented details are documented in [Backend](../backend/README.md) and the
[completed Domain/Application stage](domain-application-changes.md).
Mandatory development constraints are in [Development process](../development.md) and [Architecture](../architecture.md).

## Product and First-Version Scope

A visitor selects a nature or safari park, photographs an animal, and receives species-identification candidates.
The park card supplements shared information with a local description; Wiki remains the information/link source.
A park works without a species list, while a populated published catalog can improve recognition context.

The connection workflow records a request, agreed price, and manual payment confirmation.
In v1, all park operations are open and payment does not gate use.
Activated is the state of a completed request; Active/Suspended is the separate state of the park itself.
Payment gateways, subscriptions, and owner-based authorization are deferred.

Recognition targets species, not individuals. Health, enclosures, tickets, organizations, and employees are outside v1.
The Animal card and General chat are shared; two parks create two ParkAnimal associations to one card.
Matching scientific name/source identity or a non-conflicting normalized title permits reuse within a language.
A complete language-independent Species/Taxon identity does not yet exist.

Neural models are ready pretrained models with published weights, running entirely locally.
Training, fine-tuning, LoRA, and independently modifying/quantizing weights are excluded.
Wiki requests remain external: local execution applies to inference, not complete application independence from the network.

## Actual State

| Component | Implemented | Remaining |
| --- | --- | --- |
| Domain | Nine entities, including park/membership/request | Agree on the final schema |
| Application | Eleven services, contracts, validation, registration/context/import | End-to-end integration |
| Persistence | Nine DataSources, context, mappings, soft deletion, and transactions | Runtime DI, future Initial, SQL Server verification |
| Wiki | Interface and provider-neutral contracts | Concrete provider |
| Recognition | Service, park context, statuses/alternatives/metadata | .NET HTTP adapter and Python |
| Security/events | Interfaces and Application calls | Hashing, tokens, event transport |
| API | Program/settings scaffold | Endpoints, auth, DI, errors, uploads, and CSV |
| Frontend | .gitkeep | Angular application |
| Deployment | Placeholder infra/docker and scripts directories | Dockerfiles/Compose and instructions |
| Verification | Builds; no test projects | Automated checks at the final stage |
| Research | Plan | Dataset, model execution, measurements, and analysis |

Ready models have not yet been run in this repository.
The InferenceMilliseconds field does not establish the existence of a benchmark runner or experimental results.

## Wiki and Cards

The model returns names/candidates, not trusted links.
Links come through IAnimalInformationProvider and verified source identity.
In general mode, the client performs a separate search; with park context, it can open ParkAnimalId directly.

Park-species lists use cached Animal data. Individual park-card retrieval requests live information
and falls back to saved data without SourceUrl when the provider is unavailable or returns null.
The shared AnimalCatalogService.GetAnimalAsync does not yet have this fallback.

The future provider must define HTTP timeouts, client identification, caching, and display of text/image sources.
API and material-use requirements will be clarified during implementation using
[MediaWiki API Info](https://www.mediawiki.org/wiki/API:Info) and
[MediaWiki API Etiquette](https://www.mediawiki.org/wiki/API:Etiquette).

## Python and .NET

```text
Browser → ZooFinder.Api → Application
                          ├─ DataSource → SQL Server
                          ├─ Wiki provider → MediaWiki
                          └─ Recognition provider → local Python → ready pretrained model
```

Python receives an image, language, and optional candidates, runs inference, and returns a DTO.
It does not connect to the main database or retrieve Wiki information.
.NET owns parks, states, source identity, and validation of returned contextual IDs.

The service should remain small: an entry point, configuration, selected-model loading,
and a few simple wrappers around library calls. Load the model at startup; change profiles through
configuration/restart. Dynamic model installation, a complex framework, and an experiment web UI are unnecessary.

A label-based classifier must support a general vocabulary when the park catalog is empty.
Visitors do not have to compile that vocabulary. A generative model can use its own prompt/parser.
Failure to choose a known species indicates uncertainty; it does not itself prove that no animal is present.

### Existing Application Contract

- Request: ImageStream, ContentType, LanguageCode, Candidates.
- Context candidate: AnimalId, CommonName, ScientificName.
- Result: Status, CommonName, ScientificName, Alternatives, optional AnimalId and Execution.
- Execution: ModelId, optional Revision, InferenceMilliseconds.
- Service response: validated names/alternatives and optional ParkAnimalId.

Statuses: Recognized, Uncertain, NoAnimal.
Application validates ID membership in context, duplicates, and NoAnimal consistency.
Technical errors are not replaced by Uncertain.

Confidence scores, context hash, stage timings, and request ID are currently absent.
They may be added for integration/research after contract agreement; they are not implemented capabilities.

### Proposed HTTP Protocol — Not Implemented

- POST /v1/recognize: multipart with image and metadata.
- GET /health/live: the process is running.
- GET /health/ready: the model is ready.
- GET /v1/model: loaded-profile information.

An empty list means general mode. Agree on field names and wire status values between .NET/Python;
the Application enum is not an implemented HTTP schema.
Define behavior for URL configuration, timeouts, response limits, decoding errors, and overload.

Prepare weights and dependencies in advance. Inference uses local files without automatically
switching to external APIs. CPU/GPU support and acceptable model size depend on the available hardware.

### Initial Research Candidates

These are retained candidates for later evaluation, not a selected configuration or ZooFinder results:

- [BioCLIP](https://huggingface.co/imageomics/bioclip).
- [BioCLIP 2](https://huggingface.co/imageomics/bioclip-2).
- [Qwen3-VL-2B-Instruct](https://huggingface.co/Qwen/Qwen3-VL-2B-Instruct).
- [SmolVLM2-2.2B-Instruct](https://huggingface.co/HuggingFaceTB/SmolVLM2-2.2B-Instruct).

Before integration, check the model card, license, library/processor, local execution, and memory requirements.
Pin versions after a trial run on the target hardware.
Choose the default profile using measurements; specific models and library versions are not yet fixed.

## Import and UI

Manual species addition through Wiki is the primary simple flow.
Application preview/apply already operates on decoded rows, up to 200 per request.
CSV decoding and the confirmation form are absent; XLSX remains a later extension.

Flow: upload → validation/matching → preview → correction or selection → apply.
Preview performs no writes. Ambiguous matches are not automatically published.
Repeated apply reuses associations. ParkAnimal writes form one batch,
while preliminary shared-card/chat registration uses separate transactions.

UI must distinguish uncertainty, absence of an animal, and technical errors.
An empty park catalog must not appear as a failure.
Park/connection management is open to everyone in v1; existing login and message permissions remain.

## Remaining Work Sequence

1. Implement the Wiki provider, security/event dependencies needed by scenarios, and runtime DI.
2. Add HTTP endpoints, error handling, and upload validation.
3. Connect a simple Python service using one ready model and a .NET HTTP adapter.
4. Build minimal UI for park → photo → result → card/Wiki.
5. Connect park management, CSV preview/apply, and request/manual-payment tracking.
6. After schema agreement, create Initial; configure deployment and verify end-to-end scenarios.
7. Add automated tests at the final development stage.
8. Connect other models and research runs.

Auth is required for protected discussion scenarios but does not gate the new park flow.
UML for academic work should reflect actual use cases, classes/relationships, request states,
recognition sequence, and deployment. This document does not claim completed UML deliverables.

## Thesis Research

Compare models on a fixed dataset in general mode and with a park catalog.
Do not tailor the candidate list to each photograph using prior knowledge of the correct answer.
Include out-of-catalog species, images without animals, poor-quality images, and ambiguous cases.

Annotations are stored separately from visitor photographs. Account for image series/individuals and near duplicates
when splitting data to avoid leakage between pilot and final samples.
Document pretraining-data overlap to the extent that information is available.
No training or weight modification is performed.

| Group | Planned measurements |
| --- | --- |
| Quality | Top-1, Top-3 when ranking is available, macro-F1, confusion matrix |
| Rejection/unknown | Coverage, accepted-answer quality, erroneous acceptance of out-of-list species |
| Errors | Decode/inference errors, invalid output, unmatched names, timeout |
| Speed | Loading separately; warmed preprocessing/inference/postprocessing, median/p95 |
| Resources | Peak RAM/VRAM, throughput at a defined load |
| Context | Quality/speed changes with catalog use, catalog size, and distractor species |

Record all errors and rejections; do not report accuracy only for successful answers without coverage.
Separate Wiki latency from ML latency. Measure HTTP/.NET latency separately from model inference.
Account for synchronization in GPU timing and use consistent resource-measurement boundaries.

Record model revision, library/processor versions, device/dtype, hardware, prompt/parser/decoding,
context contents, and dataset manifest. Preprocessing may differ according to each model's requirements;
its parameters must be reproducible.

The minimal future runner is a separate CLI using the same adapters: config + manifest →
per-image JSONL → aggregate CSV → charts/errors.
Use one profile per run; no new main-database tables or experiment UI are required.
The runner and these results are not implemented.

## Effort Estimate

The initial estimate was 70–130 hours for the first version and another 16–32 hours for additional
model wrappers, the runner, and metrics export. This is a historical estimate of the entire scope before
the current changes, not an estimate of remaining work or recorded actual effort.

It excluded dataset collection/annotation, long runs, analysis, thesis writing,
and adaptation to unsupported hardware. Reassess remaining effort after selecting
the first model, hardware, and minimal screens.

## Final Acceptance Criteria

These are future manual/final automated verification criteria, not completed tests.

- Two parks share one species and chat while displaying different local descriptions.
- The first saved species receives an empty General room; repeated registration creates no duplicates.
- A park works immediately without a list/payment; suspended/unpublished data stays out of public context.
- Preview performs no writes; repeated import reuses links; a partial failure does not publish an association batch.
- Requests follow their transitions; repeated payment is idempotent and does not change the park.
- Invalid images and unavailable providers produce understandable technical errors.
- Out-of-catalog species are not added automatically; models follow one common contract.
- Repeating an experiment on a fixed manifest preserves configuration, results, and errors.
