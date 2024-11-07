using UnityEditor;
using UnityEngine;
using UnityEngine.Animations; // Required for HumanBodyBones
using UnityEditorInternal; // For ReorderableList
using MagicaCloth2;
using System.Collections.Generic;

public class MagicaColliderGenerator : EditorWindow
{
    private GameObject avatar;
    private GameObject previousAvatar = null;

    private List<ColliderInfo> collidersInfoList = new List<ColliderInfo>();
    private List<HumanBodyBones> boneDisplayOrder = new List<HumanBodyBones>
    {
        HumanBodyBones.Head,
        HumanBodyBones.Neck,
        HumanBodyBones.Chest,
        HumanBodyBones.Spine,
        HumanBodyBones.LeftUpperArm,  // Left arm
        HumanBodyBones.LeftLowerArm,  // Left elbow
        HumanBodyBones.RightUpperArm, // Right arm
        HumanBodyBones.RightLowerArm, // Right elbow
        HumanBodyBones.Hips,
        HumanBodyBones.LeftUpperLeg,  // Left leg
        HumanBodyBones.LeftLowerLeg,  // Left knee
        HumanBodyBones.RightUpperLeg, // Right leg
        HumanBodyBones.RightLowerLeg, // Right knee
    };
    // Order in which to display colliders (saved and loaded from EditorPrefs)
    private List<string> colliderOrder = new List<string>();

    // ReorderableList for colliders
    private ReorderableList collidersReorderableList;

    // Scroll position for the collider list
    private Vector2 scrollPosition = Vector2.zero;

    // For New Collider functionality
    private GameObject newColliderObject;

    // Collider groups and their default colliders
    private Dictionary<string, List<HumanBodyBones>> colliderGroupDefaults = new Dictionary<string, List<HumanBodyBones>>()
    {
        { "Colliders_Hair", new List<HumanBodyBones> { HumanBodyBones.Head, HumanBodyBones.Neck, HumanBodyBones.Chest, HumanBodyBones.Spine, HumanBodyBones.LeftUpperArm, HumanBodyBones.LeftLowerArm, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm } },
        { "Colliders_Skirt", new List<HumanBodyBones> { HumanBodyBones.Hips, HumanBodyBones.LeftUpperLeg, HumanBodyBones.LeftLowerLeg, HumanBodyBones.RightUpperLeg, HumanBodyBones.RightLowerLeg } }
    };

    // Collider group selection states
    private Dictionary<string, bool> colliderGroupSelections = new Dictionary<string, bool>()
    {
        { "Colliders_Hair", false },
        { "Colliders_Skirt", false }
    };

    private int selectedGroupIndex = 0; // For Apply to Collider Group
    private string[] colliderGroupNames;

    [MenuItem("Tools/MagicaColliderGenerator")]
    public static void ShowWindow()
    {
        GetWindow<MagicaColliderGenerator>("MagicaColliderGenerator");
    }

