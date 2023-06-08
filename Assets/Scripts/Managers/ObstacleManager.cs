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

// script managing all obstacles and arcades, including their creation and destruction
public class ObstacleManager : MonoBehaviour
{
    [SerializeField] private LayerMask obstacleLayer;
    [SerializeField] private Transform obstacleHeightCube;
    [SerializeField] private LayerMask obstacleHeightCubeLayer;
    [SerializeField] private Transform obstacleParent;
    [SerializeField] private Transform[] positionVisualizers;

    [Header("Prefabs")]
    [SerializeField] private GameObject obstaclePrefab;
    [SerializeField] private GameObject arcadePrefab;

    [Header("Settings")]
    [SerializeField] private float obstacleTranslationSpeed = 1;
    [SerializeField] private float obstacleRotationSpeed = 100;
    [SerializeField] private float obstacleDeleteDestroyDelay = 2;
    [SerializeField] private Material obstacleMaterialDefault;
    [SerializeField] private Material obstacleMaterialEditMode;
    [SerializeField] private Material obstacleMaterialDeleted;
    [SerializeField] private Material obstacleMaterialMask;

    // obstacle variables
    private Vector3[] obstaclePositions = new Vector3[3];
    private int obstaclePositionsCollected = 0;
    private List<GameObject> obstacles = new List<GameObject>();
    private Transform obstacleToEdit;
    private List<GameObject> arcades = new List<GameObject>();

    private void Awake()
    {
        ManagerCollection.obstacleManager = this;
    }

    // check, whether an obstacle is currently being scaled
    public bool GetHeightAdjustmentInProgress()
    {
        return this.obstacleHeightCube.gameObject.activeInHierarchy;
    }

    // check, whether an obstacle is currently being edited
    public bool GetEditModeActive()
    {
        return this.obstacleToEdit;
    }

    // check, whether an obstacle is currently being created
    public bool GetCreationInProgress()
    {
        return this.obstaclePositionsCollected > 0;
    }

    // get all obstacles
    public GameObject[] GetObstacles()
    {
        return this.obstacles.ToArray();
    }

    // get obstacle hit by the ray of the given controller, returns null if no obstacle is hit
    public Transform GetObstacleHitByRay(bool pointerController)
    {
        return ManagerCollection.gameManager.hmd.GetTransformHitByRay(pointerController, this.obstacleLayer);
    }

    // update/scale height of the obstacle currently being created (based on the pointer of the given controller)
    public void UpdateNewObstacleHeight(Transform visualizer, bool pointerController)
    {
        // make sure that we have a valid height cube pointer position
        Vector3 heightCubePointerPos = ManagerCollection.gameManager.hmd.GetFloorPointerPosController(pointerController, this.obstacleHeightCubeLayer);
        if (!Single.IsInfinity(heightCubePointerPos.x))
        {
            visualizer.position = heightCubePointerPos;
            ManagerCollection.gameManager.hmd.SetControllerRayEndPoint(heightCubePointerPos);
            this.SetCurrentObstacleHeight(heightCubePointerPos.y - ManagerCollection.alignmentManager.GetFloor().position.y);
        }
    }

    // handle input for the creation of a new obstacle
    public void HandleObstaclePlacement(bool pointerController, int floorLayer)
    {
        ManagerCollection.gameManager.UpdateLastInteractionTime();

        // define width and depth of the obstacle through 3 points on the floor
        if (this.obstaclePositionsCollected < 3)
        {
            // make sure that we have a valid floor pointer position
            Vector3 floorPointerPos = ManagerCollection.gameManager.hmd.GetFloorPointerPosController(pointerController, floorLayer);
            if (Single.IsInfinity(floorPointerPos.x)) return;

            // record new position and show a visualizer
            this.obstaclePositions[this.obstaclePositionsCollected] = floorPointerPos;
            this.positionVisualizers[this.obstaclePositionsCollected].position = floorPointerPos;
            this.positionVisualizers[this.obstaclePositionsCollected].gameObject.SetActive(true);

            // show explanation of where to place the next position
            this.obstaclePositionsCollected++;
            ManagerCollection.statusTextManager.ShowObstaclePlacementPosition(this.obstaclePositionsCollected);

            // instantiate a new obstacle once all three positions were collected
            if (this.obstaclePositionsCollected == 3) this.CreateObstacle();
        }
        // define final height of the obstacle
        else
        {
            // make sure that we have a valid height cube pointer position
            Vector3 heightCubePointerPos = ManagerCollection.gameManager.hmd.GetFloorPointerPosController(pointerController, this.obstacleHeightCubeLayer);
            if (Single.IsInfinity(heightCubePointerPos.x)) return;

            // calculate and apply the height
            float height = heightCubePointerPos.y - ManagerCollection.alignmentManager.GetFloor().position.y;
            this.SetCurrentObstacleHeight(height);

            // allow creating the next new obstacle
            this.RestartObstaclePlacement();
        }
    }

