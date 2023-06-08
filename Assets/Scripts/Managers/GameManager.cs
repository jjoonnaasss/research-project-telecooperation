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
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// main manager controlling the game and coordinating the collaboration of the other managers
public class GameManager : MonoBehaviour
{
    [SerializeField] public HMD hmd;
    [SerializeField] private Transform floorPointer;
    [SerializeField] private LayerMask floorLayer;
    [SerializeField] private LayerMask fireSpawnLayers;

    [Header("UI elements")]
    [SerializeField] private Toggle creationModeToggle;
    [SerializeField] private GameObject[] tooltipParents;

    [Header("Settings")]
    [SerializeField] private bool reverseHandRoles = false;
    [SerializeField] private float pointerLength = 10;
    [SerializeField] private float interactionCooldown = 1;
    [SerializeField] private float interactionCooldownJoystick = 0.25f;
    [SerializeField] private bool useZeroAsFloorHeight = false;
    [SerializeField] private bool performObjectAlignment = false;
    [SerializeField] private float triggerClickThreshold = 0.5f;
    [SerializeField] private float grabClickThreshold = 0.5f;
    [SerializeField] private float joystickAntiDriftThreshold = 0.1f;
    [SerializeField] private Material activeRewindableMaterial;

    private float lastInteraction;
    private bool primaryController;
    private bool creationMode = true;
    private bool demoMode = false;
    private Rewindable currentRewindable;

    private void Awake()
    {
        ManagerCollection.gameManager = this;
    }

    private void Start()
    {
        ManagerCollection.alignmentManager.Init(this.performObjectAlignment);
 
        this.UpdateLastInteractionTime();
        ManagerCollection.statusTextManager.ShowAlignFloor();

        this.primaryController = this.reverseHandRoles ? HMD.LEFT_CONTROLLER : HMD.RIGHT_CONTROLLER;

        if (this.useZeroAsFloorHeight) ManagerCollection.alignmentManager.SetFloorHeightTo(0);
    }

    private void Update()
    {
        // executed when saving/loading is in progress
        if (ManagerCollection.saveLoadManager.GetInProgress())
        {
            // update to be executed whenever saving/loading is in progress
            this.UpdateSaveLoadInProgress();
            // maybe insert this.uiManager.SetVisibility(this.hmd.GetGrabInput() != 0); here
        }
        // executed when there is no grab input or when the demo mode is active
        else if (this.hmd.GetGrabInput() == 0 || this.demoMode)
        {
            // hide settings UI
            ManagerCollection.uiManager.SetVisibility(false);
            // hide status text when in demo mode
            ManagerCollection.statusTextManager.SetVisibility(!this.demoMode);
            // update to be executed whenever the settings UI is hidden
            this.UpdateUIHidden();
        }
        else
        {
            // show settings UI
            ManagerCollection.uiManager.SetVisibility(true);
            // update to be executed whenever the settings UI is visible
            this.UpdateUIActive();
        }
    }

    // update to be executed whenever saving/loading is in progress
    private void UpdateSaveLoadInProgress()
    {
        // primary button to trigger anchor alignment progress
        if (Time.time - this.lastInteraction > this.interactionCooldown && this.hmd.GetPrimaryButtonReleased())
        {
            ManagerCollection.saveLoadManager.HandleAlignmentInput(this.primaryController);
        }
    }

