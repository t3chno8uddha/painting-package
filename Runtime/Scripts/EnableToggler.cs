using UnityEngine;

public class EnableToggler : MonoBehaviour
{
    [SerializeField] GameObject[] objects;

    void OnEnable()
    {
        foreach (GameObject obj in objects)
        {
            obj.SetActive(false);
        }       
    }
}