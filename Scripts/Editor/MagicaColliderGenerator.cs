using UnityEditor;
using UnityEngine;
using UnityEngine.Animations; // Required for HumanBodyBones
using MagicaCloth2;
using System.Collections.Generic;

public class MagicaColliderGenerator : EditorWindow
{
    private GameObject avatar;
    private GameObject previousAvatar = null;

    private List<ColliderInfo> collidersInfoList = new List<ColliderInfo>();

    // Order in which to display colliders
    private List<HumanBodyBones> boneDisplayOrder = new List<HumanBodyBones>
    {
        HumanBodyBones.Head,
        HumanBodyBones.Neck,
        HumanBodyBones.Chest,
        HumanBodyBones.Spine,
        HumanBodyBones.Hips,
        HumanBodyBones.LeftUpperArm,  // Left arm
        HumanBodyBones.LeftLowerArm,  // Left elbow
        HumanBodyBones.RightUpperArm, // Right arm
        HumanBodyBones.RightLowerArm, // Right elbow
        HumanBodyBones.LeftUpperLeg,  // Left leg
        HumanBodyBones.LeftLowerLeg,  // Left knee
        HumanBodyBones.RightUpperLeg, // Right leg
        HumanBodyBones.RightLowerLeg, // Right knee
    };

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
            // Check if the avatar has changed
            if (avatar != previousAvatar)
            {
                previousAvatar = avatar;
                ScanForColliders();
            }

            if (GUILayout.Button("Generate Magica Colliders"))
            {
                GenerateColliders();
                ScanForColliders();
            }

            if (GUILayout.Button("Select All Colliders"))
            {   
                ScanForColliders();
                SelectAllColliders();
            }

            if (GUILayout.Button("Delete All Colliders"))
            {
                DeleteAllColliders();
                ScanForColliders();
            }
            

            GUILayout.Space(10);
            GUILayout.Label("Existing Colliders:", EditorStyles.boldLabel);