    // update to be executed whenever the settings UI is hidden
    private void UpdateUIHidden()
    {
        // draw ray/laserpointer
        if (ManagerCollection.alignmentManager.GetAlignmentPositionsCollected() >= 4 || !this.performObjectAlignment) this.hmd.DrawControllerRay(this.primaryController, this.pointerLength);

        // use ray to set different heights
        if (!ManagerCollection.alignmentManager.GetFloorHeightSet())
        {
            // move floor height with the controller
            ManagerCollection.alignmentManager.MoveFloorWithControllerTip(this.reverseHandRoles ? HMD.LEFT_CONTROLLER : HMD.RIGHT_CONTROLLER);
        }
        else if (ManagerCollection.obstacleManager.GetHeightAdjustmentInProgress())
        {
            // adjust height of the new obstacle
            ManagerCollection.obstacleManager.UpdateNewObstacleHeight(this.floorPointer, this.primaryController);
        }
        else if (ManagerCollection.characterManager.GetHeightAdjustmentInProgress())
        {
            // adjust height of the new character
            ManagerCollection.characterManager.UpdateNewCharacterHeight(this.floorPointer, this.primaryController);
        }
        else if (ManagerCollection.alignmentManager.GetAlignmentPositionsCollected() >= 4 || !this.performObjectAlignment)
        {
            // visualize point where the ray hits the floor
            Vector3 floorPointerPos = this.hmd.GetFloorPointerPosController(this.primaryController, this.demoMode ? this.fireSpawnLayers : this.floorLayer);
            if (!Single.IsInfinity(floorPointerPos.x))
            {
                this.floorPointer.position = floorPointerPos;
                this.hmd.SetControllerRayEndPoint(floorPointerPos);
            }
        }

        // handle input for the edit modes
        if (ManagerCollection.obstacleManager.GetEditModeActive()) ManagerCollection.obstacleManager.HandleObstacleEditInput();
        else if (ManagerCollection.characterManager.GetEditModeActive()) ManagerCollection.characterManager.HandleCharacterEditInput();

        // handle input of the various buttons on the controllers
        if (this.GetDemoModeToggleAllowed() && Time.time - this.lastInteraction > this.interactionCooldown && this.hmd.GetLeftJoystickPressedDown() && this.hmd.GetRightJoystickPressedDown())
        {
            this.ToggleDemoMode();
        }
        else if (Time.time - this.lastInteraction > this.interactionCooldown && this.hmd.GetPrimaryButtonReleased())
        {
            this.HandlePrimaryButton();
        }
        else if (this.GetSecondaryButtonAllowed() && Time.time - this.lastInteraction > this.interactionCooldown && this.hmd.GetSecondaryButtonReleased())
        {
            this.HandleSecondaryButton();
        }
        else if (this.GetOptionButtonAllowed() && Time.time - this.lastInteraction > this.interactionCooldown && this.hmd.GetOptionButtonReleased())
        {
            this.HandleOptionButton();
        }
        else if (this.GetTriggerButtonAllowed() && Time.time - this.lastInteraction > this.interactionCooldown && this.hmd.GetTriggerInput() >= this.triggerClickThreshold)
        {
            this.HandleTriggerButton();
        }
        else if (this.GetGrabButtonAllowed() && Time.time - this.lastInteraction > this.interactionCooldown && this.hmd.GetGrabInput() >= this.grabClickThreshold)
        {
            this.HandleGrabButton();
        }
        else if (this.GetLeftJoystickAllowed() && Time.time - this.lastInteraction > this.interactionCooldownJoystick && Mathf.Abs(this.hmd.GetLeftJoystickInput().x) > this.joystickAntiDriftThreshold)
        {
            this.HandleLeftJoystickInput();
        }
    }

    // update to be executed whenever the settings UI is visible
    private void UpdateUIActive()
    {
        // hide ray/laserpointer
        this.hmd.HideControllerRay();
    }

    // check, whether the secondary button is allowed to trigger actions
    private bool GetSecondaryButtonAllowed()
    {
        // allowed in creation mode, when floor height has been set, object alignment is done or deactivated and neither obstacle nor character is currently being created
        bool allowedInCreationMode = ManagerCollection.alignmentManager.GetFloorHeightSet() &&
            (ManagerCollection.alignmentManager.GetAlignmentPositionsCollected() >= 4 || !this.performObjectAlignment) &&
            !ManagerCollection.obstacleManager.GetCreationInProgress() &&
            !ManagerCollection.characterManager.GetHeightAdjustmentInProgress();

        // allowed when not in creation mode or when in creation mode, but the above conditions are fulfilled
        return !this.creationMode || allowedInCreationMode;
    }

    // check, whether the option-button is allowed to trigger actions
    private bool GetOptionButtonAllowed()
    {
        // only allowed when not in creation mode or when in creation mode, but the secondary button is allowed and no edit mode is active
        return !this.creationMode || this.GetSecondaryButtonAllowed() && !ManagerCollection.obstacleManager.GetEditModeActive() && !ManagerCollection.characterManager.GetEditModeActive();
    }

    // check, whether the trigger-button is allowed to trigger actions
    private bool GetTriggerButtonAllowed()
    {
        // only allowed in demo mode
        return this.demoMode;
    }

