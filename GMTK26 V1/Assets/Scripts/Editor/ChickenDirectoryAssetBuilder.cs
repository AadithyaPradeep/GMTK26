#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Creates editable Chicken Directory ScriptableObject assets under Assets/ChickenDirectory.
/// Menu: Exploding Chickens / Generate Directory Assets
/// </summary>
public static class ChickenDirectoryAssetBuilder
{
    private const string Folder = "Assets/ChickenDirectory";

    [MenuItem("Exploding Chickens/Generate Directory Assets")]
    public static void Generate()
    {
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets", "ChickenDirectory");

        ChickenDirectoryCatalog runtime = ChickenDirectoryCatalog.CreateRuntimeDefaults();
        var list = new System.Collections.Generic.List<ChickenDirectoryEntry>();

        foreach (ChickenDirectoryEntry src in runtime.Entries)
        {
            if (src == null)
                continue;

            string path = Folder + "/" + Sanitize(src.displayName) + ".asset";
            ChickenDirectoryEntry asset = AssetDatabase.LoadAssetAtPath<ChickenDirectoryEntry>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ChickenDirectoryEntry>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.displayName = src.displayName;
            asset.portrait = src.portrait;
            asset.shortDescription = src.shortDescription;
            asset.story = src.story;
            asset.role = src.role;
            asset.worldHint = src.worldHint;
            EditorUtility.SetDirty(asset);
            list.Add(asset);
        }

        string catalogPath = Folder + "/Catalog.asset";
        ChickenDirectoryCatalog catalog = AssetDatabase.LoadAssetAtPath<ChickenDirectoryCatalog>(catalogPath);
        if (catalog == null)
        {
            catalog = ScriptableObject.CreateInstance<ChickenDirectoryCatalog>();
            AssetDatabase.CreateAsset(catalog, catalogPath);
        }

        SerializedObject so = new SerializedObject(catalog);
        SerializedProperty entriesProp = so.FindProperty("entries");
        entriesProp.ClearArray();
        for (int i = 0; i < list.Count; i++)
        {
            entriesProp.InsertArrayElementAtIndex(i);
            entriesProp.GetArrayElementAtIndex(i).objectReferenceValue = list[i];
        }

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(catalog);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        // Also copy into Resources so LoadOrCreateDefaults picks it up if assigned there.
        string resourcesDir = "Assets/Resources/ChickenDirectory";
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(resourcesDir))
            AssetDatabase.CreateFolder("Assets/Resources", "ChickenDirectory");

        string resourcesCatalog = resourcesDir + "/Catalog.asset";
        AssetDatabase.DeleteAsset(resourcesCatalog);
        AssetDatabase.CopyAsset(catalogPath, resourcesCatalog);
        AssetDatabase.SaveAssets();

        Debug.Log("Chicken Directory assets generated at " + Folder + " (" + list.Count + " entries).");
        Selection.activeObject = catalog;
    }

    private static string Sanitize(string name)
    {
        foreach (char c in Path.GetInvalidFileNameChars())
            name = name.Replace(c, '_');
        return name.Replace(' ', '_');
    }
}
#endif
