using UnityEngine;

public class LavaScroller : MonoBehaviour
{
    [Header("Скорость движения лавы")]
    public float scrollSpeedX = 0.05f; 
    public float scrollSpeedY = 0.05f; 

    private Renderer _renderer;
    private Vector2 _currentOffset = Vector2.zero;

    void Start()
    {
        _renderer = GetComponent<Renderer>();
    }

    void Update()
    {
                _currentOffset.x += scrollSpeedX * Time.deltaTime;
        _currentOffset.y += scrollSpeedY * Time.deltaTime;

                if (_renderer.material.HasProperty("_BaseMap"))
        {
            _renderer.material.SetTextureOffset("_BaseMap", _currentOffset);
        }
        
                if (_renderer.material.HasProperty("_EmissionMap"))
        {
            _renderer.material.SetTextureOffset("_EmissionMap", _currentOffset);
        }
    }
}