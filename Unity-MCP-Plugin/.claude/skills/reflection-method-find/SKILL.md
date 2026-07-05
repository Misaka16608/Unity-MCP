---
name: reflection-method-find
description: Find C# methods across every loaded assembly by name / type / parameters — including private methods. Returns serialized `MethodData` entries usable as schemas for 'reflection-method-call'.
---

# Method C# / Find

Find method in the project using C# Reflection. It looks for all assemblies in the project and finds method by its name, class name and parameters. Even private methods are available. Use 'reflection-method-call' to call the method after finding it.

## Match levels (apply to `typeName`, `MethodName`, `Parameters`)

- `typeNameMatchLevel` / `methodNameMatchLevel` (default 1 — contains ignoring case): `0` ignore filter, `1` contains-ic, `2` contains-cs, `3` starts-with-ic, `4` starts-with-cs, `5` equals-ic, `6` equals-cs.
- `parametersMatchLevel` (default 0 — ignore filter): `0` ignore, `1` count matches, `2` equals.

## Output

On match, returns a `[Success] Found N method(s):` line followed by a JSON dump of every method's `MethodData`. Pass any single entry to 'reflection-method-call' as its `filter`.

## How to Call

```bash
unity-mcp-cli run-tool reflection-method-find --input '{
  "filter": "string_value",
  "knownNamespace": false,
  "typeNameMatchLevel": 0,
  "methodNameMatchLevel": 0,
  "parametersMatchLevel": 0
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool reflection-method-find --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `filter` | `any` | Yes | Method reference. Used to find method in codebase of the project. |
| `knownNamespace` | `boolean` | No | Set to true if 'Namespace' is known and full namespace name is specified in the 'filter.Namespace' property. Otherwise, set to false. |
| `typeNameMatchLevel` | `integer` | No | Minimal match level for 'typeName'. 0 - ignore 'filter.typeName', 1 - contains ignoring case (default value), 2 - contains case sensitive, 3 - starts with ignoring case, 4 - starts with case sensitive, 5 - equals ignoring case, 6 - equals case sensitive. |
| `methodNameMatchLevel` | `integer` | No | Minimal match level for 'MethodName'. 0 - ignore 'filter.MethodName', 1 - contains ignoring case (default value), 2 - contains case sensitive, 3 - starts with ignoring case, 4 - starts with case sensitive, 5 - equals ignoring case, 6 - equals case sensitive. |
| `parametersMatchLevel` | `integer` | No | Minimal match level for 'Parameters'. 0 - ignore 'filter.Parameters' (default value), 1 - parameters count is the same, 2 - equals. |

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

