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

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// script for copying the meshes of a source object (character) to a clone object
public class MeshClone : MonoBehaviour
{
    private enum MeshCloneType {humanoid, sphere};

    [SerializeField] private MeshCloneType cloneType;

    private MeshRenderer[] cloneMeshRenderers;
    private Color surfaceColor;

    // copy the meshes of the given source object to the clone object
    public void CopyMeshes(GameObject source)
    {
        if (this.cloneType == MeshCloneType.humanoid) this.CopyMeshesHumanoid(source);
        else this.CopyMeshesSphere(source);
    }

    // apply the given opacity to this clone's meshes
    public void ApplyOpacity(float opacity, float opaqueThreshold, Material defaultMaterial, Material transparentMaterial)
    {
        if (this.cloneType == MeshCloneType.humanoid) this.ApplyOpacityHumanoid(opacity, opaqueThreshold, defaultMaterial, transparentMaterial);
        else this.ApplyOpacitySphere(opacity, opaqueThreshold, defaultMaterial, transparentMaterial);
    }

    // copy the meshes of the given source object to the clone object (humanoid visualization)
    private void CopyMeshesHumanoid(GameObject source)
    {
        // get references to the clone's mesh filters and renderers
        MeshFilter[] cloneMeshFilters = this.GetComponentsInChildren<MeshFilter>();
        this.cloneMeshRenderers = this.GetComponentsInChildren<MeshRenderer>();

        // get references to the source's skinned mesh renderers
        SkinnedMeshRenderer[] sourceSkinnedMeshRenderers = source.GetComponentsInChildren<SkinnedMeshRenderer>(true);

        // bake the surface mesh
        Mesh surfaceMesh = new Mesh();
        sourceSkinnedMeshRenderers[0].BakeMesh(surfaceMesh);

        // bake the joints mesh
        Mesh jointsMesh = new Mesh();
        sourceSkinnedMeshRenderers[1].BakeMesh(jointsMesh);

        // copy surface material to the baked surface mesh renderer
        Material surfaceMaterial = new Material(sourceSkinnedMeshRenderers[0].material);
        this.cloneMeshRenderers[0].material = surfaceMaterial;
        this.surfaceColor = surfaceMaterial.color;

        // copy joints material to the baked joints mesh renderer
        Material jointsMaterial = new Material(sourceSkinnedMeshRenderers[1].material);
        this.cloneMeshRenderers[1].material = jointsMaterial;

        // apply the baked meshes to the ghost's mesh filters
        cloneMeshFilters[0].mesh = surfaceMesh;
        cloneMeshFilters[1].mesh = jointsMesh;
    }

    // copy the meshes of the given source object to the clone object (sphere visualization)
    private void CopyMeshesSphere(GameObject source)
    {
        // get reference to the clone's mesh renderer
        this.cloneMeshRenderers = this.GetComponentsInChildren<MeshRenderer>();

        // get reference to the source's mesh renderer
        MeshRenderer sourceMeshRenderer = source.GetComponentInChildren<MeshRenderer>(true);

        // apply scale of the source to the ghost
        this.transform.localScale = source.transform.localScale;

        // copy source material to the clone mesh renderer
        Material material = new Material(sourceMeshRenderer.material);
        this.cloneMeshRenderers[0].material = material;
        this.surfaceColor = material.color;
    }

    // apply the given opacity to this clone's meshes (humanoid visualization)
    private void ApplyOpacityHumanoid(float opacity, float opaqueThreshold, Material defaultMaterial, Material transparentMaterial)
    {
        // use opaque surface material for opacities close to 100%, otherwise use transparent material
        if (opacity >= opaqueThreshold) this.cloneMeshRenderers[0].material = defaultMaterial;
        else this.cloneMeshRenderers[0].material = transparentMaterial;

        // apply opacity to the surface mesh renderer
        this.surfaceColor.a = opacity;
        this.cloneMeshRenderers[0].material.color = this.surfaceColor;

        // apply opacity to the joints mesh renderer
        Color color = this.cloneMeshRenderers[1].material.color;
        color.a = opacity;
        this.cloneMeshRenderers[1].material.color = color;
    }

    // apply the given opacity to this clone's meshes (sphere visualization)
    private void ApplyOpacitySphere(float opacity, float opaqueThreshold, Material defaultMaterial, Material transparentMaterial)
    {
        // use opaque material for opacities close to 100%, otherwise use transparent material
        if (opacity >= opaqueThreshold) this.cloneMeshRenderers[0].material = defaultMaterial;
        else this.cloneMeshRenderers[0].material = transparentMaterial;

        // apply opacity to the mesh renderer of the sphere ghost
        this.surfaceColor.a = opacity;
        this.cloneMeshRenderers[0].material.color = this.surfaceColor;
    }

}
