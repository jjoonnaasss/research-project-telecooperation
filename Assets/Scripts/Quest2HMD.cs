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

// script implementing the HMD interface for a Quest 2 or Quest Pro
public class Quest2HMD : HMD
{
    [SerializeField] private OVRHand leftHand;
    [SerializeField] private OVRHand rightHand;
    [SerializeField] private LineRenderer ray;
    [SerializeField] private Transform leftController;
    [SerializeField] private Transform rightController;
    [SerializeField] private Transform controllerTipVisualizer;

    [Header("Settings")]
    [SerializeField] private bool useCustomHandRay = false;
    [SerializeField] private HMD.noneLeftRightEnum visualizeControllerTip;
    [SerializeField] private Vector3 controllerTipOffset;

    private OVRSkeleton leftHandSkeleton;
    private OVRSkeleton rightHandSkeleton;

    private void Start()
    {
        this.leftHandSkeleton = this.leftHand.GetComponent<OVRSkeleton>();
        this.rightHandSkeleton = this.rightHand.GetComponent<OVRSkeleton>();
    }

    private void Update()
    {
        // update oculus input system
        OVRInput.Update();

        // show or hide controller tip visualizer at the desired controller
        if (this.visualizeControllerTip == HMD.noneLeftRightEnum.left) this.ShowControllerTipVisualizer(HMD.LEFT_CONTROLLER);
        else if (this.visualizeControllerTip == HMD.noneLeftRightEnum.right) this.ShowControllerTipVisualizer(HMD.RIGHT_CONTROLLER);
        else this.controllerTipVisualizer.gameObject.SetActive(false);
    }

    private void FixedUpdate()
    {
        // update oculus input system
        OVRInput.FixedUpdate();
    }

    #region HAND_INTERACTION

    // draw a laserpointer based on the orientation of the according hand (hand: false is left, true is right)
    public override void DrawHandRay(bool hand, float length)
    {
        // get starting position and direction of the ray
        (Vector3 pos, Vector3 dir) = this.useCustomHandRay ? this.GetHandRayDataCustom(hand) : this.GetHandRayDataBuiltIn(hand);
        
        // apply starting position and direction to the ray
        if (!Single.IsInfinity(pos.x))
        {
            this.ray.SetPositions(new Vector3[] { pos, pos + dir * length });
            this.ray.enabled = true;
        }
        // hide ray if no valid position and direction could be found
        else
        {
            this.ray.enabled = false;
        }
        
    }

    // get starting position and direction for the ray using the Oculus plugin's hand pointer pose
    private (Vector3, Vector3) GetHandRayDataBuiltIn(bool hand)
    {
        OVRHand activeHand = (hand == HMD.LEFT_HAND) ? this.leftHand : this.rightHand;

        if (activeHand.IsPointerPoseValid)
        {
            Transform pointerTransform = activeHand.PointerPose;
            return (pointerTransform.position, pointerTransform.forward);
        }
        else return (Vector3.negativeInfinity, Vector3.negativeInfinity);
    }

    // get starting position and direction for the ray using specific bones of the Oculus plugin's hand skeleton
    private (Vector3, Vector3) GetHandRayDataCustom(bool hand)
    {
        OVRSkeleton activeSkeleton = (hand == HMD.LEFT_HAND) ? this.leftHandSkeleton : this.rightHandSkeleton;

        if (activeSkeleton.Bones.Count > 20)
        {
            Vector3 indexTip = activeSkeleton.Bones[20].Transform.position;
            Vector3 indexDistalPhalange = activeSkeleton.Bones[8].Transform.position;
            return (indexTip, (indexTip - indexDistalPhalange).normalized);
        }
        else return (Vector3.negativeInfinity, Vector3.negativeInfinity);
    }

    // get the position of the collision between laserpointer/hand ray and the floor
    public override Vector3 GetFloorPointerPosHand(bool hand, int layerMask)
    {
        // get starting position and direction of the ray
        (Vector3 pos, Vector3 dir) = this.useCustomHandRay ? this.GetHandRayDataCustom(hand) : this.GetHandRayDataBuiltIn(hand);
        
        if (!Single.IsInfinity(pos.x))
        {
            RaycastHit hit;

            if (Physics.Raycast(pos, dir, out hit, Mathf.Infinity, layerMask))
            {
                return hit.point;
            }
        }

        return Vector3.negativeInfinity;
    }

    // check whether the user is pinching his fingers (hand: false is left, true is right)
    public override bool IsUserPinching(bool hand)
    {
        OVRHand activeHand = (hand == HMD.LEFT_HAND) ? this.leftHand : this.rightHand;

        return activeHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
    }

