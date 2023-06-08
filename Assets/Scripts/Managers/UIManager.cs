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
using UnityEngine.UI;
using static Character;

// script managing the settings UI
public class UIManager : MonoBehaviour
{
    [SerializeField] private GameObject controllerUI;
    [SerializeField] private GameObject rayInteractor;

    [Header("Settings pages")]
    [SerializeField] private GameObject mainSettingsPage;
    [SerializeField] private GameObject saveLoadPage;


    [Header("Visualization Settings")]
    [SerializeField] private GameObject humanoidSettingsTab;
    [SerializeField] private GameObject sphereSettingsTab;
    [SerializeField] private TMP_Text humanoidTransparencyText;
    [SerializeField] private TMP_Text sphereTransparencyText;
    [SerializeField] private Slider humanoidTransparencySlider;
    [SerializeField] private Slider sphereTransparencySlider;
    [SerializeField] private GameObject traceSettingsTab;
    [SerializeField] private TMP_Text traceTransparencyText;
    [SerializeField] private TMP_Text traceLengthText;
    [SerializeField] private Slider traceTransparencySlider;
    [SerializeField] private Slider traceLengthSlider;

    [Header("Load/Save UI")]
    [SerializeField] private TMP_Dropdown fileNameDropdown;
    [SerializeField] private Button saveLoadContinueButton;
    [SerializeField] private TMP_Text saveLoadInstructions;

    private bool loadingNotSaving = true;

    private void Awake()
    {
        ManagerCollection.uiManager = this;
    }
    private void Start()
    {
        // fill file-selection dropdown with the available save files
        this.fileNameDropdown.ClearOptions();
        this.fileNameDropdown.AddOptions(ManagerCollection.saveLoadManager.GetExistingSaveFileNames());
        this.fileNameDropdown.RefreshShownValue();

        // initialize character manager with full opacity, as this is the default setting at start-up
        ManagerCollection.characterManager.HandleOpacityInput(1);
    }

    // show/hide the settings UI
    public void SetVisibility(bool visible)
    {
        // toggle settings UI
        this.controllerUI.SetActive(visible);
        // toggle UI-interaction ray
        this.rayInteractor.SetActive(visible);
        // hide status text when the settings UI is active
        ManagerCollection.statusTextManager.SetVisibility(!visible);
    }

    // (de-)activate the humanoid visualization for the characters
    public void OnToggleHumanoidVisualization(bool active)
    {
        // show/hide the settings tab belonging to the humanoid visualization
        this.humanoidSettingsTab.SetActive(active);
        if (!active) return;

        // notify character manager of the visualization mode change
        ManagerCollection.characterManager.SetVisualizationMode(VisualizationMode.humanoid);
        // apply current transparency setting for the humanoid visualization
        this.OnHumanoidTransparencyChanged(this.humanoidTransparencySlider.value);
    }

    // (de-)activate the humanoid visualization for the characters
    public void OnToggleSphereVisualization(bool active)
    {
        // show/hide the settings tab belonging to the sphere visualization
        this.sphereSettingsTab.SetActive(active);
        if (!active) return;

        // notify character manager of the visualization mode change
        ManagerCollection.characterManager.SetVisualizationMode(VisualizationMode.sphere);
        // apply current transparency setting for the sphere visualization
        this.OnSphereTransparencyChanged(this.sphereTransparencySlider.value);
    }

    // change transparency of the humanoid visualization
    public void OnHumanoidTransparencyChanged(float transparency)
    {
        // hand over the value to the character manager
        ManagerCollection.characterManager.HandleOpacityInput(1 - transparency);
        // update value text on the UI
        this.humanoidTransparencyText.text = this.FloatToPercentage(transparency);
    }

    // change transparency of the sphere visualization
    public void OnSphereTransparencyChanged(float transparency)
    {
        // hand over the value to the character manager
        ManagerCollection.characterManager.HandleOpacityInput(1 - transparency);
        // update value text on the UI
        this.sphereTransparencyText.text = this.FloatToPercentage(transparency);
    }

    // (de-)activate the trail trace
    public void OnToggleTrailTrace(bool active)
    {
        this.OnToggleTrace(TraceMode.trail, active);
    }

    // (de-)activate the humanoid ghost trace
    public void OnToggleHumanoidGhostTrace(bool active)
    {
        this.OnToggleTrace(TraceMode.ghostHumanoid, active);
    }

    // (de-)activate the sphere ghost trace
    public void OnToggleSphereGhostTrace(bool active)
    {
        this.OnToggleTrace(TraceMode.ghostSphere, active);
    }