    // create obstacle with 0 height and place height cube where the new obstacle was created
    private void CreateObstacle()
    {
        // create new obstacle and transform it based on the three recorded positions
        GameObject obstacle = Instantiate(this.obstaclePrefab, this.obstaclePositions[1], Quaternion.identity, this.obstacleParent);
        this.ApplyCubeTransformations(obstacle, this.obstaclePositions[0], this.obstaclePositions[1], this.obstaclePositions[2]);

        // store reference to the new obstacle
        this.obstacles.Add(obstacle);

        // activate height cube and apply the same transformations as for the new obstacle
        this.obstacleHeightCube.gameObject.SetActive(true);
        this.ApplyCubeTransformations(this.obstacleHeightCube.gameObject, this.obstaclePositions[0], this.obstaclePositions[1], this.obstaclePositions[2]);
    }

    // transform the given cube based on the three given reference points
    private void ApplyCubeTransformations(GameObject go, Vector3 pos0, Vector3 pos1, Vector3 pos2)
    {
        // calculate axes, width and depth
        Vector3 forward = pos2 - pos1;
        Vector3 right = pos1 - pos0;
        Vector3 up = ManagerCollection.alignmentManager.GetFloor().up;
        float width = right.magnitude;
        float depth = forward.magnitude;

        // apply width and depth
        Vector3 scale = go.transform.localScale;
        scale.x = width;
        scale.z = depth;
        go.transform.localScale = scale;

        // apply axes
        go.transform.up = up;
        go.transform.forward = forward;
        go.transform.right = right;

        // set position
        go.transform.position = pos1;
    }

    // scale the height of the current new obstacle
    private void SetCurrentObstacleHeight(float height)
    {
        Vector3 scale = this.obstacles[this.obstacles.Count - 1].transform.localScale;
        scale.y = height;
        this.obstacles[this.obstacles.Count - 1].transform.localScale = scale;
    }

    // (re-)start obstacle creation mode
    public void RestartObstaclePlacement()
    {
        // hide height cube
        this.obstacleHeightCube.gameObject.SetActive(false);
        
        // reset position counter and show explanation of where to place the first position
        this.obstaclePositionsCollected = 0;
        ManagerCollection.statusTextManager.ShowObstaclePlacementPosition(0);

        // hide all position visualizers
        foreach (var visualizer in this.positionVisualizers) visualizer.gameObject.SetActive(false);

    }

    // start edit mode for the given onstacle
    public void EnterObstacleEditMode(Transform obstacle)
    {
        ManagerCollection.gameManager.UpdateLastInteractionTime();

        // apply the edit mode material
        obstacle.GetComponentInChildren<MeshRenderer>().material = this.obstacleMaterialEditMode;

        // store reference to the obstacle to be edited
        this.obstacleToEdit = obstacle;

        // show instructions for the edit mode
        ManagerCollection.statusTextManager.ShowObstacleEditMode();
    }

    // stop obstacle edit mode
    public void ExitObstacleEditMode()
    {
        ManagerCollection.gameManager.UpdateLastInteractionTime();

        // restore original material of the obstacle
        if (this.obstacleToEdit) this.obstacleToEdit.GetComponentInChildren<MeshRenderer>().material = this.obstacleMaterialDefault;

        // go back to the obstacle creation mode
        this.obstacleToEdit = null;
        this.RestartObstaclePlacement();
    }

