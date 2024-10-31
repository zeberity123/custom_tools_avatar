using UnityEditor;
using UnityEngine;
using VRM; // Ensure VRM SDK is imported

public class BlendShapeClipGenerator : EditorWindow
{
    private GameObject avatar;
    private VRMBlendShapeProxy blendShapeProxy;
    private SkinnedMeshRenderer[] skinnedMeshRenderers;

    // List of base BlendShapeClip names
    private readonly string[] blendShapeClipNames = new string[]
    {
        "Neutral",
        "A",
        "I",
        "U",
        "E",
        "O",
        "Blink",
        "Joy",
        "Angry",
        "Sorrow",
        "Fun",
        "LookUp",
        "LookDown",
        "LookLeft",
        "LookRight",
        "Blink_L",
        "Blink_R",
        "browInnerUp",
        "browDownLeft",
        "browDownRight",
        "browOuterUpLeft",
        "browOuterUpRight",
        "eyeLookDownLeft",
        "eyeLookDownRight",
        "eyeLookInLeft",
        "eyeLookInRight",
        "eyeLookOutLeft",
        "eyeLookOutRight",
        "eyeLookUpLeft",
        "eyeLookUpRight",
        "eyeBlinkLeft",
        "eyeBlinkRight",
        "eyeSquintLeft",
        "eyeSquintRight",
        "eyeWideLeft",
        "eyeWideRight",
        "cheekPuff",
        "cheekSquintLeft",
        "cheekSquintRight",
        "noseSneerLeft",
        "noseSneerRight",
        "jawOpen",
        "jawForward",
        "jawLeft",
        "jawRight",
        "mouthFunnel",
        "mouthPucker",
        "mouthLeft",
        "mouthRight",
        "mouthRollLower",
        "mouthRollUpper",
        "mouthShrugLower",
        "mouthShrugUpper",
        "mouthClose",
        "mouthSmileLeft",
        "mouthSmileRight",
        "mouthFrownLeft",
        "mouthFrownRight",
        "mouthDimpleLeft",
        "mouthDimpleRight",
        "mouthUpperUpLeft",
        "mouthUpperUpRight",
        "mouthLowerDownLeft",
        "mouthLowerDownRight",
        "mouthPressLeft",
        "mouthPressRight",
        "mouthStretchLeft",
        "mouthStretchRight",
        "tongueOut"
    };

    // Booleans for additional BlendShapeClips
    private bool includeEyeBaseJiggleL = false;
    private bool includeEyeBaseJiggleR = false;
    private bool includeEyeJiggleL = false;
    private bool includeEyeJiggleR = false;

    [MenuItem("Tools/Facial BlendShapeClip Generator")]
    public static void ShowWindow()
    {
        GetWindow<BlendShapeClipGenerator>("Facial BlendShapeClip Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Avatar Setup", EditorStyles.boldLabel);
        avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", avatar, typeof(GameObject), true);

        if (avatar != null)
        {
            if (GUILayout.Button("Scan Avatar"))
            {
                ScanAvatar();
            }

            if (blendShapeProxy != null)
            {
                EditorGUILayout.HelpBox("VRM BlendShapeProxy found.", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("VRM BlendShapeProxy not found.", MessageType.Warning);
            }

            if (skinnedMeshRenderers != null && skinnedMeshRenderers.Length > 0)
            {
                GUILayout.Label("Skinned Mesh Renderers:", EditorStyles.boldLabel);
                foreach (var renderer in skinnedMeshRenderers)
                {
                    EditorGUILayout.ObjectField(renderer, typeof(SkinnedMeshRenderer), true);
                }
            }

            if (blendShapeProxy != null)
            {
                GUILayout.Space(10);
                GUILayout.Label("Additional BlendShapeClips", EditorStyles.boldLabel);
                includeEyeBaseJiggleL = EditorGUILayout.Toggle("Include eyeBaseJiggleL", includeEyeBaseJiggleL);
                includeEyeBaseJiggleR = EditorGUILayout.Toggle("Include eyeBaseJiggleR", includeEyeBaseJiggleR);
                includeEyeJiggleL = EditorGUILayout.Toggle("Include eyeJiggleL", includeEyeJiggleL);
                includeEyeJiggleR = EditorGUILayout.Toggle("Include eyeJiggleR", includeEyeJiggleR);

                GUILayout.Space(10);

                if (GUILayout.Button("Set Facial BlendShapeClips"))
                {
                    CreateBlendShapeClips();
                }

                GUILayout.Space(10);
                if (GUILayout.Button("Delete All BlendShapeClips"))
                {
                    if (EditorUtility.DisplayDialog(
                        "Delete All BlendShapeClips",
                        "Are you sure you want to delete all BlendShapeClips?",
                        "Yes",
                        "No"))
                    {
                        DeleteAllBlendShapeClips();
                    }
                }


            }
        }
    }

    private void ScanAvatar()
    {
        blendShapeProxy = avatar.GetComponent<VRMBlendShapeProxy>();
        skinnedMeshRenderers = avatar.GetComponentsInChildren<SkinnedMeshRenderer>();
    }

    private void CreateBlendShapeClips()
    {
        // Ensure the BlendShapeAvatar exists
        var blendShapeAvatar = blendShapeProxy.BlendShapeAvatar;
        if (blendShapeAvatar == null)
        {
            // Create a new BlendShapeAvatar asset
            blendShapeAvatar = ScriptableObject.CreateInstance<BlendShapeAvatar>();
            string path = EditorUtility.SaveFilePanelInProject(
                "Save BlendShapeAvatar",
                "BlendShapeAvatar.asset",
                "asset",
                "Please enter a file name to save the asset to"
            );
            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("BlendShapeAvatar creation canceled.");
                return;
            }
            AssetDatabase.CreateAsset(blendShapeAvatar, path);
            blendShapeProxy.BlendShapeAvatar = blendShapeAvatar;
            EditorUtility.SetDirty(blendShapeProxy);
        }

        // Build the list of clip names to process
        var clipNamesToProcess = new System.Collections.Generic.List<string>(blendShapeClipNames);

        if (includeEyeBaseJiggleL)
        {
            clipNamesToProcess.Add("eyeBaseJiggleL");
        }
        if (includeEyeBaseJiggleR)
        {
            clipNamesToProcess.Add("eyeBaseJiggleR");
        }
        if (includeEyeJiggleL)
        {
            clipNamesToProcess.Add("eyeJiggleL");
        }
        if (includeEyeJiggleR)
        {
            clipNamesToProcess.Add("eyeJiggleR");
        }

        // Create or update BlendShapeClips
        foreach (string clipName in clipNamesToProcess)
        {
            var existingClip = blendShapeAvatar.Clips.Find(
                c => c != null && c.BlendShapeName == clipName
            );
            if (existingClip == null)
            {
                // Create a new BlendShapeClip
                var newClip = ScriptableObject.CreateInstance<BlendShapeClip>();
                newClip.BlendShapeName = clipName;

                // Set the values
                SetBlendShapeClipValues(newClip, clipName);

                // Save the BlendShapeClip as an asset
                string clipPath = AssetDatabase.GetAssetPath(blendShapeAvatar);
                clipPath = System.IO.Path.GetDirectoryName(clipPath);
                string assetPath = System.IO.Path.Combine(clipPath, $"{clipName}.asset");
                AssetDatabase.CreateAsset(newClip, assetPath);

                // Add the clip to the BlendShapeAvatar
                blendShapeAvatar.Clips.Add(newClip);
            }
            else
            {
                // Update existing clip
                SetBlendShapeClipValues(existingClip, clipName);
            }
        }

        // Build an ordered list for sorting
        var orderedClipNames = new System.Collections.Generic.List<string>(blendShapeClipNames);
        if (includeEyeBaseJiggleL)
        {
            orderedClipNames.Add("eyeBaseJiggleL");
        }
        if (includeEyeBaseJiggleR)
        {
            orderedClipNames.Add("eyeBaseJiggleR");
        }
        if (includeEyeJiggleL)
        {
            orderedClipNames.Add("eyeJiggleL");
        }
        if (includeEyeJiggleR)
        {
            orderedClipNames.Add("eyeJiggleR");
        }

        // Ensure the order of clips is constant
        blendShapeAvatar.Clips.Sort(
            (a, b) => orderedClipNames.IndexOf(a.BlendShapeName)
                .CompareTo(orderedClipNames.IndexOf(b.BlendShapeName))
        );

        EditorUtility.SetDirty(blendShapeAvatar);
        AssetDatabase.SaveAssets();

        Debug.Log("BlendShapeClips created or updated successfully.");
    }

