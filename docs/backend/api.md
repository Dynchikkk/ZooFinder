# API Layer

`ZooFinder.Api` is the HTTP transport and application composition layer.

> **Status:** In progress. This document may change.

## Responsibilities

- expose Application use cases through controllers;
- convert HTTP input to Application requests;
- convert Application responses to HTTP responses;
- resolve the authenticated user identity;
- apply authentication and authorization policies;
- map exceptions to problem responses;
- configure OpenAPI and dependency injection.

Controllers contain transport logic only. Application services own use-case flow.

## Endpoint Groups

| Group | Operations |
| --- | --- |
| Animals | Search catalog, retrieve animal page |
| Recognition | Upload image and return recognition suggestion |
| Users | Retrieve public/current profile, update profile |
| Auth | Register, refresh, revoke one session, revoke all sessions |
| Discussions | Retrieve room and history, send, edit, delete message |

## Requests

- Cancellation uses `HttpContext.RequestAborted`.
- Pagination parameters map to Application pagination contracts.
- Cursor values remain opaque.
- Uploaded files are converted from `IFormFile` to the neutral recognition request.
- Image validation checks size and file signature.
- Authenticated account IDs come from validated claims, not request bodies.

## Responses

- API responses use Application response contracts or thin transport wrappers.
- Dates use UTC.
- Cursor responses return `Items` and `NextCursor`.
- Deleted discussion content is excluded for regular users.
- Recognition results are labeled as suggestions.

## Exception Mapping

| Application outcome | HTTP response |
| --- | --- |
| Invalid request | `400 Bad Request` |
| Missing or invalid authentication | `401 Unauthorized` |
| Insufficient permission | `403 Forbidden` |
| Resource not found | `404 Not Found` |
| State conflict | `409 Conflict` |
| Unexpected failure | `500 Internal Server Error` |

Errors use Problem Details. Internal exception data and external-provider payloads are not returned to clients.

## Authentication

- Access tokens are accepted through the bearer scheme.
- Refresh tokens are submitted only to authentication endpoints.
- Authorization policies use account role and status.
- Refresh-session revocation does not require persisting access tokens.

## Composition

API registers Application services and Infrastructure implementations at startup. Configuration is bound and validated before the application begins accepting requests.
