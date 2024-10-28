using UnityEngine;
using UnityEditor;
using VRM;

[CustomEditor(typeof(VRMSpringBoneColliderGroup))]
public class VRMSpringBoneColliderGroupEditor : Editor
{
    void OnSceneGUI()
    {
        VRMSpringBoneColliderGroup colliderGroup = (VRMSpringBoneColliderGroup)target;

        if (colliderGroup.Colliders != null)
        {
            foreach (var collider in colliderGroup.Colliders)
            {
                Vector3 worldPos = colliderGroup.transform.TransformPoint(collider.Offset);
                Handles.color = Color.green;
                Handles.SphereHandleCap(0, worldPos, Quaternion.identity, collider.Radius * 2, EventType.Repaint);
            }
        }
    }
}
