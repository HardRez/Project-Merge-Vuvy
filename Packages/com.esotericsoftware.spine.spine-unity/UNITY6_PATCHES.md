# Changes to spine-unity 3.8 for Unity 6.6

Upstream: https://github.com/EsotericSoftware/spine-runtimes, branch `3.8`, commit `8b4844bd4b193ba9e54487ed397a777993cbad56`
(`spine-unity/Assets/Spine` as this package, with `spine-csharp/src` copied into `Runtime/spine-csharp`).

Unity 6.6 turns several instance-ID APIs into compile errors. Every other file is unchanged.

| File | Change |
|---|---|
| `Runtime/spine-unity/Components/SkeletonMecanim.cs` | `AnimationClipEqualityComparer` compares clips with `ReferenceEquals` and hashes with `GetHashCode()` instead of `GetInstanceID()`. |
| `Editor/spine-unity/Editor/Asset Types/SpineAtlasAssetInspector.cs` | `AssetDatabase.GetAssetPath(texture)` instead of passing the texture's instance ID. |
| `Editor/spine-unity/Editor/Utility/AssetUtility.cs` | Restores lost atlas references with `GetEntityId()` / `EditorUtility.EntityIdToObject`; looks SkeletonGraphics up by `EntityId`. |
| `Editor/spine-unity/Editor/Utility/DataReloadHandler.cs` | Remembers SkeletonGraphic data assets by `EntityId`. |
| `Editor/spine-unity/Editor/Utility/SpineEditorUtilities.cs` | Hierarchy icons and drag-and-drop use `EditorApplication.hierarchyWindowItemByEntityIdOnGUI`, `EntityId`-keyed tables and `EditorUtility.EntityIdToObject`. |
| `Editor/spine-unity/Editor/Windows/SpinePreferences.cs` | The settings asset lives at `Assets/MergeGame/Editor/SpineSettings.asset`; the editor never creates an `Assets/Editor` folder for it. |
