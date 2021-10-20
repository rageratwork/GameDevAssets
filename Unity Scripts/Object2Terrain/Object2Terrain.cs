using System.IO;
using UnityEngine;
using UnityEditor;

// Ported to c# from the original Unity Script found here:
// https://gist.github.com/jasonsturges/1f1c8df23aad35aa8236
// 
// More information can be found here:
// https://jasonsturges.medium.com/unity-3d-terrain-from-blender-ant-landscape-generator-7ca886b7999d

// This script is released to the public domain so have fun with it.
public class Object2Terrain
{
    [MenuItem("Terrain/Object to Terrain", false, 1)]
    static void doObject2Terrain() {
        GameObject obj = Selection.activeObject as GameObject;
        if(obj == null) {
            EditorUtility.DisplayDialog("No object selected", "Please select an object.", "Cancel");
            return;
        }

        if(obj.GetComponent<MeshFilter>() == null) {
            EditorUtility.DisplayDialog("No mesh selected", "Please select an object with a mesh.", "Cancel");
            return;
        } else if (obj.GetComponent<MeshFilter>().sharedMesh == null) {
            EditorUtility.DisplayDialog("No mesh selected", "Please select an object with a valid mesh.", "Cancel");
            return;
        }

        if(Terrain.activeTerrain == null) {
            EditorUtility.DisplayDialog("No terrain found", "Please make sure a terrain exists.", "Cancel");
            return;
        }

        TerrainData terrainData = Terrain.activeTerrain.terrainData;

        // If there's no mesh collider, add one (and then remove it later when done)
        bool addedCollider = false;
        bool addedMesh = false;
        MeshCollider objCollider = obj.GetComponent<Collider>() as MeshCollider;

        if(objCollider == null) {
            objCollider = obj.AddComponent<MeshCollider>();
            addedCollider = true;
        } else if(objCollider.sharedMesh == null) {
            objCollider.sharedMesh = obj.GetComponent<MeshFilter>().sharedMesh;
            addedMesh = true;
        }

        Undo.RegisterCompleteObjectUndo(terrainData, "Object to Terrain");

        int resolutionX = terrainData.heightmapResolution;
        int resolutionZ = terrainData.heightmapResolution;
        float[,] heights = terrainData.GetHeights(0, 0, resolutionX, resolutionZ);

        // Use bounds a bit smaller than the actual object; otherwise raycasting tends to miss at the edges
        Bounds objectBounds = objCollider.bounds;
        float leftEdge = objectBounds.center.x - objectBounds.extents.x + .01f;
        float bottomEdge = objectBounds.center.z - objectBounds.extents.z + .01f;
        float stepX = (objectBounds.size.x - .019f) / resolutionX;
        float stepZ = (objectBounds.size.z - .019f) / resolutionZ;

        // Set up raycast vars
        float y = objectBounds.center.y + objectBounds.extents.y + .01f;

        RaycastHit hit;
        Ray ray = new Ray(Vector3.zero, -Vector3.up);
        float rayDistance = objectBounds.size.y + .02f;

        float heightFactor = 1.0f / rayDistance;
        float scaleFactor = terrainData.heightmapScale.y / objectBounds.size.y;

        // Do raycasting samples over the object to see what terrain heights should be
        float z = bottomEdge;
        for(int zCount = 0; zCount < resolutionZ; zCount++) {
            float x = leftEdge;

            for(int xCount = 0; xCount < resolutionX; xCount++) {
                ray.origin = new Vector3(x, y, z);

                if(objCollider.Raycast(ray, out hit, rayDistance)) {
                    heights[zCount, xCount] = (1.0f - (y - hit.point.y) * heightFactor) / scaleFactor;
                } else {
                    heights[zCount, xCount] = 0.0f;
                }

                x += stepX;
            }

            z += stepZ;
        }

        terrainData.SetHeights(0, 0, heights);

        if (addedMesh) {
		    objCollider.sharedMesh = null;
	    }

        if(addedCollider) {
            Object.DestroyImmediate(objCollider);
        }
    }
}
