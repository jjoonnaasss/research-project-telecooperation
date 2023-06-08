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

// script controlling the mesh collider for a character's trail renderer
public class TrailMesh : MonoBehaviour
{
    // reference to the rewindable script of the character to which this script's object's mesh collider belongs to
    public Rewindable rewindable;

    private MeshCollider meshCollider;

    private void Start()
    {
        this.meshCollider = this.GetComponent<MeshCollider>();
        this.meshCollider.sharedMesh = new Mesh();
    }

    // apply the given vertices and triangles to the mesh collider
    public void ApplyMeshData(Vector3[] vertices, int[] triangles)
    {
        Mesh mesh = this.meshCollider.sharedMesh;

        // clear existing mesh to remove old data
        mesh.Clear();

        // apply new data to the mesh and insert it into the mesh collider
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        this.meshCollider.sharedMesh = mesh;
    }

    // clear the mesh currently assigned to the mesh collider
    public void ClearMesh()
    {
        this.meshCollider.sharedMesh = new Mesh();
    }
}
