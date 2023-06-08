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

// script managing the fires
public class FireManager : MonoBehaviour
{
    [SerializeField] private Transform fireParent;
    [SerializeField] private GameObject firePrefab;
    [SerializeField] private LayerMask obstacleTopLayer;
        
    // fire manager variables
    private List<GameObject> fires;
    private List<Vector3> firePositions;

    private void Awake()
    {
        ManagerCollection.fireManager = this;

        this.fires = new List<GameObject>();
        this.firePositions = new List<Vector3>();
    }

    // create a new fire at the position of the given controller's ray pointer
    public void SpawnFire(bool pointerController, LayerMask fireSpawnLayers)
    {
        // make sure that we have a valid floor pointer position
        RaycastHit? floorPointerHit = ManagerCollection.gameManager.hmd.GetFloorPointerHitController(pointerController, fireSpawnLayers);
        if (floorPointerHit == null) return;

        ManagerCollection.gameManager.UpdateLastInteractionTime();

        Vector3 floorPointerPos = ((RaycastHit)floorPointerHit).point;

        // spawn fire on the floor, if it did not hit the top of an obstacle
        if (!this.IsInLayerMask(((RaycastHit)floorPointerHit).collider.gameObject.layer, this.obstacleTopLayer)) floorPointerPos.y = ManagerCollection.alignmentManager.GetFloor().position.y;
        
        // instantiate the fire object
        GameObject fire = Instantiate(this.firePrefab, floorPointerPos, Quaternion.identity, this.fireParent);

        // let fire face the camera/user
        Vector3 forward = Camera.main.transform.position - floorPointerPos;
        forward.y = 0;
        fire.transform.forward = forward.normalized;

        // store new fire
        this.fires.Add(fire);
        this.firePositions.Add(floorPointerPos);
    }

    // get all fire positions
    public Vector3[] GetFirePositions()
    {
        return this.firePositions.ToArray();
    }

    // delete all fires
    public void RemoveAllFires()
    {
        foreach (GameObject fire in this.fires) Destroy(fire);
        this.fires.Clear();
        this.firePositions.Clear();
    }

    // helper function for checking, whether a given layer is included in the given layer mask
    private bool IsInLayerMask(int layer, LayerMask layerMask)
    {
        return ((1 << layer) & layerMask) != 0;
    }
}
