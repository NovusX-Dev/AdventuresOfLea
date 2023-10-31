using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class AI : MonoBehaviour
{
    [SerializeField] float _speed = 10f;

    NavMeshAgent _navAgent;
    Camera _camera;

    private void Awake()
    {
        _navAgent = GetComponent<NavMeshAgent>();
        _camera = Camera.main;
    }

    void Start()
    {
        
    }

    void Update()
    {
        if(Input.GetMouseButton(0))
        {
            Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if(Physics.Raycast(ray, out hit))
            {
                _navAgent.destination = hit.point;
                _navAgent.speed = _speed * Time.deltaTime;
            }
        }
    }
}
