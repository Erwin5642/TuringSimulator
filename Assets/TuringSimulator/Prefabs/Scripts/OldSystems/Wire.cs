using System.Collections;
using UnityEngine;
using System.Collections.Generic;

public class Wire : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public int curveResolution = 20;
    public float curveOffset = 1f;
    
    private WireConnector _startWireConnector;
    private WireConnector _endWireConnector;

    private LineRenderer _line;

    public float pulseSpeed = 0.5f;

    void Awake()
    {
        _line = GetComponent<LineRenderer>();
    }

    void Update()
    {
        List<Vector3> path = FindPath(startPoint.position, endPoint.position);

        _line.positionCount = path.Count;
        _line.SetPositions(path.ToArray());
    }

    List<Vector3> FindPath(Vector3 start, Vector3 end)
    {
        List<Vector3> path = new List<Vector3>();

        if (Physics.Linecast(start, end, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Module")) 
            {
                Vector3 dir = (end - start).normalized;

                // Base midpoint
                Vector3 midpoint = (start + end) / 2f;

                // Side vector perpendicular to the path
                Vector3 side = Vector3.Cross(dir, Vector3.up).normalized;

                // Control point offsets
                Vector3 p1 = start + side * curveOffset;
                Vector3 p2 = end + side * curveOffset;

                // Cubic Bézier: start (P0), p1 (P1), p2 (P2), end (P3)
                for (int i = 0; i <= curveResolution; i++)
                {
                    float t = i / (float)curveResolution;
                    float oneMinusT = 1f - t;

                    Vector3 point =
                        oneMinusT * oneMinusT * oneMinusT * start +
                        3f * oneMinusT * oneMinusT * t * p1 +
                        3f * oneMinusT * t * t * p2 +
                        t * t * t * end;

                    path.Add(point);
                }
                return path;
            }
        }
        
        path.Add(start);
        path.Add(end);
        return path;
    }
    
    public void DetachStart()
    {
        startPoint = null;
        _startWireConnector = null;
    }

    public void DetachEnd()
    {
        endPoint = null;
        _endWireConnector = null;
    }

    public void AttachStart(WireConnector wireConnector)
    {
        _startWireConnector = wireConnector;
        startPoint = _startWireConnector.transform;
    }

    public void AttachEnd(WireConnector wireConnector)
    {
        _endWireConnector = wireConnector;
        endPoint = _endWireConnector.transform;
    }

    public WireConnector GetEndConnector() => _endWireConnector;
    public WireConnector GetStartConnector() => _startWireConnector;

    public void PropagateSignal()
    {
        StartCoroutine(PulseCoroutine());
    }
    
    private IEnumerator PulseCoroutine()
    {
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * pulseSpeed;
            _line.material.color = Color.Lerp(Color.gray, Color.yellow, t);
            yield return null;
        }
        _line.material.color = Color.gray;
        
        _endWireConnector.PropagateSignal();
    }
}