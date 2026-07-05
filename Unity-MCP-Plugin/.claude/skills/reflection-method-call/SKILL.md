---
name: reflection-method-call
description: Call a C# method by reflection — including private methods. Requires a method schema obtained via 'reflection-method-find'. Supports static methods, instance methods (with optional target deserialization), and main-thread / off-thread execution.
---

# Method C# / Call

Call C# method. Any method could be called, even private methods. It requires to receive proper method schema. Use 'reflection-method-find' to find available method before using it. Receives input parameters and returns result.

## Match levels (apply to `typeName`, `MethodName`, `Parameters`)

- `typeNameMatchLevel` / `methodNameMatchLevel` (default 1 — contains ignoring case): `0` ignore filter, `1` contains-ic, `2` contains-cs, `3` starts-with-ic, `4` starts-with-cs, `5` equals-ic, `6` equals-cs.
- `parametersMatchLevel` (default 2 — equals): `0` ignore filter, `1` count matches, `2` equals.

## How to Call

```bash
unity-mcp-cli run-tool reflection-method-call --input '{
  "filter": "string_value",
  "knownNamespace": false,
  "typeNameMatchLevel": 0,
  "methodNameMatchLevel": 0,
  "parametersMatchLevel": 0,
  "targetObject": "string_value",
  "inputParameters": "string_value",
  "executeInMainThread": false
}'
```

> For complex input, save JSON to a file and use `unity-mcp-cli run-tool reflection-method-call --input-file args.json`.

### Troubleshooting

For CLI installation or connectivity issues, see the /unity-initial-setup skill.

## Input

| Name | Type | Required | Description |
|------|------|----------|-------------|
| `filter` | `any` | Yes | Method reference. Used to find method in codebase of the project. |
| `knownNamespace` | `boolean` | No | Set to true if 'Namespace' is known and full namespace name is specified in the 'filter.Namespace' property. Otherwise, set to false. |
| `typeNameMatchLevel` | `integer` | No | Minimal match level for 'typeName'. 0 - ignore 'filter.typeName', 1 - contains ignoring case (default value), 2 - contains case sensitive, 3 - starts with ignoring case, 4 - starts with case sensitive, 5 - equals ignoring case, 6 - equals case sensitive. |
| `methodNameMatchLevel` | `integer` | No | Minimal match level for 'MethodName'. 0 - ignore 'filter.MethodName', 1 - contains ignoring case (default value), 2 - contains case sensitive, 3 - starts with ignoring case, 4 - starts with case sensitive, 5 - equals ignoring case, 6 - equals case sensitive. |
| `parametersMatchLevel` | `integer` | No | Minimal match level for 'Parameters'. 0 - ignore 'filter.Parameters', 1 - parameters count is the same, 2 - equals (default value). |
| `targetObject` | `any` | No | Specify target object to call method on. Should be null if the method is static or if there is no specific target instance. New instance of the specified class will be created if the method is instance method and the targetObject is null. Required: type - full type name of the object to call method on, value - serialized object value (it will be deserialized to the specified type). |
| `inputParameters` | `any` | No | Method input parameters. Per each parameter specify: type - full type name of the object to call method on, name - parameter name, value - serialized object value (it will be deserialized to the specified type). |
| `executeInMainThread` | `boolean` | No | Set to true if the method should be executed in the main thread. Otherwise, set to false. |

## Output

### Output JSON Schema

```json
{
  "type": "object",
  "properties": {
    "result": {
      "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
    }
  },
  "$defs": {
    "com.IvanMurzak.ReflectorNet.Model.SerializedMemberList": {
      "type": "array",
      "items": {
        "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember"
      }
    },
    "com.IvanMurzak.ReflectorNet.Model.SerializedMember": {
      "type": "object",
      "properties": {
        "typeName": {
          "type": "string",
          "description": "Full type name. Eg: 'System.String', 'System.Int32', 'UnityEngine.Vector3', etc."
        },
        "name": {
          "type": "string",
          "description": "Object name."
        },
        "value": {
          "description": "Value of the object, serialized as a non stringified JSON element. Can be null if the value is not set. Can be default value if the value is an empty object or array json."
        },
        "fields": {
          "type": "array",
          "items": {
            "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
            "description": "Nested field value."
          },
          "description": "Fields of the object, serialized as a list of 'SerializedMember'."
        },
        "props": {
          "type": "array",
          "items": {
            "$ref": "#/$defs/com.IvanMurzak.ReflectorNet.Model.SerializedMember",
            "description": "Nested property value."
          },
          "description": "Properties of the object, serialized as a list of 'SerializedMember'."
        }
      },
      "required": [
        "typeName"
      ],
      "additionalProperties": false
    }
  },
  "required": [
    "result"
  ]
}
```