    // get the position of the tip of the index finger of the according hand (hand: false is left, true is right)
    public override Vector3 GetIndexTipPosition(bool hand)
    {
        OVRSkeleton activeSkeleton = (hand == HMD.LEFT_HAND) ? this.leftHandSkeleton : this.rightHandSkeleton;

        if (activeSkeleton.Bones.Count > 20) return activeSkeleton.Bones[20].Transform.position;
        return Vector3.negativeInfinity;
    }
    #endregion

    #region CONTROLLER_INTERACTION

    // draw a laserpointer based on the orientation of the according controller (controller: false is left, true is right)
    public override void DrawControllerRay(bool controller, float length)
    {
        if ((controller == HMD.LEFT_CONTROLLER && this.leftController.gameObject.activeInHierarchy) || (controller == HMD.RIGHT_CONTROLLER && this.rightController.gameObject.activeInHierarchy))
        {
            // get starting position and direction of the ray
            Vector3 pos = (controller == HMD.LEFT_CONTROLLER) ? this.leftController.position : this.rightController.position;
            Vector3 dir = (controller == HMD.LEFT_CONTROLLER) ? this.leftController.forward : this.rightController.forward;

            // apply starting position and direction to the ray
            this.ray.SetPositions(new Vector3[] { pos, pos + dir * length });
            this.ray.enabled = true;
        }
        else this.ray.enabled = false;

    }

    // set end point where the controller should stop
    public override void SetControllerRayEndPoint(Vector3 point)
    {
        this.ray.SetPosition(1, point);
    }

    // hide the laserpointer
    public override void HideControllerRay()
    {
        this.ray.enabled = false;
    }

    // get the raycast hit of the collision between laserpointer/controller ray and the given layers (e.g. floor layer)
    public override RaycastHit? GetFloorPointerHitController(bool controller, int layerMask)
    {
        if ((controller == HMD.LEFT_CONTROLLER && this.leftController.gameObject.activeInHierarchy) || (controller == HMD.RIGHT_CONTROLLER && this.rightController.gameObject.activeInHierarchy))
        {
            // get starting position and direction of the ray
            Vector3 pos = (controller == HMD.LEFT_CONTROLLER) ? this.leftController.position : this.rightController.position;
            Vector3 dir = (controller == HMD.LEFT_CONTROLLER) ? this.leftController.forward : this.rightController.forward;

            RaycastHit hit;

            if (Physics.Raycast(pos, dir, out hit, Mathf.Infinity, layerMask))
            {
                return hit;
            }
        }
        return null;
    }

    // get the position of the collision between laserpointer/controller ray and the given layers (e.g. floor layer)
    public override Vector3 GetFloorPointerPosController(bool controller, int layerMask)
    {
        RaycastHit? hit = this.GetFloorPointerHitController(controller, layerMask);

        if (hit != null) return ((RaycastHit)hit).point;
        return Vector3.negativeInfinity;
    }

    // get raycast hit of the object hit by the ray, if any object of the given layer is actually hit, else return null
    public override RaycastHit? GetControllerRaycastHit(bool controller, int layerMask)
    {
        if ((controller == HMD.LEFT_CONTROLLER && this.leftController.gameObject.activeInHierarchy) || (controller == HMD.RIGHT_CONTROLLER && this.rightController.gameObject.activeInHierarchy))
        {
            // get starting position and direction of the ray
            Vector3 pos = (controller == HMD.LEFT_CONTROLLER) ? this.leftController.position : this.rightController.position;
            Vector3 dir = (controller == HMD.LEFT_CONTROLLER) ? this.leftController.forward : this.rightController.forward;

            RaycastHit hit;

            if (Physics.Raycast(pos, dir, out hit, Mathf.Infinity, layerMask))
            {
                return hit;
            }
        }
        return null;
    }

    // get transform of the object hit by the ray, if any object of the given layer is actually hit, else return null
    public override Transform GetTransformHitByRay(bool controller, int layerMask)
    {
        RaycastHit? hit = this.GetControllerRaycastHit(controller, layerMask);
        if (hit != null) return hit.Value.transform;

        return null;
    }

    // get the position of the tip of the according controller (controller: false is left, true is right)
    public override Vector3 GetControllerTipPosition(bool controller)
    {
        if ((controller == HMD.LEFT_CONTROLLER && this.leftController.gameObject.activeInHierarchy) || (controller == HMD.RIGHT_CONTROLLER && this.rightController.gameObject.activeInHierarchy))
        {
            Transform controllerTransform = (controller == HMD.LEFT_CONTROLLER) ? this.leftController : this.rightController;
            // calculate tip position based on the controller's position and the configured offsets in the controller's axes' directions
            Vector3 tipPosition = controllerTransform.position 
                + controllerTransform.right * this.controllerTipOffset.x
                + controllerTransform.up * this.controllerTipOffset.y
                + controllerTransform.forward * this.controllerTipOffset.z;
            return tipPosition;
        }
        return Vector3.negativeInfinity;
    }

