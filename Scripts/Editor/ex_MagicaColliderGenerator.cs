using UnityEditor;
using UnityEngine;
using System.Collections.Generic;
using MagicaCloth2;
// Include VRM namespaces
using VRM;

public class MagicaColliderGenerator : EditorWindow
{
    private GameObject avatar;

    [MenuItem("Tools/MagicaColliderGenerator")]
    public static void ShowWindow()
    {
        GetWindow<MagicaColliderGenerator>("MagicaColliderGenerator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Magica Capsule Collider Generator", EditorStyles.boldLabel);

        avatar = (GameObject)EditorGUILayout.ObjectField("Avatar", avatar, typeof(GameObject), true);

        if (avatar != null)
        {
            if (GUILayout.Button("Generate Colliders"))
            {
                GenerateColliders();
            }
        }
    }

    private void GenerateColliders()
    {
        if (avatar == null)
        {
            Debug.LogError("Avatar is not assigned.");
            return;
        }

        // Get the VRMHumanoidDescription component
        var vrmHumanoid = avatar.GetComponent<VRMHumanoidDescription>();
        if (vrmHumanoid == null)
        {
            Debug.LogError("VRMHumanoidDescription component not found on the avatar.");
            return;
        }

        // Start undo operation for Unity's undo system
        Undo.RegisterFullObjectHierarchyUndo(avatar, "Generate MagicaCloth2 Colliders");

        // Bones to exclude
        HumanBodyBones[] bonesToExcludeEnums = new HumanBodyBones[]
        {
            // Left Hand Fingers
            HumanBodyBones.LeftThumbProximal,
            HumanBodyBones.LeftThumbIntermediate,
            HumanBodyBones.LeftThumbDistal,
            HumanBodyBones.LeftIndexProximal,
            HumanBodyBones.LeftIndexIntermediate,
            HumanBodyBones.LeftIndexDistal,
            HumanBodyBones.LeftMiddleProximal,
            HumanBodyBones.LeftMiddleIntermediate,
            HumanBodyBones.LeftMiddleDistal,
            HumanBodyBones.LeftRingProximal,
            HumanBodyBones.LeftRingIntermediate,
            HumanBodyBones.LeftRingDistal,
            HumanBodyBones.LeftLittleProximal,
            HumanBodyBones.LeftLittleIntermediate,
            HumanBodyBones.LeftLittleDistal,

            // Right Hand Fingers
            HumanBodyBones.RightThumbProximal,
            HumanBodyBones.RightThumbIntermediate,
            HumanBodyBones.RightThumbDistal,
            HumanBodyBones.RightIndexProximal,
            HumanBodyBones.RightIndexIntermediate,
            HumanBodyBones.RightIndexDistal,
            HumanBodyBones.RightMiddleProximal,
            HumanBodyBones.RightMiddleIntermediate,
            HumanBodyBones.RightMiddleDistal,
            HumanBodyBones.RightRingProximal,
            HumanBodyBones.RightRingIntermediate,
            HumanBodyBones.RightRingDistal,
            HumanBodyBones.RightLittleProximal,
            HumanBodyBones.RightLittleIntermediate,
            HumanBodyBones.RightLittleDistal,

            // Others a
            HumanBodyBones.LeftShoulder,
            HumanBodyBones.RightShoulder,
            HumanBodyBones.Spine,
            HumanBodyBones.Neck,
        };

        // Collect all bones to exclude
        HashSet<Transform> bonesToExclude = new HashSet<Transform>();
        foreach (var boneEnum in bonesToExcludeEnums)
        {
            var bone = GetBoneTransformFromVRM(vrmHumanoid, boneEnum);
            if (bone != null)
            {
                bonesToExclude.Add(bone);
            }
        }

        // Iterate through all HumanBodyBones
        foreach (HumanBodyBones boneEnum in System.Enum.GetValues(typeof(HumanBodyBones)))
        {
            if (boneEnum == HumanBodyBones.LastBone)
                continue;

            var bone = GetBoneTransformFromVRM(vrmHumanoid, boneEnum);
            if (bone == null)
                continue;

            if (bonesToExclude.Contains(bone))
                continue;

            if (bone.GetComponentInChildren<MagicaCapsuleCollider>())
                continue;

            // Find the first child bone to determine the length
            Transform childBone = GetFirstChildBoneVRM(bone, vrmHumanoid, bonesToExclude);
            if (childBone == null)
                continue;

            // Calculate the distance to the child bone
            float boneLength = Vector3.Distance(bone.position, childBone.position);

            // Create a new GameObject for the collider
            GameObject colliderObj = new GameObject("Collider_" + bone.name);
            colliderObj.transform.SetParent(bone, false);
            colliderObj.transform.localPosition = Vector3.zero;
            colliderObj.transform.localRotation = Quaternion.identity;

            // Add MagicaCapsuleCollider component
            MagicaCapsuleCollider capsuleCollider = colliderObj.AddComponent<MagicaCapsuleCollider>();

            // Configure the collider
            ConfigureCapsuleCollider(bone, childBone, capsuleCollider, boneLength);
        }

        Debug.Log("Colliders generated successfully.");
    }