            if (collidersInfoList != null && collidersInfoList.Count > 0)
            {
                foreach (var colliderInfo in collidersInfoList)
                {
                    if (colliderInfo.collider != null)
                    {
                        if (GUILayout.Button(colliderInfo.collider.name))
                        {
                            // Select the collider in the editor
                            Selection.activeGameObject = colliderInfo.collider.gameObject;
                        }
                    }
                }
            }
            else
            {
                GUILayout.Label("No colliders found.");
            }
        }
    }

    private class ColliderInfo
    {
        public MagicaCapsuleCollider collider;
        public HumanBodyBones? boneEnum; // Nullable
    }

    private void ScanForColliders()
    {
        collidersInfoList.Clear();

        if (avatar != null)
        {
            Animator animator = avatar.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator component not found on the avatar.");
                return;
            }

            // Build a mapping from bone Transform to HumanBodyBones enum
            Dictionary<Transform, HumanBodyBones> boneTransformMap = new Dictionary<Transform, HumanBodyBones>();

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

            // Find all MagicaCapsuleCollider components under the avatar
            MagicaCapsuleCollider[] colliders = avatar.GetComponentsInChildren<MagicaCapsuleCollider>();

            if (colliders != null && colliders.Length > 0)
            {
                foreach (var collider in colliders)
                {
                    Transform bone = collider.transform.parent; // Assuming the collider is a child of the bone
                    HumanBodyBones? boneEnum = null;

                    if (bone != null && boneTransformMap.ContainsKey(bone))
                    {
                        boneEnum = boneTransformMap[bone];
                    }

                    collidersInfoList.Add(new ColliderInfo
                    {
                        collider = collider,
                        boneEnum = boneEnum
                    });
                }

                // Now sort collidersInfoList according to the specified order
                collidersInfoList.Sort((a, b) =>
                {
                    int indexA = a.boneEnum.HasValue ? boneDisplayOrder.IndexOf(a.boneEnum.Value) : boneDisplayOrder.Count;
                    int indexB = b.boneEnum.HasValue ? boneDisplayOrder.IndexOf(b.boneEnum.Value) : boneDisplayOrder.Count;

                    if (indexA == -1) indexA = boneDisplayOrder.Count;
                    if (indexB == -1) indexB = boneDisplayOrder.Count;

                    return indexA.CompareTo(indexB);
                });
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

        // Bones to exclude
        Transform leftHand = animator.GetBoneTransform(HumanBodyBones.LeftHand);
        Transform rightHand = animator.GetBoneTransform(HumanBodyBones.RightHand);
        // Transform spine = animator.GetBoneTransform(HumanBodyBones.Spine);
        Transform leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
        Transform rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);

        // Collect all bones to exclude
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

        // bonesToExclude.Add(spine);
        bonesToExclude.Add(leftShoulder);
        bonesToExclude.Add(rightShoulder);

        // List to store generated collider objects
        List<GameObject> colliderObjects = new List<GameObject>();

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
            ConfigureCapsuleCollider(colliderObj, bone, childBone, capsuleCollider, boneLength, boneEnum);

            // Add collider object to the list
            colliderObjects.Add(colliderObj);
        }

        // Select all generated collider objects to make them visible
        Selection.objects = colliderObjects.ToArray();

        Debug.Log("Colliders generated successfully.");
    }

    private void ConfigureCapsuleCollider(GameObject colliderObj, Transform bone, Transform childBone, MagicaCapsuleCollider capsuleCollider, float boneLength, HumanBodyBones boneEnum)
    {
        // Determine the direction based on the bone's orientation
        MagicaCapsuleCollider.Direction direction = GetCapsuleDirection(bone, childBone);

        // Set center to zero in all cases
        capsuleCollider.center = Vector3.zero;

        // Adjust direction and rotation
        if (direction == MagicaCapsuleCollider.Direction.Y)
        {
            // Set the collider's direction to X
            capsuleCollider.direction = MagicaCapsuleCollider.Direction.X;
            // Rotate the collider object's local rotation by z=90 degrees
            colliderObj.transform.localRotation = Quaternion.Euler(0, 0, 90);
        }
        else
        {
            // Leave the direction as is for X and Z
            capsuleCollider.direction = MagicaCapsuleCollider.Direction.X;
            // Keep the collider object's rotation as identity
            colliderObj.transform.localRotation = Quaternion.identity;
        }

        // Default values
        float startRadius = boneLength * 0.2f; // Adjust as needed
        float endRadius = startRadius;
        float length = boneLength;

        if (boneEnum == HumanBodyBones.RightUpperArm)
        {
            length = 0.28f;
            startRadius = 0.035f;
            endRadius = 0.03f;

            capsuleCollider.reverseDirection = true;
            capsuleCollider.radiusSeparation = true;
            capsuleCollider.alignedOnCenter = false;
        }
        else if (boneEnum == HumanBodyBones.RightLowerArm)
        {
            length = 0.33f;
            startRadius = 0.03f;
            endRadius = 0.02f;

            capsuleCollider.reverseDirection = true;
            capsuleCollider.radiusSeparation = true;
            capsuleCollider.alignedOnCenter = false;
        }
        else if (boneEnum == HumanBodyBones.LeftUpperArm)
        {
            length = 0.28f;
            startRadius = 0.035f;
            endRadius = 0.03f;

            capsuleCollider.radiusSeparation = true;
            capsuleCollider.alignedOnCenter = false;
        }
        else if (boneEnum == HumanBodyBones.LeftLowerArm)
        {
            length = 0.33f;
            startRadius = 0.03f;
            endRadius = 0.02f;

            capsuleCollider.radiusSeparation = true;
            capsuleCollider.alignedOnCenter = false;
        }
        else if (boneEnum == HumanBodyBones.RightUpperLeg)
        {
            length = 0.4f;
            startRadius = 0.08f;
            endRadius = 0.045f;

            colliderObj.transform.localPosition = new Vector3(0.02f, 0, 0);
            colliderObj.transform.localRotation = Quaternion.Euler(0.05f, 0.5f, 85f);

            capsuleCollider.radiusSeparation = true;
            capsuleCollider.alignedOnCenter = false;
        }
        else if (boneEnum == HumanBodyBones.RightLowerLeg)
        {
            length = 0.39f;
            startRadius = 0.04f;
            endRadius = 0.03f;

            colliderObj.transform.localPosition = new Vector3(0, 0, 0);
            colliderObj.transform.localRotation = Quaternion.Euler(5.0f, -0.3f, 90f);

            capsuleCollider.radiusSeparation = true;
            capsuleCollider.alignedOnCenter = false;
        }
        else if (boneEnum == HumanBodyBones.LeftUpperLeg)
        {
            length = 0.4f;
            startRadius = 0.08f;
            endRadius = 0.045f;

            colliderObj.transform.localPosition = new Vector3(-0.02f, 0, 0);
            colliderObj.transform.localRotation = Quaternion.Euler(-0.05f, 0.5f, 95f);

            capsuleCollider.radiusSeparation = true;
            capsuleCollider.alignedOnCenter = false;
        }
        else if (boneEnum == HumanBodyBones.LeftLowerLeg)
        {
            length = 0.39f;
            startRadius = 0.04f;
            endRadius = 0.03f;

            colliderObj.transform.localPosition = new Vector3(0, 0, 0);
            colliderObj.transform.localRotation = Quaternion.Euler(5.0f, -0.3f, 90f);

            capsuleCollider.radiusSeparation = true;
            capsuleCollider.alignedOnCenter = false;
        }
        else if (boneEnum == HumanBodyBones.Spine)
        {
            length = 0.17f;
            startRadius = 0.04f;
            endRadius = 0.04f;

            colliderObj.transform.localPosition = new Vector3(0, 0, 0);
            colliderObj.transform.localRotation = Quaternion.Euler(0, 0, 0);

            capsuleCollider.radiusSeparation = true;
            capsuleCollider.alignedOnCenter = true;
        }
        else if (boneEnum == HumanBodyBones.Head)
        {
            // Set specific properties for the head
            length = 0.14f;
            startRadius = 0.06f;
            endRadius = 0.04f;

            // Set collider object's local position and rotation
            colliderObj.transform.localPosition = new Vector3(0, 0.06f, 0);
            colliderObj.transform.localRotation = Quaternion.Euler(-25f, 0f, 90f);

            capsuleCollider.radiusSeparation = true;
            capsuleCollider.alignedOnCenter = false;
        }
        else if (boneEnum == HumanBodyBones.Neck)
        {
            // Set specific properties for the neck
            length = 0.08f;
            startRadius = 0.02f;
            endRadius = 0.02f;

            // Set collider object's local rotation
            colliderObj.transform.localRotation = Quaternion.Euler(20f, 0f, -90f);

            capsuleCollider.radiusSeparation = false;
            capsuleCollider.alignedOnCenter = false;
        }
        else if (boneEnum == HumanBodyBones.Hips)
        {
            // Set specific properties for the hips
            length = 0.2f;
            startRadius = 0.04f;
            endRadius = 0.04f;

            // Set collider object's local rotation
            colliderObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            capsuleCollider.radiusSeparation = false;
            capsuleCollider.alignedOnCenter = true; // Set alignedOnCenter to true
        }
        else if (boneEnum == HumanBodyBones.Chest)
        {
            // Set specific properties for the chest
            length = 0.2f;
            startRadius = 0.03f;
            endRadius = 0.03f;

            // Set collider object's local position and rotation
            colliderObj.transform.localPosition = new Vector3(0f, 0.08f, 0f);
            colliderObj.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);

            capsuleCollider.radiusSeparation = false;
            capsuleCollider.alignedOnCenter = true; // Set alignedOnCenter to true
        }
        else
        {
            capsuleCollider.radiusSeparation = false;
            capsuleCollider.alignedOnCenter = false; // default
            capsuleCollider.reverseDirection = false; // default
        }

        // Configure the collider size
        capsuleCollider.SetSize(startRadius, endRadius, length);
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

    private void SelectAllColliders()
    {
        if (avatar != null)
        {
            Animator animator = avatar.GetComponent<Animator>();
            if (animator == null)
            {
                Debug.LogError("Animator component not found on the avatar.");
                return;
            }

            // Find all MagicaCapsuleCollider components under the avatar
            MagicaCapsuleCollider[] colliders = avatar.GetComponentsInChildren<MagicaCapsuleCollider>();

            List<GameObject> colliderObjects = new List<GameObject>();

            if (colliders != null && colliders.Length > 0)
            {
                foreach (var collider in colliders)
                {
                    colliderObjects.Add(collider.gameObject);
                }
            }
            Selection.objects = colliderObjects.ToArray();
        }

        // // List to store generated collider objects
        // List<GameObject> colliderObjects = new List<GameObject>();
        // colliderObjects.Add(colliderObj);
        // Selection.activeGameObject = colliderInfo.collider.gameObject;
        // GameObject colliderObj = new GameObject("Collider_" + bone.name);
        // colliderObjects.Add(colliderObj);
    }

    // Method to delete all colliders
    private void DeleteAllColliders()
    {
        if (avatar == null)
        {
            Debug.LogError("Avatar is not assigned.");
            return;
        }

        // Start undo operation
        Undo.RegisterFullObjectHierarchyUndo(avatar, "Delete MagicaCloth2 Colliders");

        // Find all MagicaCapsuleCollider components under the avatar
        MagicaCapsuleCollider[] colliders = avatar.GetComponentsInChildren<MagicaCapsuleCollider>();

        int colliderCount = colliders.Length;

        if (colliderCount == 0)
        {
            Debug.Log("No colliders found to delete.");
            return;
        }

        // Delete each collider
        foreach (MagicaCapsuleCollider collider in colliders)
        {
            // Destroy the collider's GameObject (assuming it's the one created by this tool)
            if (collider != null)
            {
                // Destroy immediate for editor scripts
                Undo.DestroyObjectImmediate(collider.gameObject);
            }
        }

        Debug.Log($"{colliderCount} colliders deleted successfully.");
    }
}
