﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.Events;

public class Circuit : MonoBehaviour
{
    public static Circuit Instance;

    public LineRenderer[] ValidLines;

    public ReactOnTouch GnobPrefab;

    public QubitConnector QubitAttachPrefab;

    private List<ReactOnTouch> CircuitPositions = new List<ReactOnTouch>();

    private void Awake()
    {
        Instance = this;
        foreach (var line in ValidLines)
        {
            for (var i = 1; i < line.positionCount - 1; i++)
            {
                var gnob = GameObject.Instantiate(GnobPrefab, line.transform.TransformPoint(line.GetPosition(i)), Quaternion.identity, transform);
                gnob.Line = line;
                gnob.LinePos = i;
                gnob.RequestTwoQubits.AddListener(() => TwoQubitsRequested(gnob));
                CircuitPositions.Add(gnob);
            }
        }
    }

    private void TwoQubitsRequested(ReactOnTouch gnob)
    {
        StartCoroutine(HandleTwoQubits(gnob));
    }

    private IEnumerator HandleTwoQubits(ReactOnTouch gnob)
    {
        var validGnobs = new List<ReactOnTouch>();
        foreach (var curGnob in CircuitPositions)
        {
            if (curGnob.Line == gnob.Line)
            {
                curGnob.gameObject.SetActive(false);
            }
            else if (curGnob.LinePos != gnob.LinePos)
            {
                curGnob.gameObject.SetActive(false);
            }
            else if (!curGnob.Operator)
            {
                validGnobs.Add(curGnob);
            }
        }
        var connector = GameObject.Instantiate(QubitAttachPrefab, Vector3.Lerp(gnob.transform.position, CameraCache.Main.transform.position, 0.2F), Quaternion.identity, gnob.Operator.transform);
        connector.ChangeColor(gnob.Operator.GetComponent<Renderer>().sharedMaterial.color);
        // was qubit connector attached?
        while (validGnobs.All(n => !n.Operator))
        {
            connector.LineRoot.position = gnob.transform.position;
            yield return new WaitForEndOfFrame();
        }
        foreach (var curGnob in CircuitPositions)
        {
            curGnob.gameObject.SetActive(true);
        }
    }

    public bool CheckPosition(OperatorControl control, out Vector3 pos)
    {
        foreach (var line in ValidLines)
        {
            for (var i = 1; i < line.positionCount - 1; i++)
            {
                pos = line.transform.TransformPoint(line.GetPosition(i));
                if (control.IsInPos(pos))
                {
                    return true;
                }
            }
        }
        pos = Vector3.zero;
        return false;
    }

}