    private Transform GetBoneTransformFromVRM(VRMHumanoidDescription vrmHumanoid, HumanBodyBones boneEnum)
    {
        var humanoid = vrmHumanoid.Description.human;
        if (humanoid == null)
        {
            Debug.LogError("VRM Humanoid Description is missing.");
            return null;
        }

        foreach (var humanBone in humanoid)
        {
            if (humanBone.humanBone == boneEnum)
            {
                // Find the transform by name
                Transform boneTransform = FindTransformRecursive(avatar.transform, humanBone.boneName);
                return boneTransform;
            }
        }
        return null;
    }

    private Transform FindTransformRecursive(Transform parent, string name)
    {
        if (parent.name == name)
            return parent;

        foreach (Transform child in parent)
        {
            var result = FindTransformRecursive(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    private Transform GetFirstChildBoneVRM(Transform bone, VRMHumanoidDescription vrmHumanoid, HashSet<Transform> bonesToExclude)
    {
        // Iterate over the humanoid bones to find a child bone that is a humanoid bone
        foreach (var humanBone in vrmHumanoid.human)
        {
            var childTransform = FindTransformRecursive(avatar.transform, humanBone.boneName);
            if (childTransform != null && childTransform.parent == bone && !bonesToExclude.Contains(childTransform))
            {
                return childTransform;
            }
        }
        return null;
    }

    private void ConfigureCapsuleCollider(Transform bone, Transform childBone, MagicaCapsuleCollider capsuleCollider, float boneLength)
    {
        // Determine the direction based on the bone's orientation
        MagicaCapsuleCollider.Direction direction = GetCapsuleDirection(bone, childBone);
        capsuleCollider.direction = direction;

        // Set the collider size
        float radius = boneLength * 0.2f; // Adjust as needed
        float length = boneLength;

        // Set the size of the capsule collider
        capsuleCollider.SetSize(radius, radius, length);
        capsuleCollider.radiusSeparation = false;

        // Set alignedOnCenter to false to align from the start point
        capsuleCollider.alignedOnCenter = false;

        // Calculate the center offset
        Vector3 centerOffset = CalculateCenterOffset(direction, length, capsuleCollider.alignedOnCenter);
        capsuleCollider.center = centerOffset;

        // Set reverseDirection if needed (optional)
        capsuleCollider.reverseDirection = false; // Adjust as needed

        // Visualization
        #if UNITY_EDITOR
        // capsuleCollider.DrawGizmo = true;
        #endif
    }

    private Vector3 CalculateCenterOffset(MagicaCapsuleCollider.Direction direction, float length, bool alignedOnCenter)
    {
        if (alignedOnCenter)
        {
            return Vector3.zero;
        }
        else
        {
            // Offset the center to align the capsule from the start point
            switch (direction)
            {
                case MagicaCapsuleCollider.Direction.X:
                    return new Vector3(length / 2, 0, 0);
                case MagicaCapsuleCollider.Direction.Y:
                    return new Vector3(0, length / 2, 0);
                case MagicaCapsuleCollider.Direction.Z:
                    return new Vector3(0, 0, length / 2);
                default:
                    return Vector3.zero;
            }
        }
    }

    private MagicaCapsuleCollider.Direction GetCapsuleDirection(Transform bone, Transform childBone)
    {
        Vector3 localDirection = bone.InverseTransformDirection(childBone.position - bone.position);
        Vector3 absDirection = new Vector3(Mathf.Abs(localDirection.x), Mathf.Abs(localDirection.y), Mathf.Abs(localDirection.z));

        if (absDirection.x > absDirection.y && absDirection.x > absDirection.z)
        {
            return MagicaCapsuleCollider.Direction.X;
        }
        else if (absDirection.y > absDirection.x && absDirection.y > absDirection.z)
        {
            return MagicaCapsuleCollider.Direction.Y;
        }
        else
        {
            return MagicaCapsuleCollider.Direction.Z;
        }
    }
}