    // check, whether the grab-button is allowed to trigger actions
    private bool GetGrabButtonAllowed()
    {
        // only allowed in demo mode
        return this.demoMode;
    }

    // check, whether the left joystick is allowed to trigger actions
    private bool GetLeftJoystickAllowed()
    {
        // only allowed in demo mode
        return this.demoMode;
    }

    // check, whether toggling the demo mode is allowed
    private bool GetDemoModeToggleAllowed()
    {
        // only allowed when in demo mode or when not in creation mode and saving/loading is not in progress
        return this.demoMode || (!this.creationMode && !ManagerCollection.saveLoadManager.GetInProgress());
    }

    // update time of last interaction
    public void UpdateLastInteractionTime()
    {
        this.lastInteraction = Time.time;
    }

    // execute primary button action
    private void HandlePrimaryButton()
    {
        // executed in demo mode
        if (this.demoMode)
        {
            // let the character whose rewindable is the currently active one start walking or halt him
            if (this.currentRewindable != null) this.currentRewindable.HandleResumeOrHalt(true);
            // start the simulation
            else ManagerCollection.characterManager.StartFleeing();
        }
        // executed when player has left the overall creation mode
        else if (!this.creationMode) ManagerCollection.characterManager.HandleDestinationInput(this.primaryController, this.floorLayer, false);
        // executed if the player hasn't set the floor height yet (happens automatically if useZeroAsFloorHeight is set to true)
        else if (!ManagerCollection.alignmentManager.GetFloorHeightSet()) ManagerCollection.alignmentManager.AdjustFloorHeightToCurrentTip(this.primaryController);
        // executed if object alignment should be performed but is not completed yet
        else if (ManagerCollection.alignmentManager.GetAlignmentPositionsCollected() < 4 && this.performObjectAlignment) ManagerCollection.alignmentManager.HandleAlignmentInput(this.primaryController);
        // executed in character creation mode
        else if (ManagerCollection.characterManager.GetCreationMode()) ManagerCollection.characterManager.HandleCharacterCreation(this.primaryController, this.floorLayer);
        //executed in obstacle creation mode
        else ManagerCollection.obstacleManager.HandleObstaclePlacement(this.primaryController, this.floorLayer);
    }

    // execute secondary button action
    private void HandleSecondaryButton()
    {
        // executed in demo mode
        if (this.demoMode)
        {
            // clear the currently activate rewindable after restoring its original color
            if (this.currentRewindable != null)
            {
                this.currentRewindable.RestoreOriginalTrailMaterial();
                this.currentRewindable = null;
            }

            // reset whole simulation (remove characters, fires and arcades)
            ManagerCollection.characterManager.ResetCharacters();
            ManagerCollection.obstacleManager.RemoveAllArcades();
        }
        // executed when player has left the overall creation mode
        else if (!this.creationMode) ManagerCollection.characterManager.ResetCharacters();
        // executed when player is in obstacle creation mode
        else if (!ManagerCollection.characterManager.GetCreationMode())
        {
            Transform obstacleHitByRay = ManagerCollection.obstacleManager.GetObstacleHitByRay(this.primaryController);

            // end edit mode if it currently is active
            if (ManagerCollection.obstacleManager.GetEditModeActive()) ManagerCollection.obstacleManager.ExitObstacleEditMode();
            // start edit mode if an obstacle is being pointed at
            else if (obstacleHitByRay) ManagerCollection.obstacleManager.EnterObstacleEditMode(obstacleHitByRay.parent);
        }
        // executed when player is in character creation mode
        else
        {
            Transform characterHitByRay = ManagerCollection.characterManager.GetCharacterHitByRay(this.primaryController);

            // end edit mode if it currently is active
            if (ManagerCollection.characterManager.GetEditModeActive()) ManagerCollection.characterManager.ExitCharacterEditMode();
            // start edit mode if a character is being pointed at
            else if (characterHitByRay) ManagerCollection.characterManager.EnterCharacterEditMode(characterHitByRay);
        }
    }

