using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CoordinateSysTransform : MonoBehaviour
{
    [SerializeField]
    private TMPro.TextMeshProUGUI debugText;
    [SerializeField]
    private GameObject target;

    private Camera mainCamera;

    // Start is called before the first frame update
    void Start()
    {
        mainCamera = Camera.main;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public bool InFrontOfCamera(Transform t, Camera cam)
    {
        return Vector3.Dot(cam.transform.forward, t.position - cam.transform.position) > 0;
    }
}
