# Features (CQRS)

One folder per domain feature, per skill `bmm-cqrs-mediatr`:

```
Features/<Domain>/
├── <Domain>Controller.cs     # thin: route → MediatR.Send → result
├── Commands/                 # Command + Handler + Validator (mutations)
├── Queries/                  # Query + Handler (reads)
└── <Domain>Contracts.cs      # DTOs / response models
```

Rules: controllers contain no logic; commands mutate, queries read — never both;
`MessId` is resolved before the handler (X-Mess-Id + membership check), never
trusted from a client-supplied DTO field.

First features land in Phase 1 (Messes).
