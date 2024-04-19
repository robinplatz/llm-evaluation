using UnityEngine;

public class DropDown : MonoBehaviour
{
    public float floor = 0.0f;
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        // the cube moves to the y = 0 position over 1 second
        transform.position = Vector3.Lerp(transform.position, new Vector3(transform.position.x, floor, transform.position.z), Time.deltaTime);
        // if the cube is within 0.01 units of floor, disable this script
        if (Vector3.Distance(transform.position, new Vector3(transform.position.x, floor, transform.position.z)) < 0.01f)
        {
            enabled = false;
        }
    }
}
