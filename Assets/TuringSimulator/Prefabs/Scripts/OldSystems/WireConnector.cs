using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;


public class WireConnector : MonoBehaviour
{
    public GameObject wirePrefab;
    private XRSimpleInteractable _interactable;
    private PortData _port;
    private List<Wire> _connectedWires = new List<Wire>();
    private Wire _currentWire = null;
    private Module _module;

    public bool isOutputPort = true;
    private const float DetectionRadius = 0.2f;

    void Awake()
    {
        _interactable = GetComponent<XRSimpleInteractable>();
        _interactable.selectEntered.AddListener(OnGrab);
        _interactable.selectExited.AddListener(OnRelease);
        _port = GetComponent<PortData>();
        _module = GetComponentInParent<Module>();
    }

    public bool PropagateSignal()
    {
        if (isOutputPort)
        {
            if (_connectedWires.Count == 1)
            {
                _connectedWires[0].PropagateSignal();
                return true;
            }
        }
        else
        {
            if (_module != null)
            {
                _module.ReceiveSignal();
                return true;
            }
        }
        return false;
    }
    
    private void OnGrab(SelectEnterEventArgs args)
    {
        if (isOutputPort && _connectedWires.Count == 0)
        {
            GameObject wireObject = Instantiate(wirePrefab, transform);
            Wire wire = wireObject.GetComponent<Wire>();
            _currentWire = wire;
            wire.AttachStart(this);
            wire.endPoint = args.interactorObject.transform;
            _connectedWires.Add(wire);
        }
        else if (!isOutputPort && _connectedWires.Count > 0)
        {
            _currentWire = _connectedWires[0];
            _currentWire.DetachEnd();
            _connectedWires.RemoveAt(0);
            _currentWire.endPoint = args.interactorObject.transform;
        }
    }

    private void OnRelease(SelectExitEventArgs args)
    {
        if (_currentWire != null)
        {
            Transform hand = args.interactorObject.transform;
            Collider[] hits = Physics.OverlapSphere(hand.position, DetectionRadius);
    
            PortData closestValidPort = null;
            WireConnector closestWireConnector = null;
            float closestDist = float.MaxValue;

            foreach (var hit in hits)
            {
                var portCandidate = hit.GetComponent<PortData>();
                var wireConnectorCandidate = hit.GetComponent<WireConnector>();
                if (portCandidate == null || wireConnectorCandidate == null) continue;
                if (portCandidate == _port) continue;

                float dist = Vector3.Distance(hand.position, wireConnectorCandidate.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    closestValidPort = portCandidate;
                    closestWireConnector = wireConnectorCandidate;
                }
            }

            if (closestValidPort != null)
            {
                if ((closestValidPort.portType != PortType.Input && _port.portType != PortType.Input) ||
                    (_port.portType == PortType.Input && closestValidPort.portType != PortType.Input))
                {
                    Debug.LogWarning("Não é possivel conectar nesta porta");
                    Debug.Log(_port.portType);
                    Debug.Log(closestValidPort.portType);
                    if (_port.portType != PortType.Input)
                    {
                        _connectedWires.Remove(_currentWire);
                    }
                    else
                    {
                        _currentWire.GetStartConnector()._connectedWires.Remove(_currentWire);
                    }    
                    Destroy(_currentWire.gameObject);
                }
                else
                {
                    Debug.Log("Portas conectadas");
                    _currentWire.AttachEnd(closestWireConnector);
                    closestWireConnector._connectedWires.Add(_currentWire);    
                }
            }
            else
            {
                Debug.LogWarning("Nenhuma porta disponivel");
                if (_port.portType != PortType.Input)
                {
                    _connectedWires.Remove(_currentWire);
                }
                else
                {
                    _currentWire.GetStartConnector()._connectedWires.Remove(_currentWire);
                }
                Destroy(_currentWire.gameObject);
            }

            _currentWire = null;
        }
    }
    
    public int GetConnectedWireCount() => _connectedWires.Count;
    public Wire[] GetConnectedWires() => _connectedWires.ToArray();
    public Module GetModule() => _module;
}
