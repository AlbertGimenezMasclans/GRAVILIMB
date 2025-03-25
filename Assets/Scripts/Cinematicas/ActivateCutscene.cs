using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class ActivateCutscene : MonoBehaviour
{
    public PlayableDirector playabledirector;
    public GameObject gameObject;
    public float DesactiveDirector;

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            if (playabledirector != null)
            {
                playabledirector.Play();
                StartCoroutine(Disable());
            }
        }
    }

    private IEnumerator Disable()
    {
        yield return new WaitForSeconds(DesactiveDirector);
        gameObject.SetActive(false);
    }
}
