using UnityEngine;
using UnityEditor;
using VRM;
using System.Collections.Generic;

public class VRMColliderGenerator : EditorWindow
{
    private GameObject vrmAvatar;

    // Slider variables for the size (radius) of spheres between joints
    private float sizeBetweenLeftWristAndElbow = 0.02f;
    private float sizeBetweenLeftElbowAndArm = 0.02f;
    private float sizeBetweenRightWristAndElbow = 0.02f;
    private float sizeBetweenRightElbowAndArm = 0.02f;
    private float sizeBetweenLeftAnkleAndKnee = 0.03f;
    private float sizeBetweenLeftKneeAndLeg = 0.05f;
    private float sizeBetweenRightAnkleAndKnee = 0.03f;
    private float sizeBetweenRightKneeAndLeg = 0.05f;

    // References to colliders for immediate updates
    private List<VRMSpringBoneColliderGroup.SphereCollider> collidersBetweenLeftWristAndElbow = new List<VRMSpringBoneColliderGroup.SphereCollider>();
    private List<VRMSpringBoneColliderGroup.SphereCollider> collidersBetweenLeftElbowAndArm = new List<VRMSpringBoneColliderGroup.SphereCollider>();
    private List<VRMSpringBoneColliderGroup.SphereCollider> collidersBetweenRightWristAndElbow = new List<VRMSpringBoneColliderGroup.SphereCollider>();
    private List<VRMSpringBoneColliderGroup.SphereCollider> collidersBetweenRightElbowAndArm = new List<VRMSpringBoneColliderGroup.SphereCollider>();
    private List<VRMSpringBoneColliderGroup.SphereCollider> collidersBetweenLeftAnkleAndKnee = new List<VRMSpringBoneColliderGroup.SphereCollider>();
    private List<VRMSpringBoneColliderGroup.SphereCollider> collidersBetweenLeftKneeAndLeg = new List<VRMSpringBoneColliderGroup.SphereCollider>();
    private List<VRMSpringBoneColliderGroup.SphereCollider> collidersBetweenRightAnkleAndKnee = new List<VRMSpringBoneColliderGroup.SphereCollider>();
    private List<VRMSpringBoneColliderGroup.SphereCollider> collidersBetweenRightKneeAndLeg = new List<VRMSpringBoneColliderGroup.SphereCollider>();

    // Flag to check if colliders have been generated
    private bool collidersGenerated = false;

    private List<(string boneName, string[] partialNames)> chestColliderBones = new List<(string, string[])>
    {
        // Existing bones
        ("Hips_L", new string[] { "Hips_L", "hip_L", "LeftUpLeg", "UpperLeg_L", "LeftHip" }),
        ("Hips_R", new string[] { "Hips_R", "hip_R", "RightUpLeg", "UpperLeg_R", "RightHip" }),
        ("Left leg", new string[] { "Left leg", "LeftLeg", "Leg_L", "LeftUpLeg" }),
        ("Left knee", new string[] { "Left knee", "LeftKnee", "LeftLeg", "Knee_L" }),
        ("Left ankle", new string[] { "Left ankle", "LeftAnkle", "LeftFoot", "Ankle_L" }),
        ("Right leg", new string[] { "Right leg", "RightLeg", "Leg_R", "RightUpLeg" }),
        ("Right knee", new string[] { "Right knee", "RightKnee", "RightLeg", "Knee_R" }),
        ("Right ankle", new string[] { "Right ankle", "RightAnkle", "RightFoot", "Ankle_R" }),
        ("Spine", new string[] { "Spine", "spine" }),
        ("Chest", new string[] { "Chest", "chest", "UpperChest" }),
        ("Breast_L", new string[] { "Breast_L", "LeftBreast", "BreastL", "Breast_L" }),
        ("Breast_R", new string[] { "Breast_R", "RightBreast", "BreastR", "Breast_R" }),
        ("Left shoulder", new string[] { "Left shoulder", "LeftShoulder", "Shoulder_L" }),
        ("Left arm", new string[] { "Left arm", "LeftArm", "Arm_L", "LeftUpperArm" }),
        ("Left elbow", new string[] { "Left elbow", "LeftElbow", "elbow_L", "LeftElbow" }),
        ("Left wrist", new string[] { "Left wrist", "LeftWrist", "Wrist_L", "LeftHand" }),
        ("Right shoulder", new string[] { "Right shoulder", "RightShoulder", "Shoulder_R" }),
        ("Right arm", new string[] { "Right arm", "RightArm", "Arm_R", "RightUpperArm" }),
        ("Right elbow", new string[] { "Right elbow", "RightElbow", "elbow_R", "RightElbow" }),
        ("Right wrist", new string[] { "Right wrist", "RightWrist", "Wrist_R", "RightHand" })
    };

