using UnityEngine;
using UnityEngine.InputSystem;
public class Police : MonoBehaviour
{
    Transform policeTransform ;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
       policeTransform  =transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();

            Ray ray = Camera.main.ScreenPointToRay(mousePos);

            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                if (hit.collider.CompareTag("Floor"))
                {
                    Debug.Log(hit.point);
                    policeTransform.position = new Vector3(hit.point.x, policeTransform.position.y, hit.point.z);
                }
               
                //policeTransform.position =new Vector3(hit.point.x, policeTransform.position.y, hit.point.z);
            }
        }
    }
}
