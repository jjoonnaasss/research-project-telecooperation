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

// script handling object alignment and setting the floor's height
public class AlignmentManager : MonoBehaviour
{
    [SerializeField] private Transform floor;
    [SerializeField] private Transform[] positionVisualizers;
    [SerializeField] private Transform alignmentTarget;

    [Header("Settings")]
    [SerializeField] private Vector3 alignmentTranslationOffset;
    [SerializeField] private Vector3 alignmentRotationOffset;
    [SerializeField] private float delayForFloorHiding = 5;

    // alignment variables
    private Vector3[] alignmentPositions = new Vector3[3];
    private int alignmentPositionsCollected = 0;
    private bool floorHeightSet = false;
    private bool performObjectAlignment;

    private void Awake()
    {
        ManagerCollection.alignmentManager = this;
    }

    public void Init(bool performObjectAlignment)
    {
        this.performObjectAlignment = performObjectAlignment;

        // destroy alignment target, if no object alignment should be executed
        if (!performObjectAlignment) Destroy(this.alignmentTarget.gameObject, 0);
    }

    // check, if floor height has been set yet
    public bool GetFloorHeightSet()
    {
        return this.floorHeightSet;
    }

    // get floor transform
    public Transform GetFloor()
    {
        return this.floor;
    }

    // get number of alignment positions collected so far
    public int GetAlignmentPositionsCollected()
    {
        return this.alignmentPositionsCollected;
    }

    // move the floor with the tip of the given controller (executed before the floor height is set)
    public void MoveFloorWithControllerTip(bool controller)
    {
        Vector3 tipPosition = ManagerCollection.gameManager.hmd.GetControllerTipPosition(controller);
        if (!Single.IsInfinity(tipPosition.x)) this.floor.position = tipPosition;
    }

    // handle user input for the alignment, with the parameter indicating which controller to use
    public void HandleAlignmentInput(bool controller)
    {
        // make sure that we have a valid tip position
        Vector3 tipPosition = ManagerCollection.gameManager.hmd.GetControllerTipPosition(controller);
        if (Single.IsInfinity(tipPosition.x)) return;

        ManagerCollection.gameManager.UpdateLastInteractionTime();

        // record new alignment position and place a position visualizer
        if (this.alignmentPositionsCollected < 3)
        {
            this.alignmentPositions[this.alignmentPositionsCollected] = tipPosition;
            this.positionVisualizers[this.alignmentPositionsCollected].position = tipPosition;
            this.positionVisualizers[this.alignmentPositionsCollected].gameObject.SetActive(true);
        }
        // execute alignment once enough positions were collected
        else this.DoAlignment();

        this.alignmentPositionsCollected++;

        // update the instructions shown to the user
        if (this.alignmentPositionsCollected < 3) ManagerCollection.statusTextManager.ShowObjectAlignPosition(this.alignmentPositionsCollected);
        else if (this.alignmentPositionsCollected == 3) ManagerCollection.statusTextManager.ShowObjectAlignConfirm();
    }

    // execute the actual alignment
    private void DoAlignment()
    {
        // align target with the recorded alignment positions
        this.alignmentTarget.position = this.alignmentPositions[1] + this.alignmentTranslationOffset;
        this.alignmentTarget.right = -(this.alignmentPositions[0] - this.alignmentPositions[1]).normalized;
        this.alignmentTarget.forward = (this.alignmentPositions[2] - this.alignmentPositions[1]).normalized;
        this.alignmentTarget.Rotate(this.alignmentRotationOffset);

        // hide the position visualizers
        foreach (Transform visualizer in this.positionVisualizers) visualizer.gameObject.SetActive(false);

        // start the obstacle creation/placement mode
        ManagerCollection.obstacleManager.RestartObstaclePlacement();
    }

    // adjust floor height to the tip-position of the given controller and destroy the floor's mesh renderer after the given delay
    public void AdjustFloorHeightToCurrentTip(bool controller)
    {
        // make sure that we have a valid tip position
        Vector3 tipPosition = ManagerCollection.gameManager.hmd.GetControllerTipPosition(controller);
        if (Single.IsInfinity(tipPosition.x)) return;

        ManagerCollection.gameManager.UpdateLastInteractionTime();

        this.floor.position = tipPosition;
        this.floorHeightSet = true;
        this.DestroyFloorVisuals(this.delayForFloorHiding);

        // show instructions for the object alignment to the user
        if (this.performObjectAlignment) ManagerCollection.statusTextManager.ShowObjectAlignPosition(0);
        // start the obstacle creation/placement mode, if no object alignment should be performed
        else ManagerCollection.obstacleManager.RestartObstaclePlacement();
    }

    // apply the given floor height
    public void SetFloorHeightTo(float height)
    {
        Vector3 pos = Vector3.zero;
        pos.y = height;
        this.floor.position = pos;
        this.floorHeightSet = true;
        this.DestroyFloorVisuals(0);

        if (this.performObjectAlignment) ManagerCollection.statusTextManager.ShowObjectAlignPosition(this.alignmentPositionsCollected);
        else ManagerCollection.obstacleManager.RestartObstaclePlacement();
    }

    // destroy the mesh renderer of the floor visualization object
    public void DestroyFloorVisuals(float delay)
    {
        Destroy(this.floor.GetComponent<MeshRenderer>(), delay);
    }
}
