using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Microsoft.MixedReality.Toolkit.UI;
using Microsoft.MixedReality.Toolkit.Utilities;
using UnityEngine;
using UnityEngine.Events;

public class Circuit : MonoBehaviour
{
    [Serializable]
    public class CheckCircuitLine
    {
        public string[] Gates_Operators;
    }

    [Serializable]
    public class CheckCircuit
    {
        public string Name;
        public CheckCircuitLine[] CircuitLines;
        public UnityEvent FoundEvent;

        public Texture2D HintImage;

        public KeyValuePair<UnityEvent, List<string>> GetToCheckLines()
        {
            var lines = new List<string>();
            foreach (var line in CircuitLines)
            {
                lines.Add(string.Join(";", line.Gates_Operators));
            }
            return new KeyValuePair<UnityEvent, List<string>>(FoundEvent, lines);
        }
    }

    [Serializable]
    public class ImageEvent : UnityEvent<Texture2D> { }

    public static Circuit Instance;

    public LineRenderer[] ValidLines;

    public LineRenderer[] ResultLines;

    public ReactOnTouch GnobPrefab;

    public QubitConnector QubitAttachPrefab;

    private ReactOnTouch[][] _circuitPositions;

    private Transform[][] _resultPositions;

    public CheckCircuit[] ValidCircuits;

    public ImageEvent HintRequest;

    private void Awake()
    {
        _circuitPositions = new ReactOnTouch[ValidLines.Length][];
        Instance = this;
        var lineIdx = 0;
        foreach (var line in ValidLines)
        {
            _circuitPositions[lineIdx] = new ReactOnTouch[line.positionCount - 2];
            for (var i = 1; i < line.positionCount - 1; i++)
            {
                var gnob = GameObject.Instantiate(GnobPrefab, line.transform.TransformPoint(line.GetPosition(i)), Quaternion.identity, transform);
                gnob.Line = line;
                gnob.LinePos = i;
                gnob.RequestTwoQubits.AddListener(() => TwoQubitsRequested(gnob));
                gnob.RequestResult.AddListener(() => ResultRequested(gnob));
                _circuitPositions[lineIdx][i - 1] = gnob;
            }
            lineIdx++;
        }
        _resultPositions = new Transform[ResultLines.Length][];
        lineIdx = 0;
        foreach (var line in ResultLines)
        {
            _resultPositions[lineIdx] = new Transform[line.positionCount - 2];
            for (var i = 1; i < line.positionCount - 1; i++)
            {
                var gO = new GameObject();
                gO.transform.parent = transform;
                gO.transform.position = line.transform.TransformPoint(line.GetPosition(i));
                _resultPositions[lineIdx][i - 1] = gO.transform;
            }
            lineIdx++;
        }
        StartCoroutine(DoCheckCircuit());
    }

    public void ShowHint()
    {
        if (ValidCircuits.Length < 1) return;
        HintRequest?.Invoke(ValidCircuits[0].HintImage);
    }

    public void ClearCircuit()
    {
        foreach (var circuit in _circuitPositions)
        {
            foreach (var pos in circuit)
            {
                if (pos.Operator)
                    Destroy(pos.Operator.gameObject);
            }
        }
    }

    private IEnumerator DoCheckCircuit()
    {
        while (this)
        {
            yield return new WaitForSeconds(1f);
            var lines = new List<string>();
            foreach (var circuitLine in _circuitPositions)
            {
                lines.Add(string.Join(";", (from gnob in circuitLine
                                            where gnob.Operator
                                            select gnob.Operator.gameObject.name).Reverse()));
            }
            foreach (var circuit in ValidCircuits)
            {
                var validLines = circuit.GetToCheckLines();
                var isValid = true;
                foreach (var line in validLines.Value)
                {
                    if (!lines.Contains(line))
                    {
                        isValid = false;
                        break;
                    }
                }
                if (isValid)
                {
                    validLines.Key?.Invoke();
                }
            }
        }
    }

    private void ResultRequested(ReactOnTouch gnob)
    {

        //find result position
        for (var i = 0; i < _circuitPositions.Length; i++)
        {
            var line = _circuitPositions[i];
            for (var y = 0; y < line.Length; y++)
            {
                if (line[y] == gnob)
                {
                    //position found!
                    var connector = GameObject.Instantiate(QubitAttachPrefab, _resultPositions[i][y].position, Quaternion.identity, gnob.Operator.transform);
                    connector.Text = "V";
                    connector.LineRoot.position = gnob.transform.position;
                    Destroy(connector.GetComponentInChildren<ObjectManipulator>());
                    return;
                }
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
        foreach (var curGnobLine in _circuitPositions)
        {
            foreach (var curGnob in curGnobLine)
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
        }
        var connector = GameObject.Instantiate(QubitAttachPrefab, Vector3.Lerp(gnob.transform.position, CameraCache.Main.transform.position, 0.2F), Quaternion.identity, gnob.Operator.transform);
        connector.ChangeColor(gnob.Operator.GetComponent<Renderer>().sharedMaterial.color);
        // was qubit connector attached?
        while (validGnobs.All(n => !n.Operator))
        {
            connector.LineRoot.position = gnob.transform.position;
            yield return new WaitForEndOfFrame();
        }
        foreach (var curGnobLine in _circuitPositions)
        {
            foreach (var curGnob in curGnobLine)
            {
                curGnob.gameObject.SetActive(true);
            }
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