    private void DeleteAllBlendShapeClips()
    {
        var blendShapeAvatar = blendShapeProxy.BlendShapeAvatar;
        if (blendShapeAvatar != null)
        {
            // Get the asset path of the BlendShapeAvatar
            string avatarAssetPath = AssetDatabase.GetAssetPath(blendShapeAvatar);
            string avatarAssetDirectory = System.IO.Path.GetDirectoryName(avatarAssetPath);

            // Iterate over the Clips
            foreach (var clip in blendShapeAvatar.Clips)
            {
                if (clip != null)
                {
                    string clipAssetPath = AssetDatabase.GetAssetPath(clip);
                    if (!string.IsNullOrEmpty(clipAssetPath))
                    {
                        AssetDatabase.DeleteAsset(clipAssetPath);
                    }
                }
            }

            // Clear the Clips list
            blendShapeAvatar.Clips.Clear();

            EditorUtility.SetDirty(blendShapeAvatar);
            AssetDatabase.SaveAssets();

            Debug.Log("All BlendShapeClips have been deleted.");
        }
        else
        {
            Debug.LogWarning("BlendShapeAvatar is null. Cannot delete BlendShapeClips.");
        }
    }

    private void SetBlendShapeClipValues(BlendShapeClip clip, string clipName)
    {
        clipName = (clipName == "eyeBaseJiggleL") ? "eyeBaseSwingL" : clipName;
        clipName = (clipName == "eyeBaseJiggleR") ? "eyeBaseSwingR" : clipName;
        clipName = (clipName == "eyeJiggleL") ? "eyeCenterSwingL" : clipName;
        clipName = (clipName == "eyeJiggleR") ? "eyeCenterSwingR" : clipName;

        var blendShapeBindingList = new System.Collections.Generic.List<BlendShapeBinding>();

        foreach (var renderer in skinnedMeshRenderers)
        {
            var mesh = renderer.sharedMesh;
            if (mesh != null)
            {
                int index = mesh.GetBlendShapeIndex(clipName);
                if (index >= 0)
                {
                    var binding = new BlendShapeBinding
                    {
                        RelativePath = GetRelativePath(avatar.transform, renderer.transform),
                        Index = index,
                        Weight = 100f
                    };
                    blendShapeBindingList.Add(binding);
                }
            }
        }

        if (blendShapeBindingList.Count > 0)
        {
            clip.Values = blendShapeBindingList.ToArray();
        }
    }

    private string GetRelativePath(Transform root, Transform target)
    {
        System.Text.StringBuilder path = new System.Text.StringBuilder();
        while (target != root && target != null)
        {
            path.Insert(0, "/" + target.name);
            target = target.parent;
        }
        if (target == null)
        {
            return null;
        }
        if (path.Length > 0)
            path.Remove(0, 1); // Remove leading slash
        return path.ToString();
    }
}
