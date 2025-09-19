using System;
using Unity.VisualScripting;
using UnityEngine;

public class ForceUp : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject collider2D;
    [SerializeField] private Rigidbody rb;

    [Header("Force")] 
    [SerializeField] private float forceUp;

    private void Update()
    {
        
    }
}