    // execute option-button action
    private void HandleOptionButton()
    {
        // executed in demo mode
        if (this.demoMode) Debug.Log("Option button demo mode");
        // executed when player has left the overall creation mode
        else if (!this.creationMode) Debug.Log("TODO: option button");
        // executed when player is in obstacle creation mode
        else if (!ManagerCollection.characterManager.GetCreationMode())
        {
            // delete obstacle if there is one being pointed at
            Transform obstacleHitByRay = ManagerCollection.obstacleManager.GetObstacleHitByRay(this.primaryController);
            if (obstacleHitByRay) ManagerCollection.obstacleManager.DeleteObstacle(obstacleHitByRay.parent);
        }
        // executed when player is in character creation mode
        else
        {
            // delete character if there is one being pointed at
            Transform characterHitByRay = ManagerCollection.characterManager.GetCharacterHitByRay(this.primaryController);
            if (characterHitByRay) ManagerCollection.characterManager.DeleteCharacter(characterHitByRay);
        }
    }

    // execute trigger-button action
    private void HandleTriggerButton()
    {
        // executed in demo mode
        if (this.demoMode)
        {
            // spawn a new fire
            if (this.hmd.GetRightTriggerInput() > this.hmd.GetLeftTriggerInput()) ManagerCollection.fireManager.SpawnFire(this.primaryController, this.fireSpawnLayers);
            // spawn a new arcade
            else ManagerCollection.obstacleManager.HandleArcadePlacement(this.primaryController, this.floorLayer);
        }
    }

    // execute grab-button action
    private void HandleGrabButton()
    {
        // executed in demo mode
        if (this.demoMode)
        {
            RaycastHit? hit = ManagerCollection.characterManager.GetRewindTrailRaycastHit(this.primaryController);

            if (hit != null)
            {
                // restore the old rewindable's original trail color
                if (this.currentRewindable != null) this.currentRewindable.RestoreOriginalTrailMaterial();

                // store the rewindable whose trail the controller ray hit as the currently active rewindable and highlight it
                this.currentRewindable = hit.Value.collider.GetComponent<TrailMesh>().rewindable;
                this.currentRewindable.HighlightTrail(this.activeRewindableMaterial);

                // rewind the character belonging to the rewindable to its stored position closest to the hit
                this.currentRewindable.HandleRewindTrailSelected(hit.Value.point);
            }
            else if (this.currentRewindable != null)
            {
                // clear the currently activate rewindable, if no trail was hit, after restoring its original color
                this.currentRewindable.RestoreOriginalTrailMaterial();
                this.currentRewindable = null;
            }
        }
    }

    // execute actions based on the left joystick's input
    private void HandleLeftJoystickInput()
    {
        // executed in demo mode
        if (this.demoMode)
        {
            // rewind the character whose rewindable is the currently active one forward or backward by one position
            if (this.currentRewindable != null)
            {
                if (this.hmd.GetLeftJoystickInput().x > 0) this.currentRewindable.GoToNextState();
                else if (this.hmd.GetLeftJoystickInput().x < 0) this.currentRewindable.GoToPreviousState();
            }
        }
    }

    // (de-)activate the overall creation mode
    public void SetCreationMode(bool mode)
    {
        // set flag
        this.creationMode = mode;
        // hide obstacle visualizations outside of the creation mode
        ManagerCollection.obstacleManager.SetObstacleVisibility(mode);
        // set UI-toggle to the according value
        this.creationModeToggle.isOn = mode;

        // exit obstacle and character creation mode when overall creation mode is deactivated
        if (!mode)
        {
            if (ManagerCollection.characterManager.GetCreationMode()) ManagerCollection.characterManager.ExitCharacterEditMode();
            else ManagerCollection.obstacleManager.ExitObstacleEditMode();
        }

        // update status text
        if (!mode) ManagerCollection.statusTextManager.ShowTargetSelection();
        else if (ManagerCollection.characterManager.GetCreationMode()) ManagerCollection.statusTextManager.ShowCharacterCreationMode();
        else ManagerCollection.statusTextManager.ShowObstaclePlacementPosition(0);
    }

    // (de-)activate the demo mode
    private void ToggleDemoMode()
    {
        this.UpdateLastInteractionTime();
        this.demoMode = !this.demoMode;

        // hide status text in the demo mode
        ManagerCollection.statusTextManager.SetVisibility(!this.demoMode);
        // show tooltips in the demo mode
        foreach (GameObject parent in this.tooltipParents) parent.SetActive(this.demoMode);

        // hide timer outside of the demo mode
        if (!this.demoMode) ManagerCollection.statusTextManager.HideTimer();
    }

    // quit application
    public void Quit()
    {
        #if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
        #endif
        Application.Quit();
    }
}