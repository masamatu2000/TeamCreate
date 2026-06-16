using UnityEngine;
using UnityEngine.InputSystem;

public class Police : MonoBehaviour
{
    Transform policeTransform;
    Renderer policeRenderer;

    bool isPoliceSelected = false;
    Color defaultColor;

    void Start()
    {
        policeTransform = transform;
        policeRenderer = GetComponent<Renderer>();

        if (policeRenderer != null)
        {
            defaultColor = policeRenderer.material.color;
        }
    }

    void Update()
    {
        
        if (!Mouse.current.leftButton.wasPressedThisFrame)
        {
            return;
        }

        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = Camera.main.ScreenPointToRay(mousePos);
        //範囲外で左クリックした場合は、処理を終了する
        if (!Physics.Raycast(ray, out RaycastHit hit))
        {
            return;
        }

        if (!isPoliceSelected)
        {
            if (hit.collider.CompareTag("Police"))
            {
                Debug.Log("Police Point " + hit.point);

                isPoliceSelected = true;

                if (policeRenderer != null)
                {
                    policeRenderer.material.color = Color.white;
                }
            }
        }
        else
        {
            if (hit.collider.CompareTag("Floor"))
            {
                Debug.Log("Floor Point " + hit.point);

                policeTransform.position =
                    new Vector3(hit.point.x, policeTransform.position.y, hit.point.z);

                isPoliceSelected = false;

                if (policeRenderer != null)
                {
                    policeRenderer.material.color = defaultColor;
                }
            }
        }
    }
}