using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Obi
{
    public class ObiBlueprintRenderModeVoxels : ObiBlueprintRenderMode
    {
        public override string name
        {
            get { return "Voxels"; }
        }

        Mesh mesh;
        List<Vector3> vertices;
        List<int> tris;
        List<Vector2> uv;
        Material voxelMaterial;

        public ObiBlueprintRenderModeVoxels(ObiSoftbodySurfaceBlueprintEditor editor) : base(editor)
        {
            mesh = new Mesh();
            vertices = new List<Vector3>();
            tris = new List<int>();
            uv = new List<Vector2>();

            voxelMaterial = Resources.Load<Material>("VoxelMaterial");
        }

        public override void OnSceneRepaint(SceneView sceneView)
        {
            var voxelizer = ((ObiSoftbodySurfaceBlueprint)editor.blueprint).shapeVoxelizer;

            if (voxelizer != null)
            {
                if (mesh == null) mesh = new Mesh();

                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
                mesh.Clear();
                vertices.Clear();
                tris.Clear();
                uv.Clear();

                for (int x = 0; x < voxelizer.resolution.x; ++x)
                    for (int y = 0; y < voxelizer.resolution.y; ++y)
                        for (int z = 0; z < voxelizer.resolution.z; ++z)
                            if (voxelizer[x, y, z] == MeshVoxelizer.Voxel.Boundary)
                            {
                                // face:
                                if (!voxelizer.VoxelExists(x, y, z - 1) || voxelizer[x, y, z - 1] == MeshVoxelizer.Voxel.Outside)
                                {
                                    tris.Add(vertices.Count + 0);
                                    tris.Add(vertices.Count + 2);
                                    tris.Add(vertices.Count + 1);

                                    tris.Add(vertices.Count + 1);
                                    tris.Add(vertices.Count + 2);
                                    tris.Add(vertices.Count + 3);

                                    vertices.Add(new Vector3(voxelizer.Origin.x + x, voxelizer.Origin.y + y, voxelizer.Origin.z + z) * voxelizer.voxelSize);
                                    vertices.Add(new Vector3(voxelizer.Origin.x + x + 1, voxelizer.Origin.y + y, voxelizer.Origin.z + z) * voxelizer.voxelSize);
                                    vertices.Add(new Vector3(voxelizer.Origin.x + x, voxelizer.Origin.y + y + 1, voxelizer.Origin.z + z) * voxelizer.voxelSize);
                                    vertices.Add(new Vector3(voxelizer.Origin.x + x + 1, voxelizer.Origin.y + y + 1, voxelizer.Origin.z + z) * voxelizer.voxelSize);

                                    uv.Add(new Vector2(0, 0));
                                    uv.Add(new Vector2(1, 0));
                                    uv.Add(new Vector2(0, 1));
                                    uv.Add(new Vector2(1, 1));
                                }

                                // face:
                                if (!voxelizer.VoxelExists(x, y, z + 1) || voxelizer[x, y, z + 1] == MeshVoxelizer.Voxel.Outside)
                                {
                                    tris.Add(vertices.Count + 0);
                                    tris.Add(vertices.Count + 2);
                                    tris.Add(vertices.Count + 1);

                                    tris.Add(vertices.Count + 1);
                                    tris.Add(vertices.Count + 2);
                                    tris.Add(vertices.Count + 3);

                                    vertices.Add(new Vector3(voxelizer.Origin.x + x, voxelizer.Origin.y + y, voxelizer.Origin.z + z + 1) * voxelizer.voxelSize);
                                    vertices.Add(new Vector3(voxelizer.Origin.x + x, voxelizer.Origin.y + y + 1, voxelizer.Origin.z + z + 1) * voxelizer.voxelSize);
                                    vertices.Add(new Vector3(voxelizer.Origin.x + x + 1, voxelizer.Origin.y + y, voxelizer.Origin.z + z + 1) * voxelizer.voxelSize);
                                    vertices.Add(new Vector3(voxelizer.Origin.x + x + 1, voxelizer.Origin.y + y + 1, voxelizer.Origin.z + z + 1) * voxelizer.voxelSize);

                                    uv.Add(new Vector2(0, 0));
                                    uv.Add(new Vector2(1, 0));
                                    uv.Add(new Vector2(0, 1));
                                    uv.Add(new Vector2(1, 1));
                                }

                                // face:
                                if (!voxelizer.VoxelExists(x - 1, y, z) || voxelizer[x - 1, y, z] == MeshVoxelizer.Voxel.Outside)
                                {
                                    tris.Add(vertices.Count + 0);
                                    tris.Add(vertices.Count + 2);
                                    tris.Add(vertices.Count + 1);

                                    tris.Add(vertices.Count + 1);
                                    tris.Add(vertices.Count + 2);
                                    tris.Add(vertices.Count + 3);

                                    vertices.Add(new Vector3(voxelizer.Origin.x + x, voxelizer.Origin.y + y, voxelizer.Origin.z + z) * voxelizer.voxelSize);
                                    vertices.Add(new Vector3(voxelizer.Origin.x + x, voxelizer.Origin.y + y + 1, voxelizer.Origin.z + z) * voxelizer.voxelSize);
                                    vertices.Add(new Vector3(voxelizer.Origin.x + x, voxelizer.Origin.y + y, voxelizer.Origin.z + z + 1) * voxelizer.voxelSize);
                                    vertices.Add(new Vector3(voxelizer.Origin.x + x, voxelizer.Origin.y + y + 1, voxelizer.Origin.z + z + 1) * voxelizer.voxelSize);

                                    uv.Add(new Vector2(0, 0));
                                    uv.Add(new Vector2(1, 0));
                                    uv.Add(new Vector2(0, 1));
                                    uv.Add(new Vector2(1, 1));
                                }

                                // face:
                                if (!voxelizer.VoxelExists(x + 1, y, z) || voxelizer[x + 1, y, z] == MeshVoxelizer.Voxel.Outside)
                                {
                                    tris.Add(vertices.Count + 0);
                                    tris.Add(vertices.Count + 2);
                                    tris.Add(vertices.Count + 1);

                                    tris.Add(vertices.Count + 1);
                                    tris.Add(vertices.Count + 2);
                                    tris.Add(vertices.Count + 3);

                                    vertices.Add(new Vector3(voxelizer.Origin.x + x + 1, voxelizer.Origin.y + y, voxelizer.Origin.z + z) * voxelizer.voxelSize);
                                    vertices.Add(new Vector3(voxelizer.Origin.x + x + 1, voxelizer.Origin.y + y, voxelizer.Origin.z + z + 1) * voxelizer.voxelSize);
                                    vertices.Add(new Vector3(voxelizer.Origin.x + x + 1, voxelizer.Origin.y + y + 1, voxelizer.Origin.z + z) * voxelizer.voxelSize);
                                    vertices.Add(new Vector3(voxelizer.Origin.x + x + 1, voxelizer.Origin.y + y + 1, voxelizer.Origin.z + z + 1) * voxelizer.voxelSize);

                                    uv.Add(new Vector2(0, 0));
                                    uv.Add(new Vector2(1, 0));
                                    uv.Add(new Vector2(0, 1));
                                    uv.Add(new Vector2(1, 1));
                                }

                                // face:
                                if (!voxelizer.VoxelExists(x, y - 1, z) || voxelizer[x, y - 1, z] == MeshVoxelizer.Voxel.Outside)
                                {
                                    tris.Add(vertices.Count + 0);
                                    tris.Add(vertices.Count + 2);
                                    tris.Add(vertices.Count + 1);

                                    tris.Add(vertices.Count + 1);
                                    tris.Add(vertices.Count + 2);
                                    tris.Add(vertices.Count + 3);

                                    vertices.Add(new Vector3(voxelizer.Origin.x + x, voxelizer.Origin.y + y, voxelizer.Origin.z + z) * voxelizer.voxelSize);
                                    vertices.Add(new Vector3(voxelizer.Origin.x + x, voxelizer.Origin.y + y, voxelizer.Origin.z + z + 1) * voxelizer.voxelSize);
                                    vertices.Add(new Vector3(voxelizer.Origin.x + x + 1, voxelizer.Origin.y + y, voxelizer.Origin.z + z) * voxelizer.voxelSize);
                                    vertices.Add(new Vector3(voxelizer.Origin.x + x + 1, voxelizer.Origin.y + y, voxelizer.Origin.z + z + 1) * voxelizer.voxelSize);

                                    uv.Add(new Vector2(0, 0));
                                    uv.Add(new Vector2(1, 0));
                                    uv.Add(new Vector2(0, 1));
                                    uv.Add(new Vector2(1, 1));
                                }

                                // face:
                                if (!voxelizer.VoxelExists(x, y + 1, z) || voxelizer[x, y + 1, z] == MeshVoxelizer.Voxel.Outside)
                                {
                                    tris.Add(vertices.Count + 0);
                                    tris.Add(vertices.Count + 2);
                                    tris.Add(vertices.Count + 1);

                                    tris.Add(vertices.Count + 1);
                                    tris.Add(vertices.Count + 2);
                                    tris.Add(vertices.Count + 3);

                                    vertices.Add(new Vector3(voxelizer.Origin.x + x, voxelizer.Origin.y + y + 1, voxelizer.Origin.z + z) * voxelizer.voxelSize);
                                    vertices.Add(new Vector3(voxelizer.Origin.x + x + 1, voxelizer.Origin.y + y + 1, voxelizer.Origin.z + z) * voxelizer.voxelSize);
                                    vertices.Add(new Vector3(voxelizer.Origin.x + x, voxelizer.Origin.y + y + 1, voxelizer.Origin.z + z + 1) * voxelizer.voxelSize);
                                    vertices.Add(new Vector3(voxelizer.Origin.x + x + 1, voxelizer.Origin.y + y + 1, voxelizer.Origin.z + z + 1) * voxelizer.voxelSize);

                                    uv.Add(new Vector2(0, 0));
                                    uv.Add(new Vector2(1, 0));
                                    uv.Add(new Vector2(0, 1));
                                    uv.Add(new Vector2(1, 1));
                                }
                            }

                mesh.SetVertices(vertices);
                mesh.SetUVs(0, uv);
                mesh.SetIndices(tris, MeshTopology.Triangles, 0);

                if (voxelMaterial.SetPass(0))
                    Graphics.DrawMeshNow(mesh, Matrix4x4.identity);

            }
        }

    }
}