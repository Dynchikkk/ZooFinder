# Recognition HTTP Protocol

The .NET adapter implements this contract; the Python service/model is not implemented yet.
Python selects its model through server configuration and has no database/Wikipedia access.

## Request

`POST v1/recognize` relative to BaseUrl. Content-Type: `multipart/form-data`; Accept: `application/json`.

| Part | Contents |
| --- | --- |
| image | Image bytes, an image Content-Type, and filename `image`; accepted types come from provider settings |
| metadata | UTF-8 application/json with languageCode (`en`/`ru`) and candidates (required array, possibly empty) |

```json
{
  "languageCode": "en",
  "candidates": [
    { "animalId": "11111111-1111-4111-8111-111111111111", "commonName": "Lion", "scientificName": "Panthera leo" }
  ]
}
```

Candidates provide context without forcing an in-catalog answer. Animal IDs are UUID strings; scientificName may be null.
The image limit follows Application's recognition constraints. Python must validate decoding; no filename extension or
Content-Length header is guaranteed. The caller owns the stream; the adapter consumes its current position without closing it.

## Response

HTTP 200, Content-Type: `application/json`. Property names use camelCase.

```json
{
  "status": "recognized",
  "commonName": "Lion",
  "scientificName": "Panthera leo",
  "animalId": "11111111-1111-4111-8111-111111111111",
  "alternatives": [],
  "execution": { "modelId": "local-model", "revision": null, "inferenceMilliseconds": 125.5 }
}
```

status is required: exactly `recognized`, `uncertain`, or `noAnimal`. alternatives is a required array;
entries contain commonName, scientificName, and optional animalId. Names/animalId and the whole execution object may be null.
When execution is present, modelId and inferenceMilliseconds are required; revision is optional.
Outcome, ID, and metadata consistency follow [Application recognition rules](application.md#recognition).

## Errors and Composition

Error body: `{ "code": "invalid_image" }`, Content-Type: `application/json`.

| HTTP | Code | Meaning |
| --- | --- | --- |
| 400 | invalid_metadata | .NET/Python protocol mismatch |
| 413 | image_too_large | Image size rejected |
| 415 | unsupported_image_type | Image type rejected |
| 422 | invalid_image | Image decoding failed |
| 429 | busy | Service overloaded |
| 503 | model_not_ready | Model unavailable |
| 500 | inference_failed | Inference failed |

413/415/422 require matching codes and become request-validation errors. HTTP 408/429, any 5xx, network failure,
and timeout raise AnimalRecognitionUnavailableException. Other statuses, invalid JSON/results, and oversized responses
are protocol errors. Caller cancellation propagates. Errors never become uncertain/noAnimal results.

API owns BaseUrl, timeout, response limit, accepted image types, and HttpClient configuration. BaseUrl is absolute HTTP(S),
ends with `/`, and has no credentials/query/fragment. Disable redirects/retries for this streaming POST;
HttpClient.Timeout must not be shorter than the configured request timeout. No caching or image persistence is performed.
