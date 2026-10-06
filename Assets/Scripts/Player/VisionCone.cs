using UnityEngine;

// Put on a child of the Cop (no local rotation) with a MeshRenderer using a VisionReveal material.
[RequireComponent(typeof(MeshFilter))]
public class VisionCone : MonoBehaviour
{
    [SerializeField] float viewDistance = 6f;
    [SerializeField] float viewAngle = 90f;
    [SerializeField] int segments = 24;
    [SerializeField] LayerMask obstacleMask;
    [SerializeField] float rayHeight = 1f;       // cast above the floor
    [SerializeField] float groundOffset = 0.05f; // avoid z-fighting with floor

    Mesh mesh;
    Vector3[] verts;

    void Awake()
    {
        mesh = new Mesh { name = "VisionCone" };
        mesh.MarkDynamic();
        GetComponent<MeshFilter>().mesh = mesh;

        verts = new Vector3[segments + 2];
        int[] tris = new int[segments * 3];
        for (int i = 0; i < segments; i++)
        {
            tris[i * 3] = 0;
            tris[i * 3 + 1] = i + 1;
            tris[i * 3 + 2] = i + 2;
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
    }

    void LateUpdate()
    {
        // Not visible anyway when the vision is fully closed
        if (DeityVision.Instance && DeityVision.Instance.Radius <= 0f) return;

        Vector3 origin = transform.position;
        Vector3 rayOrigin = origin + Vector3.up * rayHeight;
        float half = viewAngle * 0.5f;

        verts[0] = Vector3.up * groundOffset;

        for (int i = 0; i <= segments; i++)
        {
            float angle = -half + viewAngle * i / segments;
            Vector3 dir = Quaternion.AngleAxis(angle, Vector3.up) * transform.right;

            float dist = Physics.Raycast(rayOrigin, dir, out RaycastHit hit, viewDistance, obstacleMask)
                ? hit.distance
                : viewDistance;

            verts[i + 1] = transform.InverseTransformPoint(origin + dir * dist) + Vector3.up * groundOffset;
        }

        mesh.vertices = verts;
        mesh.RecalculateBounds();
    }
}