    // handle input for the obstacle edit mode
    public void HandleObstacleEditInput()
    {
        // translate obstacle
        Vector2 leftInput = ManagerCollection.gameManager.hmd.GetLeftJoystickInput();

        Vector3 xTranslation = Camera.main.transform.right;
        xTranslation.y = 0;
        Vector3 zTranslation = Camera.main.transform.forward;
        zTranslation.y = 0;

        this.obstacleToEdit.Translate((xTranslation * leftInput.x + zTranslation * leftInput.y) * this.obstacleTranslationSpeed * Time.deltaTime, Space.World);

        // rotate obstacle
        Vector2 rightInput = ManagerCollection.gameManager.hmd.GetRightJoystickInput();
        Vector3 center = this.obstacleToEdit.GetComponentInChildren<BoxCollider>().bounds.center;

        if (Mathf.Abs(rightInput.x) > Mathf.Abs(rightInput.y)) this.obstacleToEdit.RotateAround(center, this.obstacleToEdit.up, rightInput.x * this.obstacleRotationSpeed * Time.deltaTime);
        else this.obstacleToEdit.RotateAround(center, this.obstacleToEdit.right, rightInput.y * this.obstacleRotationSpeed * Time.deltaTime);
    }

    // delete the given obstacle
    public void DeleteObstacle(Transform obstacle, bool instant = false)
    {
        ManagerCollection.gameManager.UpdateLastInteractionTime();

        // change appearance of the obstacle (e.g. red material) and destroy it after the configured delay
        if (this.obstacles.Remove(obstacle.gameObject))
        {
            obstacle.GetComponentInChildren<MeshRenderer>().material = this.obstacleMaterialDeleted;
            Destroy(obstacle.gameObject, instant ? 0 : this.obstacleDeleteDestroyDelay);
        }
    }

    // show/hide all obstacle visualizations
    public void SetObstacleVisibility(bool visibility)
    {
        foreach (GameObject obstacle in this.obstacles) obstacle.GetComponentInChildren<MeshRenderer>().material = visibility ? this.obstacleMaterialDefault : this.obstacleMaterialMask;
    }

    // delete all obstacles and arcades
    public void RemoveAllObstaclesAndArcades()
    {
        // delete all obstacles
        while (this.obstacles.Count > 0) this.DeleteObstacle(this.obstacles[0].transform, true);

        // reset variables
        this.obstaclePositionsCollected = 0;
        this.obstacleToEdit = null;

        // delete all arcades
        this.RemoveAllArcades();
    }

    // recreate an obstacle with the given position, rotation and scale (executed when loading saved data)
    public void RecreateObstacle(Vector3 localPos, Quaternion localRot, Vector3 localScale)
    {
        // instantiate new obstacle and apply the given transformations
        GameObject obstacle = Instantiate(this.obstaclePrefab, this.obstacleParent);
        obstacle.transform.localPosition = localPos;
        obstacle.transform.localRotation = localRot;
        obstacle.transform.localScale = localScale;

        // store new obstacle in the list
        this.obstacles.Add(obstacle);
    }

    // instantiate a new arcade where the given controller's ray hits the floor
    public void HandleArcadePlacement(bool pointerController, int floorLayer)
    {
        ManagerCollection.gameManager.UpdateLastInteractionTime();

        // make sure that we have a valid floor pointer position
        Vector3 floorPointerPos = ManagerCollection.gameManager.hmd.GetFloorPointerPosController(pointerController, floorLayer);
        if (Single.IsInfinity(floorPointerPos.x)) return;

        // spawn arcade at the given position
        GameObject arcade = Instantiate(this.arcadePrefab, floorPointerPos, Quaternion.identity, this.obstacleParent);
        this.arcades.Add(arcade);

        // let arcade face the player
        Vector3 dir = Camera.main.transform.position - arcade.transform.position;
        dir.y = 0;
        arcade.transform.forward = dir.normalized;
    }

    // delete all arcades
    public void RemoveAllArcades()
    {
        while (this.arcades.Count > 0)
        {
            Destroy(this.arcades[0]);
            this.arcades.RemoveAt(0);
        }
    }
}
