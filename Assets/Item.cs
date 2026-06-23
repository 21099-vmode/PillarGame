using UnityEngine;

public class Item : MonoBehaviour
{
    public string itemName = "Снежок"; 
    private Rigidbody _rb;
    private Collider _collider;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        _collider = GetComponent<Collider>();
    }

        public void PickUp(Transform holdPoint)
    {
        _rb.isKinematic = true;         _collider.enabled = false; 
        transform.SetParent(holdPoint);         transform.localPosition = Vector3.zero;         transform.localRotation = Quaternion.identity;     }

        public void Drop()
    {
        transform.SetParent(null);         _rb.isKinematic = false;         _collider.enabled = true;     }
}