    // show the tip visualizer for the given controller
    private void ShowControllerTipVisualizer(bool controller) 
    {
        Vector3 pos = this.GetControllerTipPosition(controller);
        if (!Single.IsInfinity(pos.x))
        {
            this.controllerTipVisualizer.position = pos;
            this.controllerTipVisualizer.gameObject.SetActive(true);
        }
        else this.controllerTipVisualizer.gameObject.SetActive(false);
    }

    // check, whether the primary button has been released (checks both controllers)
    public override bool GetPrimaryButtonReleased()
    {
        return this.GetButtonReleased(OVRInput.Button.One) || this.GetButtonReleased(OVRInput.Button.Three);
    }

    // check, whether the secondary button has been released (checks both controllers)
    public override bool GetSecondaryButtonReleased()
    {
        return this.GetButtonReleased(OVRInput.Button.Two) || this.GetButtonReleased(OVRInput.Button.Four);
    }

    // check, whether the option button has been released
    public override bool GetOptionButtonReleased()
    {
        return this.GetButtonReleased(OVRInput.Button.Start);
    }

    // check, whether the given button has been released
    private bool GetButtonReleased(OVRInput.Button button)
    {
        return OVRInput.Get(button);
    }

    // get the input from the left joystick
    public override Vector2 GetLeftJoystickInput()
    {
        return OVRInput.Get(OVRInput.Axis2D.PrimaryThumbstick);
    }

    // get the input from the right joystick
    public override Vector2 GetRightJoystickInput()
    {
        return OVRInput.Get(OVRInput.Axis2D.SecondaryThumbstick);
    }

    // get information if the left joystick is currently being pressed down
    public override bool GetLeftJoystickPressedDown()
    {
        return OVRInput.Get(OVRInput.Button.PrimaryThumbstick);
    }

    // get information if the right joystick is currently being pressed down
    public override bool GetRightJoystickPressedDown()
    {
        return OVRInput.Get(OVRInput.Button.SecondaryThumbstick);
    }

    // get the input from the grab-button (returns information from the grab-button with the bigger value)
    public override float GetGrabInput()
    {
        float leftGrab = OVRInput.Get(OVRInput.Axis1D.PrimaryHandTrigger, OVRInput.Controller.Touch);
        float rightGrab = OVRInput.Get(OVRInput.Axis1D.SecondaryHandTrigger, OVRInput.Controller.Touch);

        if (Mathf.Abs(leftGrab) > Mathf.Abs(rightGrab)) return leftGrab;
        return rightGrab;
    }

    // get the input from the trigger-button (returns information from the trigger with the bigger value)
    public override float GetTriggerInput()
    {
        float leftTrigger = this.GetLeftTriggerInput();
        float rightTrigger = this.GetRightTriggerInput();

        if (Mathf.Abs(leftTrigger) > Mathf.Abs(rightTrigger)) return leftTrigger;
        return rightTrigger;
    }

    // get the input from the left trigger-button
    public override float GetLeftTriggerInput()
    {
        return OVRInput.Get(OVRInput.Axis1D.PrimaryIndexTrigger, OVRInput.Controller.Touch);
    }

    // get the input from the right trigger-button
    public override float GetRightTriggerInput()
    {
        return OVRInput.Get(OVRInput.Axis1D.SecondaryIndexTrigger, OVRInput.Controller.Touch);
    }

    #endregion

    // get the y-position of the floor from the guardian (unsure if this works correctly)
    public override float GetFloorY()
    {
        Vector3[] playArea = OVRManager.boundary.GetGeometry(OVRBoundary.BoundaryType.PlayArea);
        if (playArea.Length > 0) return playArea[0].y;

        return 0;
    }

    // visualize controller tip in the editor
    void OnDrawGizmosSelected()
    {

#if UNITY_EDITOR
        Vector3 tipPosition = this.GetControllerTipPosition(HMD.RIGHT_CONTROLLER);

        // the controllers' scale is 0.5 in the editor, but 1 during runtime, this has to be compensated for the gizmos
        tipPosition -= this.controllerTipOffset / 2;
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(tipPosition, 0.001f);
        Gizmos.color = Color.white;
#endif
    }
}
