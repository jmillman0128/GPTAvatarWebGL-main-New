using UnityEngine;

public class MenuTracking : MonoBehaviour
{
    public Transform player;
    public float distance = 1.0f;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Vector3 menuPosition = player.position;
        transform.position = menuPosition;
        transform.rotation = player.rotation;
    }
}
