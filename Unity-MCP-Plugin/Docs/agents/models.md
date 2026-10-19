# Object Reference Hierarchy

```text
ObjectRef (base — contains InstanceID)
├── AssetObjectRef (+ AssetPath, AssetGuid, AssetType)
│   └── GameObjectRef (+ Path, Name)
├── ComponentRef (+ Index, TypeName)
└── SceneRef (+ Path, BuildIndex)
```

`AssetObjectRef` identifies an asset by path. Object references use `[JsonPropertyName]` for their serialized fields. Scene and object queries are exposed through RQL; the fork's old prefab snapshot DTOs have been removed.
