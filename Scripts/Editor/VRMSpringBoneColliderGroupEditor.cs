using UnityEngine;
using UnityEditor;
using VRM;
using System.Collections.Generic;

public class VRMColliderGenerator : EditorWindow
{
    private GameObject avatar;
    private Animator animator;

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

    private Dictionary<Transform, HumanBodyBones> boneTransformMap;

    [MenuItem("Tools/VRM Collider Generator")]
    public static void ShowWindow()
    {
        GetWindow<VRMColliderGenerator>("VRM Collider Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("VRM Avatar", EditorStyles.boldLabel);
        GameObject previousAvatar = avatar;
        avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", avatar, typeof(GameObject), true);

        if (avatar != previousAvatar)
        {
            animator = avatar != null ? avatar.GetComponent<Animator>() : null;
            collidersGenerated = false;
        }

        if (GUILayout.Button("Generate Colliders"))
        {
            if (avatar != null)
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

        // Get the Animator component
        animator = avatar.GetComponent<Animator>();
        if (animator == null)
        {
            EditorUtility.DisplayDialog("Error", "Animator component not found on the avatar.", "OK");
            return;
        }

        // Build a mapping from bone Transform to HumanBodyBones enum
        boneTransformMap = new Dictionary<Transform, HumanBodyBones>();

        foreach (HumanBodyBones boneEnum in System.Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (boneEnum == HumanBodyBones.LastBone)
                continue;

            Transform boneTransform = animator.GetBoneTransform(boneEnum);
            if (boneTransform != null)
            {
                boneTransformMap[boneTransform] = boneEnum;
            }
        }

        // Generate colliders for individual bones
        GenerateBoneColliders();

        // Generate colliders between bones
        GenerateBetweenBoneColliders();

        // Generate head colliders
        GenerateHeadColliders();
    }

    private void GenerateBoneColliders()
    {
        // List of bones to create colliders for
        List<HumanBodyBones> bonesToCreateColliders = new List<HumanBodyBones>
        {
            HumanBodyBones.Hips,
            HumanBodyBones.Spine,
            HumanBodyBones.Chest,
            HumanBodyBones.LeftUpperLeg,
            HumanBodyBones.LeftLowerLeg,
            HumanBodyBones.LeftFoot,
            HumanBodyBones.RightUpperLeg,
            HumanBodyBones.RightLowerLeg,
            HumanBodyBones.RightFoot,
            HumanBodyBones.LeftUpperArm,
            HumanBodyBones.LeftLowerArm,
            HumanBodyBones.LeftHand,
            HumanBodyBones.RightUpperArm,
            HumanBodyBones.RightLowerArm,
            HumanBodyBones.RightHand
        };

        foreach (HumanBodyBones boneEnum in bonesToCreateColliders)
        {
            Transform bone = animator.GetBoneTransform(boneEnum);

            if (bone != null)
            {
                // Remove existing collider group if it exists
                Transform existingColliderGroup = bone.Find(bone.name + "_Col");
                if (existingColliderGroup != null)
                {
                    DestroyImmediate(existingColliderGroup.gameObject);
                }

                // Create the collider group under the bone
                GameObject colliderGroupObj = new GameObject(bone.name + "_Col");
                colliderGroupObj.transform.SetParent(bone, false);

                VRMSpringBoneColliderGroup colliderGroup = colliderGroupObj.AddComponent<VRMSpringBoneColliderGroup>();

                Vector3 localPosition = Vector3.zero; // The collider is at the bone's position
                float radius = CalculateBoneColliderRadius(bone);

                var collider = CreateSphereCollider(localPosition, radius);
                colliderGroup.Colliders = new VRMSpringBoneColliderGroup.SphereCollider[] { collider };
            }
            else
            {
                Debug.LogWarning($"Bone '{boneEnum}' not found. Collider will not be created for this bone.");
            }
        }
    }

    private void GenerateBetweenBoneColliders()
    {
        // Add spheres between joints and store references
        AddSpheresBetweenBones(HumanBodyBones.LeftHand, HumanBodyBones.LeftLowerArm, sizeBetweenLeftWristAndElbow, collidersBetweenLeftWristAndElbow);
        AddSpheresBetweenBones(HumanBodyBones.LeftLowerArm, HumanBodyBones.LeftUpperArm, sizeBetweenLeftElbowAndArm, collidersBetweenLeftElbowAndArm);
        AddSpheresBetweenBones(HumanBodyBones.RightHand, HumanBodyBones.RightLowerArm, sizeBetweenRightWristAndElbow, collidersBetweenRightWristAndElbow);
        AddSpheresBetweenBones(HumanBodyBones.RightLowerArm, HumanBodyBones.RightUpperArm, sizeBetweenRightElbowAndArm, collidersBetweenRightElbowAndArm);
        AddSpheresBetweenBones(HumanBodyBones.LeftFoot, HumanBodyBones.LeftLowerLeg, sizeBetweenLeftAnkleAndKnee, collidersBetweenLeftAnkleAndKnee);
        AddSpheresBetweenBones(HumanBodyBones.LeftLowerLeg, HumanBodyBones.LeftUpperLeg, sizeBetweenLeftKneeAndLeg, collidersBetweenLeftKneeAndLeg);
        AddSpheresBetweenBones(HumanBodyBones.RightFoot, HumanBodyBones.RightLowerLeg, sizeBetweenRightAnkleAndKnee, collidersBetweenRightAnkleAndKnee);
        AddSpheresBetweenBones(HumanBodyBones.RightLowerLeg, HumanBodyBones.RightUpperLeg, sizeBetweenRightKneeAndLeg, collidersBetweenRightKneeAndLeg);
    }

    private void AddSpheresBetweenBones(HumanBodyBones startBoneEnum, HumanBodyBones endBoneEnum, float size, List<VRMSpringBoneColliderGroup.SphereCollider> colliderList)
    {
        Transform startBone = animator.GetBoneTransform(startBoneEnum);
        Transform endBone = animator.GetBoneTransform(endBoneEnum);

        if (startBone == null || endBone == null)
        {
            Debug.LogWarning($"Could not find bones '{startBoneEnum}' and/or '{endBoneEnum}'. Colliders will not be created between these bones.");
            return;
        }

        // Create the collider group under the start bone
        Transform existingColliderGroup = startBone.Find(startBone.name + "_Col");
        VRMSpringBoneColliderGroup colliderGroup;

        if (existingColliderGroup != null)
        {
            colliderGroup = existingColliderGroup.GetComponent<VRMSpringBoneColliderGroup>();
        }
        else
        {
            GameObject colliderGroupObj = new GameObject(startBone.name + "_Col");
            colliderGroupObj.transform.SetParent(startBone, false);
            colliderGroup = colliderGroupObj.AddComponent<VRMSpringBoneColliderGroup>();
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

            Vector3 localPosition = startBone.InverseTransformPoint(position);

            var collider = CreateSphereCollider(localPosition, size);
            colliders.Add(collider);
            colliderList.Add(collider); // Store reference for updates
        }

        // Add colliders to the collider group
        var existingColliders = new List<VRMSpringBoneColliderGroup.SphereCollider>(colliderGroup.Colliders ?? new VRMSpringBoneColliderGroup.SphereCollider[0]);
        existingColliders.AddRange(colliders);
        colliderGroup.Colliders = existingColliders.ToArray();
    }

    private void DeleteAllColliders()
    {
        if (avatar == null)
        {
            Debug.LogError("Avatar is not assigned.");
            return;
        }

        Undo.RegisterFullObjectHierarchyUndo(avatar, "Delete VRM SpringBone Colliders");
        VRMSpringBoneColliderGroup[] colliderGroups = avatar.GetComponentsInChildren<VRMSpringBoneColliderGroup>();

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
        Transform headBone = animator.GetBoneTransform(HumanBodyBones.Head);

        if (headBone == null)
        {
            EditorUtility.DisplayDialog("Error", "Head bone not found.", "OK");
            return;
        }

        // Remove existing collider group if it exists
        Transform existingColliderGroup = headBone.Find(headBone.name + "_Col");
        if (existingColliderGroup != null)
        {
            DestroyImmediate(existingColliderGroup.gameObject);
        }

        // Find Eye bones
        Transform eyeLBone = animator.GetBoneTransform(HumanBodyBones.LeftEye);
        Transform eyeRBone = animator.GetBoneTransform(HumanBodyBones.RightEye);

        // Find Hair Root bone
        Transform hairRootBone = FindHighestChild(headBone);

        // Create the collider group under the Head bone
        GameObject colliderGroupObj = new GameObject(headBone.name + "_Col");
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
        HumanBodyBones boneEnum;
        if (!boneTransformMap.TryGetValue(bone, out boneEnum))
        {
            // Default radius if bone not in map
            return 0.05f;
        }

        if (boneEnum == HumanBodyBones.LeftHand || boneEnum == HumanBodyBones.RightHand || boneEnum == HumanBodyBones.LeftFoot || boneEnum == HumanBodyBones.RightFoot)
        {
            return 0.01f; // Smaller radius for wrists and ankles
        }
        else if (boneEnum == HumanBodyBones.LeftLowerArm || boneEnum == HumanBodyBones.RightLowerArm || boneEnum == HumanBodyBones.LeftUpperArm || boneEnum == HumanBodyBones.RightUpperArm || boneEnum == HumanBodyBones.LeftLowerLeg || boneEnum == HumanBodyBones.RightLowerLeg || boneEnum == HumanBodyBones.LeftUpperLeg || boneEnum == HumanBodyBones.RightUpperLeg)
        {
            return 0.02f; // Medium radius for arms and legs
        }
        else if (boneEnum == HumanBodyBones.LeftEye || boneEnum == HumanBodyBones.RightEye || boneEnum == HumanBodyBones.Head)
        {
            return 0.07f; // Larger radius for head and eyes
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
