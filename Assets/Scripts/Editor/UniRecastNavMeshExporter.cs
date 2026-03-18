using UnityEditor;
using UnityEngine;
using System.IO;
using DotRecast.Core;
using DotRecast.Detour;
using DotRecast.Detour.Io;
using UniRecast.Core;

public static class UniRecastNavMeshExporter
{
    [MenuItem("Tools/UniRecast/Export NavMesh")]
    public static void Export()
    {
        var surface = Object.FindObjectOfType<UniRcNavMeshSurface>();
        if (surface == null)
        {
            EditorUtility.DisplayDialog(
                "UniRecast",
                "UniRcNavMeshSurface not found in scene",
                "OK"
            );
            return;
        }

        if (!surface.HasNavMeshData())
        {
            EditorUtility.DisplayDialog(
                "UniRecast",
                "NavMesh is not baked",
                "OK"
            );
            return;
        }

        DtNavMesh navMesh = surface.GetNavMeshData();

        var path = EditorUtility.SaveFilePanel(
            "Export NavMesh",
            Application.dataPath,
            "navmesh",
            "bin"
        );

        if (string.IsNullOrEmpty(path))
            return;

        using var fs = new FileStream(path, FileMode.Create, FileAccess.Write);
        using var bw = new BinaryWriter(fs);

        var writer = new DtMeshSetWriter();
        writer.Write(bw, navMesh, RcByteOrder.LITTLE_ENDIAN, true);

        Debug.Log($"NavMesh exported: {path}");
    }
}