    [MenuItem("Tools/VRM Collider Generator")]
    public static void ShowWindow()
    {
        GetWindow<VRMColliderGenerator>("VRM Collider Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("VRM Avatar", EditorStyles.boldLabel);
        vrmAvatar = (GameObject)EditorGUILayout.ObjectField("Avatar", vrmAvatar, typeof(GameObject), true);

        if (GUILayout.Button("Generate Colliders"))
        {
            if (vrmAvatar != null)
            {
                GenerateColliders();
                collidersGenerated = true;
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign a VRM avatar.", "OK");
            }
        }

        if (GUILayout.Button("Delete All Colliders"))
        {
            DeleteAllColliders();
        }

        if (collidersGenerated)
        {
            GUILayout.Space(10);
            GUILayout.Label("Adjust Sphere Sizes Between Joints", EditorStyles.boldLabel);

            // Define the size range and step
            float minSize = 0.01f;
            float maxSize = 0.1f;

            // Left Arm
            sizeBetweenLeftWristAndElbow = EditorGUILayout.Slider("Left Wrist to Elbow Size", sizeBetweenLeftWristAndElbow, minSize, maxSize);
            sizeBetweenLeftElbowAndArm = EditorGUILayout.Slider("Left Elbow to Arm Size", sizeBetweenLeftElbowAndArm, minSize, maxSize);

            // Right Arm
            sizeBetweenRightWristAndElbow = EditorGUILayout.Slider("Right Wrist to Elbow Size", sizeBetweenRightWristAndElbow, minSize, maxSize);
            sizeBetweenRightElbowAndArm = EditorGUILayout.Slider("Right Elbow to Arm Size", sizeBetweenRightElbowAndArm, minSize, maxSize);

            // Left Leg
            sizeBetweenLeftAnkleAndKnee = EditorGUILayout.Slider("Left Ankle to Knee Size", sizeBetweenLeftAnkleAndKnee, minSize, maxSize);
            sizeBetweenLeftKneeAndLeg = EditorGUILayout.Slider("Left Knee to Leg Size", sizeBetweenLeftKneeAndLeg, minSize, maxSize);

            // Right Leg
            sizeBetweenRightAnkleAndKnee = EditorGUILayout.Slider("Right Ankle to Knee Size", sizeBetweenRightAnkleAndKnee, minSize, maxSize);
            sizeBetweenRightKneeAndLeg = EditorGUILayout.Slider("Right Knee to Leg Size", sizeBetweenRightKneeAndLeg, minSize, maxSize);

            // Apply changes immediately
            UpdateColliderSizes();
        }
    }

    private void GenerateColliders()
    {
        // Reset collider lists
        collidersBetweenLeftWristAndElbow.Clear();
        collidersBetweenLeftElbowAndArm.Clear();
        collidersBetweenRightWristAndElbow.Clear();
        collidersBetweenRightElbowAndArm.Clear();
        collidersBetweenLeftAnkleAndKnee.Clear();
        collidersBetweenLeftKneeAndLeg.Clear();
        collidersBetweenRightAnkleAndKnee.Clear();
        collidersBetweenRightKneeAndLeg.Clear();

        // Generate chest colliders
        GenerateChestColliders();

        // Generate head colliders
        GenerateHeadColliders();
    }

    private void GenerateChestColliders()
    {
        // For the chest collider group
        Transform chestBone = FindBone(vrmAvatar.transform, "Chest") ?? FindBoneByPartialName(vrmAvatar.transform, "Chest");

        if (chestBone == null)
        {
            EditorUtility.DisplayDialog("Error", "Chest bone not found.", "OK");
            return;
        }

        // Remove existing collider group if it exists
        Transform existingColliderGroup = chestBone.Find("Chest_Col");
        if (existingColliderGroup != null)
        {
            DestroyImmediate(existingColliderGroup.gameObject);
        }

        // Create the collider group under the Chest bone
        GameObject colliderGroupObj = new GameObject("Chest_Col");
        colliderGroupObj.transform.SetParent(chestBone, false);

        VRMSpringBoneColliderGroup colliderGroup = colliderGroupObj.AddComponent<VRMSpringBoneColliderGroup>();

        // Use a list to collect all colliders
        List<VRMSpringBoneColliderGroup.SphereCollider> colliders = new List<VRMSpringBoneColliderGroup.SphereCollider>();

        // Existing colliders at specified bones
        foreach (var (boneName, partialNames) in chestColliderBones)
        {
            Transform bone = FindBoneByPartialNames(vrmAvatar.transform, partialNames);

            if (bone != null)
            {
                Vector3 localPosition = colliderGroupObj.transform.InverseTransformPoint(bone.position);
                float radius = CalculateBoneColliderRadius(bone);

                colliders.Add(CreateSphereCollider(localPosition, radius));
            }
            else
            {
                Debug.LogWarning($"Bone '{boneName}' not found. Collider will not be created for this bone.");
            }
        }

        // Add spheres between joints and store references
        colliders.AddRange(AddSpheresBetweenBones(colliderGroupObj.transform, "Left wrist", "Left elbow", sizeBetweenLeftWristAndElbow, collidersBetweenLeftWristAndElbow));
        colliders.AddRange(AddSpheresBetweenBones(colliderGroupObj.transform, "Left elbow", "Left arm", sizeBetweenLeftElbowAndArm, collidersBetweenLeftElbowAndArm));
        colliders.AddRange(AddSpheresBetweenBones(colliderGroupObj.transform, "Right wrist", "Right elbow", sizeBetweenRightWristAndElbow, collidersBetweenRightWristAndElbow));
        colliders.AddRange(AddSpheresBetweenBones(colliderGroupObj.transform, "Right elbow", "Right arm", sizeBetweenRightElbowAndArm, collidersBetweenRightElbowAndArm));
        colliders.AddRange(AddSpheresBetweenBones(colliderGroupObj.transform, "Left ankle", "Left knee", sizeBetweenLeftAnkleAndKnee, collidersBetweenLeftAnkleAndKnee));
        colliders.AddRange(AddSpheresBetweenBones(colliderGroupObj.transform, "Left knee", "Left leg", sizeBetweenLeftKneeAndLeg, collidersBetweenLeftKneeAndLeg));
        colliders.AddRange(AddSpheresBetweenBones(colliderGroupObj.transform, "Right ankle", "Right knee", sizeBetweenRightAnkleAndKnee, collidersBetweenRightAnkleAndKnee));
        colliders.AddRange(AddSpheresBetweenBones(colliderGroupObj.transform, "Right knee", "Right leg", sizeBetweenRightKneeAndLeg, collidersBetweenRightKneeAndLeg));

        colliderGroup.Colliders = colliders.ToArray();
    }

    private List<VRMSpringBoneColliderGroup.SphereCollider> AddSpheresBetweenBones(Transform colliderGroupTransform, string startBoneName, string endBoneName, float size, List<VRMSpringBoneColliderGroup.SphereCollider> colliderList)
    {
        var startBoneEntry = chestColliderBones.Find(b => b.boneName == startBoneName);
        var endBoneEntry = chestColliderBones.Find(b => b.boneName == endBoneName);

        if (startBoneEntry == default || endBoneEntry == default)
        {
            Debug.LogWarning($"Bone entries not found for '{startBoneName}' and/or '{endBoneName}'.");
            return new List<VRMSpringBoneColliderGroup.SphereCollider>();
        }

        Transform startBone = FindBoneByPartialNames(vrmAvatar.transform, startBoneEntry.partialNames);
        Transform endBone = FindBoneByPartialNames(vrmAvatar.transform, endBoneEntry.partialNames);

        if (startBone == null || endBone == null)
        {
            Debug.LogWarning($"Could not find bones '{startBoneName}' and/or '{endBoneName}'. Colliders will not be created between these bones.");
            return new List<VRMSpringBoneColliderGroup.SphereCollider>();
        }

        Vector3 startPosition = startBone.position;
        Vector3 endPosition = endBone.position;
        float distance = Vector3.Distance(startPosition, endPosition);

        // Calculate number of spheres needed so they slightly overlap
        int numSpheres = Mathf.Max(1, Mathf.CeilToInt(distance / (2 * size)));

        // Compute positions and add colliders
        List<VRMSpringBoneColliderGroup.SphereCollider> colliders = new List<VRMSpringBoneColliderGroup.SphereCollider>();

        for (int i = 1; i <= numSpheres; i++)
        {
            float t = (float)i / (numSpheres + 1); // t from 0 to 1
            Vector3 position = Vector3.Lerp(startPosition, endPosition, t);

            Vector3 localPosition = colliderGroupTransform.InverseTransformPoint(position);

            var collider = CreateSphereCollider(localPosition, size);
            colliders.Add(collider);
            colliderList.Add(collider); // Store reference for updates
        }

        return colliders;
    }
    
    private void DeleteAllColliders()
    {
        if (vrmAvatar == null)
        {
            Debug.LogError("Avatar is not assigned.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(vrmAvatar, "Delete VRM SpringBone Colliders");
        VRMSpringBoneColliderGroup[] colliderGroups = vrmAvatar.GetComponentsInChildren<VRMSpringBoneColliderGroup>();

        int colliderCount = colliderGroups.Length;

        if (colliderCount == 0)
        {
            Debug.Log("No VRM Colliders found to delete");
            return;
        }

        foreach (VRMSpringBoneColliderGroup collider in colliderGroups)
        {
            if (collider != null)
            {
                Undo.DestroyObjectImmediate(collider.gameObject);
            }
        }

        Debug.Log($"{colliderCount} colliders deleted successfully.");
    }

    private void UpdateColliderSizes()
    {
        // Update sizes for colliders between joints
        UpdateColliderListSizes(collidersBetweenLeftWristAndElbow, sizeBetweenLeftWristAndElbow);
        UpdateColliderListSizes(collidersBetweenLeftElbowAndArm, sizeBetweenLeftElbowAndArm);
        UpdateColliderListSizes(collidersBetweenRightWristAndElbow, sizeBetweenRightWristAndElbow);
        UpdateColliderListSizes(collidersBetweenRightElbowAndArm, sizeBetweenRightElbowAndArm);
        UpdateColliderListSizes(collidersBetweenLeftAnkleAndKnee, sizeBetweenLeftAnkleAndKnee);
        UpdateColliderListSizes(collidersBetweenLeftKneeAndLeg, sizeBetweenLeftKneeAndLeg);
        UpdateColliderListSizes(collidersBetweenRightAnkleAndKnee, sizeBetweenRightAnkleAndKnee);
        UpdateColliderListSizes(collidersBetweenRightKneeAndLeg, sizeBetweenRightKneeAndLeg);
    }

    private void UpdateColliderListSizes(List<VRMSpringBoneColliderGroup.SphereCollider> colliderList, float newSize)
    {
        foreach (var collider in colliderList)
        {
            collider.Radius = newSize;
        }
    }

    private void GenerateHeadColliders()
    {
        // For the head collider group
        Transform headBone = FindBone(vrmAvatar.transform, "Head");

        if (headBone == null)
        {
            EditorUtility.DisplayDialog("Error", "Head bone not found.", "OK");
            return;
        }

        // Remove existing collider group if it exists
        Transform existingColliderGroup = headBone.Find("Head_Col");
        if (existingColliderGroup != null)
        {
            DestroyImmediate(existingColliderGroup.gameObject);
        }

        // Find Eye bones
        Transform eyeLBone = FindBoneByPartialName(headBone, "Eye_L");
        Transform eyeRBone = FindBoneByPartialName(headBone, "Eye_R");

        // Find Hair Root bone
        Transform hairRootBone = FindBoneByPartialName(headBone, "Hair Root") ?? FindHighestChild(headBone);

        // Create the collider group under the Head bone
        GameObject colliderGroupObj = new GameObject("Head_Col");
        colliderGroupObj.transform.SetParent(headBone, false);

        VRMSpringBoneColliderGroup colliderGroup = colliderGroupObj.AddComponent<VRMSpringBoneColliderGroup>();
        List<VRMSpringBoneColliderGroup.SphereCollider> colliders = new List<VRMSpringBoneColliderGroup.SphereCollider>();

        // Get Head's Z position
        float headZ = headBone.position.z;

        // Create collider at the midpoint between the eyes
        if (eyeLBone != null && eyeRBone != null)
        {
            Vector3 eyeCenter = (eyeLBone.position + eyeRBone.position) / 2f;
            eyeCenter.z = headZ; // Set Z to Head's Z

            Vector3 localEyeCenter = colliderGroupObj.transform.InverseTransformPoint(eyeCenter);

            colliders.Add(CreateSphereCollider(localEyeCenter, 0.075f)); // Radius 0.075
        }
        else
        {
            Debug.LogWarning("Eye bones not found. Eye collider will not be created.");
        }

        // Create collider at the midpoint between eyes and hair root
        if (hairRootBone != null && eyeLBone != null && eyeRBone != null)
        {
            float yEyes = (eyeLBone.position.y + eyeRBone.position.y) / 2f;
            float yHairRoot = hairRootBone.position.y;

            float yMidpoint = (yEyes + yHairRoot) / 2f;

            Vector3 colliderPosition = new Vector3(headBone.position.x, yMidpoint, headZ);

            Vector3 localColliderPosition = colliderGroupObj.transform.InverseTransformPoint(colliderPosition);

            colliders.Add(CreateSphereCollider(localColliderPosition, 0.085f)); // Radius 0.085
        }
        else
        {
            Debug.LogWarning("Necessary bones not found. Second collider will not be created.");
        }

        colliderGroup.Colliders = colliders.ToArray();
    }

    private float CalculateBoneColliderRadius(Transform bone)
    {
        // You can set different radii based on bone names
        string boneName = bone.name.ToLower();

        if (boneName.Contains("wrist"))
        {
            return 0.01f; // Smaller radius for wrists and ankles
        }
        else if (boneName.Contains("elbow") || boneName.Contains("shoulder") || boneName.Contains("arm") || boneName.Contains("ankle"))
        {
            return 0.02f; // Medium radius for knees and elbows
        }
        else if (boneName.Contains("knee"))
        {
            return 0.04f;
        }
        else if (boneName.Contains("breast"))
        {
            return 0.07f; // Larger radius for breasts
        }
        else
        {
            return 0.05f; // Default radius
        }
    }

    private VRMSpringBoneColliderGroup.SphereCollider CreateSphereCollider(Vector3 offset, float radius)
    {
        return new VRMSpringBoneColliderGroup.SphereCollider
        {
            Offset = offset,
            Radius = radius
        };
    }

    private Transform FindBone(Transform root, string boneName)
    {
        string lowerBoneName = boneName.ToLower();

        foreach (Transform child in root.GetComponentsInChildren<Transform>())
        {
            if (child.name.ToLower() == lowerBoneName)
            {
                return child;
            }
        }
        return null;
    }

    private Transform FindBoneByPartialName(Transform root, string partialName)
    {
        string lowerPartialName = partialName.ToLower();

        foreach (Transform child in root.GetComponentsInChildren<Transform>())
        {
            if (child.name.ToLower().Contains(lowerPartialName))
            {
                return child;
            }
        }
        return null;
    }

    private Transform FindBoneByPartialNames(Transform root, string[] partialNames)
    {
        foreach (Transform child in root.GetComponentsInChildren<Transform>())
        {
            string lowerChildName = child.name.ToLower();
            foreach (string partialName in partialNames)
            {
                if (lowerChildName.Contains(partialName.ToLower()))
                {
                    return child;
                }
            }
        }
        return null;
    }

    private Transform FindHighestChild(Transform parent)
    {
        Transform highestChild = null;
        float highestY = float.MinValue;

        foreach (Transform child in parent.GetComponentsInChildren<Transform>())
        {
            if (child == parent) continue; // Skip self

            float childY = child.position.y;

            if (childY > highestY)
            {
                highestY = childY;
                highestChild = child;
            }
        }

        return highestChild;
    }
}
