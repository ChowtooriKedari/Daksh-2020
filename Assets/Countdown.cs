using UnityEngine;
using UnityEngine.SceneManagement;

public class Countdown : MonoBehaviour
{
    void Start()
    {
        // Only specifying the sceneName or sceneBuildIndex will load the Scene with the Single mode
        SceneManager.LoadScene("Earth", LoadSceneMode.Additive);
    }
}