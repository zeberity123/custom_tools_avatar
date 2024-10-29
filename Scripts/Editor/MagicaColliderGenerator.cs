using UnityEditor;
using UnityEngine;
using UnityEngine.Animations; // Required for HumanBodyBones
using MagicaCloth2;
using System.Collections.Generic;           // Ensure this matches the namespace of MagicaCloth2

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

        Animator animator = avatar.GetComponent<Animator>();
        if (animator == null)
        {
            Debug.LogError("Animator component not found on the avatar.");
            return;
        }

        // Start undo operation for Unity's undo system
        Undo.RegisterFullObjectHierarchyUndo(avatar, "Generate MagicaCloth2 Colliders");

        // Bones to exclude (LeftHand and RightHand and their child bones)
        Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        Transform hip = animator.GetBoneTransform(HumanBodyBones.Hips);
        Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        Transform neck = animator.GetBoneTransform(HumanBodyBones.Neck);



        // Collect all bones to exclude (LeftHand and RightHand and their descendants)
        HashSet<Transform> bonesToExclude = new HashSet<Transform>();
        if (leftHand != null)
            foreach (Transform child in leftHand)
            {
                CollectChildBones(child, bonesToExclude);
            }
        if (rightHand != null)
            foreach (Transform child in rightHand)
            {
                CollectChildBones(child, bonesToExclude);
            }

        bonesToExclude.Add(hip);
        bonesToExclude.Add(spine);
        bonesToExclude.Add(neck);

        // Iterate through all HumanBodyBones
        foreach (HumanBodyBones boneEnum in System.Enum.GetValues(typeof(HumanBodyBones)))
        {
            // Skip the LastBone enum value
            if (boneEnum == HumanBodyBones.LastBone)
                continue;

            Transform bone = animator.GetBoneTransform(boneEnum);
            if (bone == null)
                continue; // Bone not assigned in the avatar

            // Skip if the bone is in the exclusion list
            if (bonesToExclude.Contains(bone))
                continue;

            // Skip if the bone already has a MagicaCapsuleCollider
            if (bone.GetComponentInChildren<MagicaCapsuleCollider>())
                continue;

            // Find the first child bone to determine the length
            Transform childBone = GetFirstChildBone(bone, animator, bonesToExclude);
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

    private void ConfigureCapsuleCollider(Transform bone, Transform childBone, MagicaCapsuleCollider capsuleCollider, float boneLength)
    {
        // Determine the direction based on the bone's orientation
        MagicaCapsuleCollider.Direction direction = GetCapsuleDirection(bone, childBone);
        capsuleCollider.direction = direction;

        // Set the collider size
        float radius = boneLength * 0.2f; // Adjust as needed (increased to 0.2f for better visibility)
        float length = boneLength;

        // Since MagicaCapsuleCollider uses SetSize(float startRadius, float endRadius, float length)
        capsuleCollider.SetSize(radius, radius, length);
        capsuleCollider.radiusSeparation = false;

        // Set alignedOnCenter to false to align from the start point
        capsuleCollider.alignedOnCenter = false;

        // Calculate the center offset
        Vector3 centerOffset = CalculateCenterOffset(direction, length, capsuleCollider.alignedOnCenter);
        capsuleCollider.center = centerOffset;

        // Set reverseDirection if needed (optional, depending on bone orientation)
        capsuleCollider.reverseDirection = false; // Set to true if necessary

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

    private Transform GetFirstChildBone(Transform bone, Animator animator, HashSet<Transform> bonesToExclude)
    {
        // Iterate over children to find the first valid bone in the HumanBodyBones and not in the exclusion list
        foreach (Transform child in bone)
        {
            if (bonesToExclude.Contains(child))
                continue;

            foreach (HumanBodyBones boneEnum in System.Enum.GetValues(typeof(HumanBodyBones)))
            {
                if (boneEnum == HumanBodyBones.LastBone)
                    continue;

                Transform childBone = animator.GetBoneTransform(boneEnum);
                if (childBone == child)
                {
                    return childBone;
                }
            }
        }
        return null;
    }

    private void CollectChildBones(Transform parentBone, HashSet<Transform> bonesToExclude)
    {
        if (parentBone == null)
            return;

        bonesToExclude.Add(parentBone);

        foreach (Transform child in parentBone)
        {
            CollectChildBones(child, bonesToExclude);
        }
    }
}