    private void OnEnable()
    {
        LoadColliderOrder();
        // Initialize collider group names
        colliderGroupNames = new string[] { "Colliders_Hair", "Colliders_Skirt" };
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

            if (GUILayout.Button("Deselect All Colliders"))
            {
                ScanForColliders();
                DeselectAllColliders();
            }

            if (GUILayout.Button("Delete All Colliders"))
            {
                DeleteAllColliders();
                ScanForColliders();
            }

            GUILayout.Space(10);

            // New Collider Section
            GUILayout.Label("Create a New Collider:", EditorStyles.boldLabel);
            newColliderObject = (GameObject)EditorGUILayout.ObjectField("Target Object", newColliderObject, typeof(GameObject), true);
            if (GUILayout.Button("Create Collider"))
            {
                CreateNewCollider();
                ScanForColliders();
            }

            GUILayout.Space(10);
            GUILayout.Label("Edit Collider Group:", EditorStyles.boldLabel);

            selectedGroupIndex = EditorGUILayout.Popup("Select Group", selectedGroupIndex, colliderGroupNames);

            if (GUILayout.Button("Apply"))
            {
                string selectedGroupName = colliderGroupNames[selectedGroupIndex];
                ApplyCurrentSelectionToGroup(selectedGroupName);
            }

            GUILayout.Space(10);

            // Collider Groups Checkboxes
            foreach (var groupName in colliderGroupNames)
            {
                bool prevGroupSelected = colliderGroupSelections[groupName];
                colliderGroupSelections[groupName] = EditorGUILayout.ToggleLeft(groupName, colliderGroupSelections[groupName]);
                if (prevGroupSelected != colliderGroupSelections[groupName])
                {
                    ApplyColliderGroupSelection(groupName, colliderGroupSelections[groupName]);
                }
            }

            GUILayout.Space(10);
            GUILayout.Label("Existing Colliders:", EditorStyles.boldLabel);

            if (collidersInfoList != null && collidersInfoList.Count > 0)
            {
                // Draw the reorderable list
                if (collidersReorderableList == null)
                {
                    InitializeReorderableList();
                }

                // Adjust the height of the list dynamically
                float listHeight = position.height - 300;
                if (listHeight < 100) listHeight = 100;

                scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, GUILayout.Height(listHeight));
                collidersReorderableList.DoLayoutList();
                EditorGUILayout.EndScrollView();
            }
            else
            {
                GUILayout.Label("No colliders found.");
            }

