---
name: type-get-json-schema
description: Generate a JSON Schema for a C# type name via reflection. Supports primitives, enums, arrays, generic collections, dictionaries, and complex objects. Knobs control inclusion of nested `$defs` and whether type-level / property-level descriptions are emitted.
---

# Type / Get Json Schema

Generates a JSON Schema for a given C# type name using reflection. Supports primitives, enums, arrays, generic collections, dictionaries, and complex objects. The type must be present in any loaded assembly. Use the full type name (e.g. 'UnityEngine.Vector3') for best results.

## How to Call

```bash
unity-mcp-cli run-tool type-get-json-schema --input '{
  "typeName": "string_value",
  "descriptionMode": "string_value",
  "propertyDescriptionMode": "string_value",
  "includeNestedTypes": false,
  "writeIndented": false
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool type-get-json-schema --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `typeName` | `string` | Yes | Full C# type name to generate the schema for. Examples: 'System.String', 'UnityEngine.Vector3', 'System.Collections.Generic.List<System.Int32>'. Simple names like 'Vector3' are also accepted when unambiguous. |
| `descriptionMode` | `string` | No | Controls the type-level 'description' field. Include: keep on the target type only. IncludeRecursively: keep on the target type and inside $defs entries. Ignore: strip all type-level descriptions. Default: Ignore. |
| `propertyDescriptionMode` | `string` | No | Controls 'description' fields on properties, fields, and array items. Include: keep on the target type's own properties/items only. IncludeRecursively: keep on all properties/items including those inside $defs entries. Ignore: strip all property/item descriptions. Default: Ignore. |
| `includeNestedTypes` | `boolean` | No | When true, complex nested types are extracted into '$defs' and referenced via '$ref' instead of being inlined. Useful for large or recursive types. Default: false. |
| `writeIndented` | `boolean` | No | Whether to format the output JSON with indentation for readability. Default: false. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "type": "string"
    }
  },
  "required": [
    "result"
  ]
}
```

