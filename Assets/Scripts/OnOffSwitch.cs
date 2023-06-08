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
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// script controlling a simple on-off toggle
public class OnOffSwitch : MonoBehaviour
{
    [SerializeField] private Image onImage;
    [SerializeField] private Image offImage;
    [SerializeField] private Image handleImage;
    [SerializeField] private int toggleOffsetX = 20;
    [SerializeField] private int animationSpeed = 1;

    public bool isOn;

    [SerializeField] private UnityEvent onActivated;
    [SerializeField] private UnityEvent onDeactivated;

    private int currentOffsetX;


    void Start()
    {
        // set the toggle to the starting position
        this.currentOffsetX = 0;
    }

    void Update()
    {
        // toggle is set to true, but hasn't reached the "true" position yet
        if (this.isOn && this.currentOffsetX < 20) // TODO: replace 20 with toggleOffsetX?
        {
            // move toggle towards the "true" position
            MoveImagesX(this.animationSpeed);
            this.currentOffsetX += this.animationSpeed;
        }
        // toggle is set to false, but hasn't reached the "false" position yet
        else if (!this.isOn && this.currentOffsetX > 0)
        {
            // move toggle towards the "false" position
            MoveImagesX(-this.animationSpeed);
            this.currentOffsetX -= this.animationSpeed;
        }
        // toggle was moved too far to the left
        if (this.currentOffsetX < 0)
        {
            // set toggle to the starting position
            MoveImagesX(-this.currentOffsetX);
            this.currentOffsetX = 0;
        }
        // toggle was moved too far to the right
        else if (this.currentOffsetX > this.toggleOffsetX)
        {
            // set toggle to the "true"/right position
            MoveImagesX(this.toggleOffsetX - this.currentOffsetX);
            this.currentOffsetX = this.toggleOffsetX;
        }

        // (de-)activate the green/red on-/offImages according to the toggles value
        this.onImage.gameObject.SetActive(this.isOn || this.currentOffsetX != 0);
        this.offImage.gameObject.SetActive(!this.isOn || this.currentOffsetX < this.toggleOffsetX);
    }

    // move the toggle's images
    private void MoveImagesX(float amount)
    {
        this.onImage.rectTransform.anchoredPosition = this.onImage.rectTransform.anchoredPosition + new Vector2(amount, 0);
        this.offImage.rectTransform.anchoredPosition = this.offImage.rectTransform.anchoredPosition + new Vector2(amount, 0);
        this.handleImage.rectTransform.anchoredPosition = this.handleImage.rectTransform.anchoredPosition + new Vector2(amount, 0);
    }

    // invert the toggle's value
    public void Switch()
    {
        this.isOn = !this.isOn;
        if (this.isOn) this.onActivated.Invoke();
        else this.onDeactivated.Invoke();
    }
}