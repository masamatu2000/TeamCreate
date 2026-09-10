using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class TitleSceneManager : MonoBehaviour
{
    private void Update()
    {
        bool startButtonPressed =
         Keyboard.current.spaceKey.wasPressedThisFrame ||
         Keyboard.current.aKey.wasPressedThisFrame ||
         Keyboard.current.bKey.wasPressedThisFrame ||
         Keyboard.current.cKey.wasPressedThisFrame;
        if (startButtonPressed)
        {
            SceneManager.LoadScene("RuleScene");
        }
    }
}