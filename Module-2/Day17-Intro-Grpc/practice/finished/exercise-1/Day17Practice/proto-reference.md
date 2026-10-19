# Protocol Buffer Quick Reference

## Scalar Types

| Proto | C# | Default |
|-------|-----|---------|
| int32 | int | 0 |
| int64 | long | 0 |
| float | float | 0 |
| double | double | 0 |
| bool | bool | false |
| string | string | "" |
| bytes | ByteString | empty |

## Field Rules

- Every field needs a **unique number** within its message
- Field numbers are wire identifiers (1-15 use 1 byte, 16-2047 use 2 bytes)
- Never reuse or change field numbers after deployment
- `repeated` = list/collection (zero or more items)

## Naming Conventions

- Messages: PascalCase (`Product`, `GetProductRequest`)
- Fields: snake_case (`stock_quantity`) -- generates PascalCase in C#
- Enums: type name PascalCase (`ProductCategory`); values UPPER_SNAKE_CASE (`PRODUCT_CATEGORY_UNSPECIFIED`)
- Services: PascalCase (`ProductCatalog`)
- RPC methods: PascalCase (`GetProduct`)

## Enum Rules

- Must start at 0 (UNSPECIFIED/UNKNOWN)
- Values use UPPER_SNAKE_CASE
