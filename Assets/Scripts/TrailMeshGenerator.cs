/*
Jonas Wombacher - Research Project Telecooperation
Copyright (C) 2023 Jonas Wombacher

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// script creating meshes around trail renderers
public class TrailMeshGenerator : MonoBehaviour
{
    private static TrailPositionVertices[] trailPositionVertices;

    // calculate vertices and triangles for a 3d mesh along the given trail
    public static (Vector3[], int[]) GenerateMeshData(TrailRenderer trail, float sideLength)
    {
        // get the trail's positions
        Vector3[] positions = new Vector3[trail.positionCount];
        trail.GetPositions(positions);

        // simplify trail by removing unnecessary positions
        if (trail.positionCount > 2) positions = TrailMeshGenerator.SimplifyTrailPositions(positions);

        // create structs containing four vertices for each of the positions
        TrailPositionVertices[] structs = TrailMeshGenerator.GetVerticesStructs(positions, sideLength);

        // collect all vertices in an array
        Vector3[] vertices = TrailMeshGenerator.GetVertices(structs);

        // define the triangles based on indices from the vertices array
        int[] triangles = TrailMeshGenerator.GetTriangles(positions.Length);

        // store structs in static field for the OnDrawGizmos-method (debugging purposes)
        TrailMeshGenerator.trailPositionVertices = structs;

        // return the calculated vertices and triangles
        return (vertices, triangles);
    }

    // simplify trail by removing unnecessary positions from straight segments of the trail
    private static Vector3[] SimplifyTrailPositions(Vector3[] positions)
    {
        List<Vector3> simplified = new List<Vector3>(positions);

        int i = 0;
        while (i + 2 < simplified.Count)
        {
            Vector3 forward0 = simplified[i + 1] - simplified[i];
            Vector3 forward1 = simplified[i + 2] - simplified[i + 1];

            // don't keep position, if the direction leading to it and the direction leading away from it are too similar
            if (Vector3.Dot(forward0.normalized, forward1.normalized) > 0.999f) simplified.RemoveAt(i + 1);
            else i++;
        }
        
        return simplified.ToArray();
    }

    // create structs containing four vertices for each of the given positions
    private static TrailPositionVertices[] GetVerticesStructs(Vector3[] positions, float sideLength)
    {
        TrailPositionVertices[] structs = new TrailPositionVertices[positions.Length];

        for (int i = 0; i < positions.Length; i++)
        {
            // as the last position doesn't have a following one to calculate its forward, we reuse the previous one
            if (i + 1 == positions.Length) structs[i] = new TrailPositionVertices(positions[i], positions[i] - positions[i - 1], sideLength);
            // set forward axis of the position to the vector from itself to the following position
            else structs[i] = new TrailPositionVertices(positions[i], positions[i + 1] - positions[i], sideLength);
        }

        return structs;
    }

    // collect all vertices from the given structs in an array
    private static Vector3[] GetVertices(TrailPositionVertices[] structs)
    {
        // we have four vertices per position in the trail
        Vector3[] vertices = new Vector3[structs.Length * 4];

        for (int i = 0; i < structs.Length; i++)
        {
            int offset = i * 4;
            Vector3[] positionVertices = structs[i].GetVerticesAsArray();
            vertices[offset] = positionVertices[0];
            vertices[offset + 1] = positionVertices[1];
            vertices[offset + 2] = positionVertices[2];
            vertices[offset + 3] = positionVertices[3];
        }

        return vertices;
    }

    // define the triangles based on indices from the given vertices array
    private static int[] GetTriangles(int positionCount)
    {
        // we need one rectangle for the "lid" at the start of the trail, four per segment between two positions and one for the "lid" at the end of the trail
        int rectangleCount = 1 + (positionCount - 1) * 4 + 1;
        // we need three vertex-indices per triangle and two triangles per rectangle
        int[] triangles = new int[rectangleCount * 2 * 3];

        // triangles for the "lid" at the start of the trail
        triangles[0] = 1;
        triangles[1] = 0;
        triangles[2] = 3;
        triangles[3] = 1;
        triangles[4] = 3;
        triangles[5] = 2;

        int vertexOffset = 0;
        int triangleOffset = 6;

        // triangles for the segments between positions
        for (int i = 0; i < positionCount - 1; i++)
        {
            // triangles facing up
            triangles[triangleOffset] = vertexOffset;  // vertex 0 of leftTop of the first position
            triangles[triangleOffset + 1] = vertexOffset + 4;  // vertex 0 of leftTop of the second position
            triangles[triangleOffset + 2] = vertexOffset + 7;  // vertex 3 of rightTop of the second position

            triangles[triangleOffset + 3] = vertexOffset;  // vertex 0 of leftTop of the first position
            triangles[triangleOffset + 4] = vertexOffset + 7;  // vertex 3 of rightTop of the second position
            triangles[triangleOffset + 5] = vertexOffset + 3;  // vertex 3 of rightTop of the first position

            triangleOffset += 6;

            // triangles facing to the right
            triangles[triangleOffset] = vertexOffset + 3;  // vertex 3 of rightTop of the first position
            triangles[triangleOffset + 1] = vertexOffset + 7;  // vertex 3 of rightTop of the second position
            triangles[triangleOffset + 2] = vertexOffset + 6;  // vertex 2 of rightBottom of the second position

            triangles[triangleOffset + 3] = vertexOffset + 3;  // vertex 3 of rightTop of the first position
            triangles[triangleOffset + 4] = vertexOffset + 6;  // vertex 2 of rightBottom of the second position
            triangles[triangleOffset + 5] = vertexOffset + 2;  // vertex 2 of rightBottom of the first position

            triangleOffset += 6;

            // triangles facing to the bottom
            triangles[triangleOffset] = vertexOffset + 2;  // vertex 2 of rightBottom of the first position
            triangles[triangleOffset + 1] = vertexOffset + 6;  // vertex 2 of rightBottom of the second position
            triangles[triangleOffset + 2] = vertexOffset + 5;  // vertex 1 of leftBottom of the second position

            triangles[triangleOffset + 3] = vertexOffset + 2;  // vertex 2 of rightBottom of the first position
            triangles[triangleOffset + 4] = vertexOffset + 5;  // vertex 1 of leftBottom of the second position
            triangles[triangleOffset + 5] = vertexOffset + 1;  // vertex 1 of leftBottom of the first position

            triangleOffset += 6;

            // triangles facing to the left
            triangles[triangleOffset] = vertexOffset + 1;  // vertex 1 of leftBottom of the first position
            triangles[triangleOffset + 1] = vertexOffset + 5;  // vertex 1 of leftBottom of the second position
            triangles[triangleOffset + 2] = vertexOffset + 4;  // vertex 0 of leftTop of the second position

            triangles[triangleOffset + 3] = vertexOffset + 1;  // vertex 1 of leftBottom of the first position
            triangles[triangleOffset + 4] = vertexOffset + 4;  // vertex 0 of leftTop of the second position
            triangles[triangleOffset + 5] = vertexOffset + 0;  // vertex 0 of leftTop of the first position

            triangleOffset += 6;

            // increase vertex offset by four to move one position ahead
            vertexOffset += 4;
        }


        // triangles for the "lid" at the end of the trail ([^n] is the C#-equivalent of [-n] in e.g. Python)
        triangles[^6] = vertexOffset + 3;
        triangles[^5] = vertexOffset + 0;
        triangles[^4] = vertexOffset + 1;
        triangles[^3] = vertexOffset + 2;
        triangles[^2] = vertexOffset + 3;
        triangles[^1] = vertexOffset + 1;

        return triangles;
    }

    private void OnDrawGizmos()
    {
        if (TrailMeshGenerator.trailPositionVertices == null) return;

        Gizmos.color = Color.green;
        foreach (TrailPositionVertices tpv in TrailMeshGenerator.trailPositionVertices)
        {
            Gizmos.DrawSphere(tpv.leftTop, 0.025f);
            Gizmos.DrawSphere(tpv.leftBottom, 0.025f);
            Gizmos.DrawSphere(tpv.rightBottom, 0.025f);
            Gizmos.DrawSphere(tpv.rightTop, 0.025f);
        }

    }

    // struct generating and storing four vertices around the given position in the trail
    [System.Serializable]
    private struct TrailPositionVertices
    {
        // the names are chosen from a viewpoint where you look at the trail position from an earlier position in the trail
        public Vector3 leftTop;
        public Vector3 leftBottom;
        public Vector3 rightBottom;
        public Vector3 rightTop;

        public TrailPositionVertices(Vector3 position, Vector3 forward, float sideLength)
        {
            // rotate forward axis 90 degrees around the global up axis to get the position's right axis
            Vector3 right = Quaternion.Euler(0, 90, 0) * forward;

            // calculate vectors for the required offsets from the position as the center of the square
            Vector3 xOffset = right.normalized * (sideLength / 2);
            Vector3 yOffset = Vector3.up * (sideLength / 2);

            // calculate the four vertices
            this.leftTop = position - xOffset + yOffset;
            this.leftBottom = position - xOffset - yOffset;
            this.rightBottom = position + xOffset - yOffset;
            this.rightTop = position + xOffset + yOffset;
        }

        public Vector3[] GetVerticesAsArray()
        {
            return new Vector3[] { this.leftTop, this.leftBottom, this.rightBottom, this.rightTop };
        }
    }
}