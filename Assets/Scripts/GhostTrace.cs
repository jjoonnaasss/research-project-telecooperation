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

// script managing the ghost trace of the character it is attached to
public class GhostTrace : MonoBehaviour
{
    [SerializeField] private float ghostSpawnDistance = 0.75f;
    [SerializeField] private MeshClone ghostPrefab;
    [SerializeField] private Transform ghostParent;
    [SerializeField] private float ghostLifeDuration = 10;
    [SerializeField] private float ghostOpacity = 0.5f;
    [SerializeField] private float triggerFullOpacityThreshold = 1;
    [SerializeField] private Material bodyMaterialDefault;
    [SerializeField] private Material bodyMaterialTransparent;
    [SerializeField] private bool active;
    [SerializeField] private Rewindable rewindable;

    private float distanceTraveled;
    private Vector3 lastPosition;
    private List<GhostContainer> ghosts;
    private Queue<MeshClone> ghostPool;
    private bool destroying = false;
    private Color color;

    private void Start()
    {
        this.ghosts = new List<GhostContainer>();
        this.ghostPool = new Queue<MeshClone>();
        this.lastPosition = this.transform.position;
        this.distanceTraveled = 0;

        if (!this.ghostParent) this.SearchParent();
    }

    private void Update()
    {
        // don't create new ghosts while the destruction of all ghosts is in progress
        if (this.destroying) return;

        // update the distance travelled by the character this script belongs to
        this.distanceTraveled += Vector3.Distance(this.transform.position, this.lastPosition);
        this.lastPosition = this.transform.position;

        // spawn new ghost, if the character has moved far enough since the last ghost
        if (this.active && this.rewindable.GetRecording() && this.distanceTraveled >= this.ghostSpawnDistance)
        {
            this.distanceTraveled = 0;
            this.SpawnGhost();
        }

        List<GhostContainer> ghostsToRemove = new List<GhostContainer>();

        // determine which ghosts have to be destroyed, because they have exhausted their time to live
        foreach (GhostContainer ghostContainer in this.ghosts)
        {
            ghostContainer.life -= Time.deltaTime;
            if (ghostContainer.life <= 0) ghostsToRemove.Add(ghostContainer);
        }

        // destroy the determined ghosts and release their game objects back into the pool
        foreach (GhostContainer ghostContainer in ghostsToRemove)
        {
            this.ghosts.Remove(ghostContainer);
            ghostContainer.ghost.gameObject.SetActive(false);
            this.ghostPool.Enqueue(ghostContainer.ghost);
        }
    }

    // create a new ghost at the current position of the character this script belongs to
    private void SpawnGhost()
    {
        MeshClone ghost;

        // take an existing game object out of the pool or create a new one, if the pool is empty
        if (this.ghostPool.Count > 0)
        {
            ghost = this.ghostPool.Dequeue();
            ghost.gameObject.SetActive(true);
        }
        else ghost = Instantiate(this.ghostPrefab, this.ghostParent);

        // store ghost in the list
        this.ghosts.Add(new GhostContainer(ghost, this.ghostLifeDuration, this.rewindable.GetStateCount()));

        // apply position and rotation of the character to the new ghost
        ghost.transform.localPosition = this.transform.localPosition;
        ghost.transform.localRotation = this.transform.localRotation;

        // copy meshes from the character to the new ghost, so that the ghost takes on the current appearance (including posture) of the character
        ghost.CopyMeshes(this.gameObject);

        // fade all ghosts depending on how far away from the character they are
        this.ApplyFadingOpacities();
    }

    // fade all ghosts depending on how far away from the character they are
    private void ApplyFadingOpacities()
    {
        int ghostCount = this.ghosts.Count;
        float maxOpacity = this.ghostOpacity;

        // loop over all ghosts, the first ghost in the list is farthest away from the character
        for (int i = 0; i < ghostCount; i++)
        {
            float fraction = (float) i / (float) (ghostCount - 1);

            // when there is only one ghost, the fraction is NaN, set it to one instead
            if (float.IsNaN(fraction)) fraction = 1;
            
            // apply the opacity determined by the calculated fraction to the according ghost
            this.ghosts[i].ghost.ApplyOpacity(fraction * maxOpacity, this.triggerFullOpacityThreshold, this.bodyMaterialDefault, this.bodyMaterialTransparent);
        }
    }

    // search the GhostParent game object by its name and store it
    private void SearchParent()
    {
        GameObject parent = GameObject.Find("GhostParent");
        if (parent) this.ghostParent = parent.transform;
    }
    
    // (de-)activate this ghost trace
    public void SetActive(bool active)
    {
        this.active = active;
    }

    // set the time to live for new ghosts
    public void SetLength(float length)
    {
        this.ghostLifeDuration = length;
    }

    // set the max ghost opacity/min ghost transparency
    public void SetTransparency(float transparency)
    {
        this.ghostOpacity = 1 - transparency;
    }

    // set the default color for new ghosts
    public void SetColor(Color color)
    {
        this.color = color;
    }

    // delete all ghosts
    public void DestroyAllGhosts()
    {
        // set flag to prevent spawning new ghosts while deleting
        this.destroying = true;

        // destroy all ghost game objects and clear the list
        foreach (GhostContainer ghostContainer in this.ghosts) Destroy(ghostContainer.ghost.gameObject);
        this.ghosts.RemoveAll(ghost => true);

        // unset the flag
        this.destroying = false;
    }

    // only show ghosts, that have been spawned before the rewind state with the given index was saved
    public void ApplyRewindStateIndex(int index)
    {
        foreach (GhostContainer ghostContainer in this.ghosts) ghostContainer.ghost.gameObject.SetActive(index >= ghostContainer.rewindStateCount);
    }
}

// class wrapping a MeshClone and storing the ghosts current lifetime along with the number of rewind states at the moment of spawning
public class GhostContainer
{
    public MeshClone ghost;
    public float life = 2;
    public float fade = 1;
    public int rewindStateCount;

    public GhostContainer(MeshClone ghost, float life, int rewindStateCount)
    {
        this.ghost = ghost;
        this.life = life;
        this.rewindStateCount = rewindStateCount;
    }
}