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
using TMPro;
using UnityEngine;

// script managing the status text explaining the current application state
public class StatusTextManager : MonoBehaviour
{
    [SerializeField] private GameObject canvas;
    [SerializeField] private TMP_Text text;
    [SerializeField] private GameObject rewindIndicator;
    [SerializeField] private GameObject timerParent;
    [SerializeField] private GameObject errorParent;
    [SerializeField] private TMP_Text errorText;
    [SerializeField] private GameObject successParent;
    [SerializeField] private TMP_Text successText;
    [SerializeField] private float messageDuration = 5;

    private float errorMessageTimer = 0;
    private float successMessageTimer = 0;

    // texts explaining the obstacle creation process
    private string[] obstacleTexts = new string[] {
        "Obstacle placement\nfront left corner",
        "Obstacle placement\nfront right corner",
        "Obstacle placement\nback right corner",
        "Obstacle placement\nheight"
    };

    private void Awake()
    {
        ManagerCollection.statusTextManager = this;
    }

    private void Update()
    {
        this.UpdateMessageTimers();
    }

    // update the message timers and hide the messages if their timer has run out
    private void UpdateMessageTimers()
    {
        if (this.errorMessageTimer <= 0)
        {
            // hide error message, if it reached its desired duration and is still visible
            if (this.errorParent.activeInHierarchy) this.errorParent.SetActive(false);
        }
        else
        {
            // decrease timer
            this.errorMessageTimer -= Time.deltaTime;
        }

        if (this.successMessageTimer <= 0)
        {
            // hide success message, if it reached its desired duration and is still visible
            if (this.successParent.activeInHierarchy) this.successParent.SetActive(false);
        }
        else
        {
            // decrease timer
            this.successMessageTimer -= Time.deltaTime;
        }
    }

    // show explanation for the given obstacle creation position
    public void ShowObstaclePlacementPosition(int pos)
    {
        this.text.text = this.obstacleTexts[pos];
    }

    // show explanation for setting the current obstacle's height
    public void ShowObstaclePlacementHeight()
    {
        this.text.text = this.obstacleTexts[3];
    }

    // show explanation for setting the anchor alignment position
    public void ShowAnchorAlignmentPosition()
    {
        this.text.text = "Set room anchor position";
    }

    // show explanation when the anchor alignment position has been set
    public void ShowAnchorAlignmentDone()
    {
        this.text.text = "Room anchor\nDone, use menu to continue";
    }

    // show explanation for the obstacle edit mode
    public void ShowObstacleEditMode()
    {
        this.text.text = "Obstacle edit mode.\nLeft stick for movement, right stick for rotation.";
    }

    // show explanation for the floor alignment
    public void ShowAlignFloor()
    {
        this.text.text = "Align floor";
    }

    // show explanation for the given object alignment position
    public void ShowObjectAlignPosition(int pos)
    {
        this.text.text = "Object align position " + (pos + 1).ToString() + "/3";
    }

    // show explanation when all object alignment positions have been recorded
    public void ShowObjectAlignConfirm()
    {
        this.text.text = "Execute object alignment";
    }

    // show character creation mode status
    public void ShowCharacterCreationMode()
    {
        this.text.text = "Character creation mode";
    }

    // show explanation for the character edit mode
    public void ShowCharacterEditMode()
    {
        this.text.text = "Character edit mode.\nLeft stick for movement, right stick for rotation.";
    }

    // show explanation for setting new goals and resetting the simulation
    public void ShowTargetSelection()
    {
        this.text.text = "A/X: new goal, B/Y: reset\nOption: start/stop rewind";
    }

    // show/hide rewind indicator
    public void SetRewindIndicatorVisibility(bool visible)
    {
        this.rewindIndicator.SetActive(visible);
    }

    // show/hide status text
    public void SetVisibility(bool visible)
    {
        this.canvas.SetActive(visible);
    }

    // show timer with the given value
    public void ShowTimer(float time)
    { 
        // make sure all other texts are hidden
        this.SetVisibility(false);

        // show the timer
        this.timerParent.GetComponentInChildren<TMP_Text>().text = $"Elapsed time:\n{time.ToString("0.00")}s";
        this.timerParent.SetActive(true);
    }

    // hide the timer
    public void HideTimer()
    {
        this.timerParent.SetActive(false);
    }

    // show the given error message to the user
    public void ShowErrorMessage(string message)
    {
        // show the desired error message
        this.errorText.text = message;
        this.errorParent.SetActive(true);

        // set the error message's timer
        this.errorMessageTimer = this.messageDuration;
    }

    // show the given success message to the user
    public void ShowSuccessMessage(string message)
    {
        // show the desired success message
        this.successText.text = message;
        this.successParent.SetActive(true);

        // set the success message's timer
        this.successMessageTimer = this.messageDuration;
    }
}
