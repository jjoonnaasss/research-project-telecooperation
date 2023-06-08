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

using UnityEngine;

// interface for the HMDs to be used in the application
public abstract class HMD : MonoBehaviour
{
    public const bool LEFT_HAND = false;
    public const bool RIGHT_HAND = true;
    public const bool LEFT_CONTROLLER = false;
    public const bool RIGHT_CONTROLLER= true;

    public enum noneLeftRightEnum { none, left, right };

    #region HAND_INTERACTION

    // draw a laserpointer based on the orientation of the according hand (hand: false is left, true is right)
    public abstract void DrawHandRay(bool hand, float length);

    // get the position of the collision between laserpointer/hand ray and the floor
    public abstract Vector3 GetFloorPointerPosHand(bool hand, int layerMask);

    // check whether the user is pinching his fingers (hand: false is left, true is right)
    public abstract bool IsUserPinching(bool hand);

    // get the position of the tip of the index finger of the according hand (hand: false is left, true is right)
    public abstract Vector3 GetIndexTipPosition(bool hand);

    #endregion

    #region CONTROLLER_INTERACTION

    // draw a laserpointer based on the orientation of the according controller (controller: false is left, true is right)
    public abstract void DrawControllerRay(bool controller, float length);

    // set end point where the controller should stop
    public abstract void SetControllerRayEndPoint(Vector3 point);

    // hide the laserpointer
    public abstract void HideControllerRay();

    // get the raycast hit of the collision between laserpointer/controller ray and the given layers (e.g. floor layer)
    public abstract RaycastHit? GetFloorPointerHitController(bool controller, int layerMask);

    // get the position of the collision between laserpointer/controller ray and the given layers (e.g. floor layer)
    public abstract Vector3 GetFloorPointerPosController(bool controller, int layerMask);

    // get raycast hit of the object hit by the ray, if any object of the given layer is actually hit, else return null
    public abstract RaycastHit? GetControllerRaycastHit(bool controller, int layerMask);

    // get transform of the object hit by the ray, if any object of the given layer is actually hit, else return null
    public abstract Transform GetTransformHitByRay(bool controller, int layerMask);

    // get the position of the tip of the according controller (controller: false is left, true is right)
    public abstract Vector3 GetControllerTipPosition(bool controller);

    // check, whether the primary button has been released
    public abstract bool GetPrimaryButtonReleased();

    // check, whether the secondary button has been released
    public abstract bool GetSecondaryButtonReleased();

    // check, whether the option button has been released
    public abstract bool GetOptionButtonReleased();

    // get the input from the left joystick
    public abstract Vector2 GetLeftJoystickInput();

    // get the input from the right joystick
    public abstract Vector2 GetRightJoystickInput();

    // get information if the left joystick is currently being pressed down
    public abstract bool GetLeftJoystickPressedDown();

    // get information if the right joystick is currently being pressed down
    public abstract bool GetRightJoystickPressedDown();

    // get the input from the grab-button
    public abstract float GetGrabInput();

    // get the input from the trigger-button
    public abstract float GetTriggerInput();

    // get the input from the left trigger-button
    public abstract float GetLeftTriggerInput();

    // get the input from the right trigger-button
    public abstract float GetRightTriggerInput();

    #endregion

    // get the y-position of the floor
    public abstract float GetFloorY();
}