    // (de-)activate the given trace
    private void OnToggleTrace(TraceMode trace, bool active)
    {
        // hide trace settings tab
        if (!active) this.traceSettingsTab.SetActive(false);
        else
        {
            // show trace settings tab
            this.traceSettingsTab.SetActive(true);
            // notify character manager of the trace mode change
            ManagerCollection.characterManager.SetTraceMode(trace);
            // show settings of the new trace mode on the UI
            this.traceLengthSlider.value = ManagerCollection.characterManager.GetTraceLengthSetting(trace);
            this.traceTransparencySlider.value = ManagerCollection.characterManager.GetTraceTransparencySetting(trace);
        }
    }

    // update length setting of the currently active trace mode
    public void OnTraceLengthChanged(float length)
    {
        // get actual length calculated with the configured max length
        string calculatedLength = ManagerCollection.characterManager.SetTraceLength(length).ToString("0.0");
        // show new length setting on the UI
        this.traceLengthText.text = (length == 1) ? "infinite" : (calculatedLength + "s");
    }

    // update transparency setting of the currently active trace mode
    public void OnTraceTransparencyChanged(float transparency)
    {
        // hand value over to the character manager
        ManagerCollection.characterManager.SetTraceTransparency(transparency);
        // show new transparency setting on the UI
        this.traceTransparencyText.text = this.FloatToPercentage(transparency);
    }

    // convert the given float (min 0, max 1) to a string containing the percentage representation, e.g. 0.5 -> "50%"
    private string FloatToPercentage(float val)
    {
        return ((int)(val * 100)).ToString() + "%";
    }

    // show/hide the UI-tab for saving and loading
    public void SetSaveLoadVisibility(bool visible)
    {
        // show either the main settings page, or the saving/loading-page
        this.mainSettingsPage.SetActive(!visible);
        this.saveLoadPage.SetActive(visible);

        if (visible)
        {
            // when saving, the continue-button should only be interactable, once the anchor alignment is completed
            this.saveLoadContinueButton.interactable = this.loadingNotSaving;

            // initialize the anchor alignment
            ManagerCollection.saveLoadManager.RestartAnchorAlignment();
            ManagerCollection.statusTextManager.ShowAnchorAlignmentPosition();
            this.saveLoadInstructions.gameObject.SetActive(!this.loadingNotSaving);
        }
        else
        {
            // cancel saving/loading and return to the creation mode
            ManagerCollection.saveLoadManager.OnCancel();
            ManagerCollection.gameManager.SetCreationMode(true);
        }
    }

    // toggle between saving and loading
    public void OnToggleSaveLoadMode(bool load)
    {
        // store state to be available when the saving/loading-page is hidden and then shown again
        this.loadingNotSaving = load;

        if (load)
        {
            // when loading, the file selection dropdown should only contain the names of the existing files
            this.fileNameDropdown.options.RemoveAt(0);
            // no anchor alignment necessary when loading
            ManagerCollection.saveLoadManager.SkipAnchorAlignment();
        }
        else
        {
            // when saving, add an option for creating a new file to the file selection dropdown
            this.fileNameDropdown.options.Insert(0, new TMP_Dropdown.OptionData("Create new file"));
            // restart the anchor alignment
            ManagerCollection.saveLoadManager.RestartAnchorAlignment();
        }
        
        // refresh dropdown to show the updated set of options
        this.fileNameDropdown.RefreshShownValue();

        // only show the instructions when saving
        this.saveLoadInstructions.gameObject.SetActive(!load);

        // directly allow continuing when loading, as anchor alignment is only necessary when saving
        this.saveLoadContinueButton.interactable = load;
    }

    // allow interaction with the continue-button once the anchor alignment has been completed
    public void OnSaveLoadAnchorCompleted()
    {
        this.saveLoadContinueButton.interactable = true;
    }

    // progress in the saving/loading-process
    public void SaveLoadContinue()
    {
        // prevent ArgumentOutOfRangeException, can happen when switching between loading and saving
        if (this.fileNameDropdown.value >= this.fileNameDropdown.options.Count) this.fileNameDropdown.SetValueWithoutNotify(this.fileNameDropdown.options.Count - 1);

        // extract the filename selected by the user
        string fileName = this.fileNameDropdown.options[this.fileNameDropdown.value].text;

        // save mode
        if (this.fileNameDropdown.options[0].text == "Create new file")
        {
            // save current application state to the given file
            ManagerCollection.saveLoadManager.SaveData(fileName);
        }
        // load mode
        else
        {
            // load save data from the given file
            ManagerCollection.saveLoadManager.LoadData(fileName);
        }

        // hide the saving/loading-page
        this.SetSaveLoadVisibility(false);
    }
}