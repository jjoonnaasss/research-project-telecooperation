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

// script implementing tooltips for the controller-buttons
public class Tooltip : MonoBehaviour
{
    [SerializeField] private Transform lineTarget;
    [SerializeField] private Vector3 lineSourceOffset;
    [SerializeField] private Vector3 lineTargetOffset;
    [SerializeField] private LineRenderer lineRenderer;
    [SerializeField] private TMP_Text textField;

    private RectTransform canvasRect;

    void Start()
    {
        this.canvasRect = GetComponent<RectTransform>();
    }

    private void Update()
    {
        this.AlignLineRenderer();
    }

    // update line renderer positions to make sure the tooltip stays attached to its button (lineTarget)
    private void AlignLineRenderer()
    {
        Vector3 lineSource = this.canvasRect.position + this.lineSourceOffset.x * this.canvasRect.right + this.lineSourceOffset.y * this.canvasRect.up + this.lineSourceOffset.z * this.canvasRect.forward;
        Vector3 lineTargetPos = this.lineTarget.position + this.lineTargetOffset.x * this.lineTarget.right + this.lineTargetOffset.y * this.lineTarget.up + this.lineTargetOffset.z * this.lineTarget.forward;
        this.lineRenderer.SetPositions(new Vector3[] { lineSource, lineTargetPos });
    }

    // set content of the tooltip's text field
    public void SetTextContent(string content)
    {
        this.textField.text = content;
    }

    // update tooltip visualization in the editor
    void OnDrawGizmosSelected()
    {

#if UNITY_EDITOR
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(GetComponent<RectTransform>().position + this.lineSourceOffset, 0.001f);
        Gizmos.color = Color.white;

        if (this.lineTarget && this.lineRenderer)
        {
            RectTransform canvasRect = this.GetComponent<RectTransform>();
            Vector3 lineSource = canvasRect.position + this.lineSourceOffset.x * canvasRect.right + this.lineSourceOffset.y * canvasRect.up + lineSourceOffset.z * canvasRect.forward;
            Vector3 lineTargetPos = this.lineTarget.position + this.lineTargetOffset.x * this.lineTarget.right + this.lineTargetOffset.y * this.lineTarget.up + this.lineTargetOffset.z * this.lineTarget.forward;
            this.lineRenderer.SetPositions(new Vector3[] { lineSource, lineTargetPos });
        }
#endif
    }
}