            GUILayout.Space(10);
            GUILayout.Label("Collider Groups:", EditorStyles.boldLabel);
        }
    }

    private void InitializeReorderableList()
    {
        collidersReorderableList = new ReorderableList(collidersInfoList, typeof(ColliderInfo), true, false, false, false);

        collidersReorderableList.drawElementCallback = (Rect rect, int index, bool isActive, bool isFocused) =>
        {
            ColliderInfo colliderInfo = collidersInfoList[index];
            if (colliderInfo.collider != null)
            {
                rect.y += 2;
                bool prevSelected = colliderInfo.isSelected;
                colliderInfo.isSelected = EditorGUI.ToggleLeft(new Rect(rect.x, rect.y, rect.width, EditorGUIUtility.singleLineHeight), colliderInfo.collider.name, colliderInfo.isSelected);

                if (prevSelected != colliderInfo.isSelected)
                {
                    UpdateSelection();
                }
            }
        };

        collidersReorderableList.onReorderCallback = (ReorderableList list) =>
        {
            SaveColliderOrder();
        };

        collidersReorderableList.headerHeight = 0;
        collidersReorderableList.footerHeight = 0;
    }

    private class ColliderInfo
    {
        public MagicaCapsuleCollider collider;
        public HumanBodyBones? boneEnum; // Nullable
        public bool isSelected = false; // Selection state in the UI
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
                        boneEnum = boneEnum,
                        isSelected = false
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

                SaveColliderOrder();

                // Restore the saved order
                RestoreColliderOrder();

                // Initialize the reorderable list
                InitializeReorderableList();
            }
        }
    }

    private void UpdateSelection()
    {
        List<GameObject> selectedObjects = new List<GameObject>();
        foreach (var colliderInfo in collidersInfoList)
        {
            if (colliderInfo.isSelected)
            {
                selectedObjects.Add(colliderInfo.collider.gameObject);
            }
        }
        Selection.objects = selectedObjects.ToArray();
    }

    private void ApplyColliderGroupSelection(string groupName, bool isSelected)
    {
        if (colliderGroupDefaults.ContainsKey(groupName))
        {
            var defaultBones = colliderGroupDefaults[groupName];
            foreach (var colliderInfo in collidersInfoList)
            {
                if (colliderInfo.boneEnum.HasValue && defaultBones.Contains(colliderInfo.boneEnum.Value))
                {
                    colliderInfo.isSelected = isSelected;
                }
            }
            UpdateSelection();
        }
    }

    private void ApplyCurrentSelectionToGroup(string groupName)
    {
        List<HumanBodyBones> selectedBones = new List<HumanBodyBones>();
        foreach (var colliderInfo in collidersInfoList)
        {
            if (colliderInfo.isSelected && colliderInfo.boneEnum.HasValue)
            {
                selectedBones.Add(colliderInfo.boneEnum.Value);
            }
        }
        colliderGroupDefaults[groupName] = selectedBones;
        Debug.Log($"Updated defaults for {groupName}.");
    }

    private void SelectAllColliders()
    {
        foreach (var colliderInfo in collidersInfoList)
        {
            colliderInfo.isSelected = true;
        }
        UpdateSelection();
    }

    private void DeselectAllColliders()
    {
        foreach (var colliderInfo in collidersInfoList)
        {
            colliderInfo.isSelected = false;
        }
        UpdateSelection();
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

        bonesToExclude.Add(leftShoulder);
        bonesToExclude.Add(rightShoulder);

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

        }

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

        // Adjust settings based on specific bones (Include your custom configurations here)
        // Example for Head
        if (boneEnum == HumanBodyBones.Head)
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
        else if (boneEnum == HumanBodyBones.RightUpperArm)
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

    private void CreateNewCollider()
    {
        if (newColliderObject == null)
        {
            Debug.LogError("Please assign a target object for the new collider.");
            return;
        }

        // Start undo operation
        Undo.RegisterFullObjectHierarchyUndo(newColliderObject, "Create New MagicaCapsuleCollider");

        // Create a new GameObject for the collider
        GameObject colliderObj = new GameObject("Collider_" + newColliderObject.name);
        colliderObj.transform.SetParent(newColliderObject.transform, false);
        colliderObj.transform.localPosition = Vector3.zero;
        colliderObj.transform.localRotation = Quaternion.identity;

        // Add MagicaCapsuleCollider component
        MagicaCapsuleCollider capsuleCollider = colliderObj.AddComponent<MagicaCapsuleCollider>();

        // Set properties
        capsuleCollider.direction = MagicaCapsuleCollider.Direction.X;
        capsuleCollider.center = Vector3.zero;
        capsuleCollider.SetSize(0.05f, 0.05f, 0.2f);
        capsuleCollider.radiusSeparation = false;
        capsuleCollider.alignedOnCenter = false;

        // Select the new collider
        Selection.activeGameObject = colliderObj;

        Debug.Log($"Created new collider under {newColliderObject.name}.");
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
            if (collider != null)
            {
                // Destroy immediate for editor scripts
                Undo.DestroyObjectImmediate(collider.gameObject);
            }
        }

        Debug.Log($"{colliderCount} colliders deleted successfully.");
    }

    private void SaveColliderOrder()
    {
        colliderOrder.Clear();
        foreach (var colliderInfo in collidersInfoList)
        {
            colliderOrder.Add(colliderInfo.collider.name);
        }

        string serializedOrder = string.Join(";", colliderOrder);
        EditorPrefs.SetString("MagicaColliderOrder", serializedOrder);
    }

    private void LoadColliderOrder()
    {
        colliderOrder.Clear();
        string serializedOrder = EditorPrefs.GetString("MagicaColliderOrder", "");
        if (!string.IsNullOrEmpty(serializedOrder))
        {
            colliderOrder.AddRange(serializedOrder.Split(';'));
        }
    }

    private void RestoreColliderOrder()
    {
        if (colliderOrder.Count == 0)
            return;

        // Create a mapping from collider name to ColliderInfo
        Dictionary<string, ColliderInfo> colliderInfoMap = new Dictionary<string, ColliderInfo>();
        foreach (var colliderInfo in collidersInfoList)
        {
            colliderInfoMap[colliderInfo.collider.name] = colliderInfo;
        }

        // Reorder collidersInfoList based on saved order
        List<ColliderInfo> reorderedList = new List<ColliderInfo>();
        foreach (var colliderName in colliderOrder)
        {
            if (colliderInfoMap.ContainsKey(colliderName))
            {
                reorderedList.Add(colliderInfoMap[colliderName]);
                colliderInfoMap.Remove(colliderName);
            }
        }

        // Add any colliders that were not in the saved order
        foreach (var colliderInfo in colliderInfoMap.Values)
        {
            reorderedList.Add(colliderInfo);
        }

        collidersInfoList = reorderedList;
    }
}
