# Synchronization rules

ByteSync supports creating synchronization rules to automatically trigger actions when comparison conditions are met. Rules can compare file or directory attributes using different elements and operators.

## Comparison elements

- **Content**
- **Date**
- **Size**
- **Presence**
- **Name** (supports `Equals` and `NotEquals` operators, wildcard `*` is allowed)

Use `Name` to match items based on their file name. When a pattern contains `*`, the rule interprets it as a wildcard.
