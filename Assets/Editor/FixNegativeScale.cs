using UnityEngine;
using UnityEditor;
using Unity.Android;


public class FixNegativeScaleTool
{
    [MenuItem("Tools/Fix Negative Scales")]
    static void FixAllNegativeScales()
    {
        GameObject[] allObjects = Object.FindObjectsOfType<GameObject>();
        int fixedCount = 0;

        foreach (GameObject obj in allObjects)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(obj)) continue; // Skip prefabs if needed

            Vector3 scale = obj.transform.localScale;
            bool changed = false;

            if (scale.x < 0)
            {
                scale.x = Mathf.Abs(scale.x);
                changed = true;
            }
            if (scale.y < 0)
            {
                scale.y = Mathf.Abs(scale.y);
                changed = true;
            }
            if (scale.z < 0)
            {
                scale.z = Mathf.Abs(scale.z);
                changed = true;
            }

            if (changed)
            {
                Undo.RecordObject(obj.transform, "Fix Negative Scale");
                obj.transform.localScale = scale;
                Debug.LogWarning($"✔ Fixed scale: {GetGameObjectPath(obj)}");
                fixedCount++;
            }
        }

        Debug.Log($"✅ Total fixed GameObjects: {fixedCount}");
    }

    static string GetGameObjectPath(GameObject obj)
    {
        string path = obj.name;
        while (obj.transform.parent != null)
        {
            obj = obj.transform.parent.gameObject;
            path = obj.name + "/" + path;
        }
        return path;
    }
}
