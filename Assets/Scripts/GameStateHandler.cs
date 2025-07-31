using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;

public class GameStateHandler : MonoBehaviour
{
    [SerializeField] GameObject cam;
    [SerializeField] float rageBiteZoom = 3f;
    [SerializeField] float rageZoom = 5f;
    [SerializeField] float normalZoom = 8f;
    [SerializeField] float transitionDuration = 0.5f;

    public bool gameStarted;
     CinemachineCamera vcam;
    Coroutine currentTransition;
    

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
         vcam = cam.GetComponent<CinemachineCamera>();
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void StartGame()
    {

    }

    public void EndGame()
    {

    }

    public void AttachCamera(GameObject newObj)
    {
        vcam.Follow = newObj.transform;
        vcam.LookAt = newObj.transform;
    }
    
    public void RageBiteCamera()
    {
        if (currentTransition != null) StopCoroutine(currentTransition);
        currentTransition = StartCoroutine(RageBiteSequence());
    }

    IEnumerator RageBiteSequence()
    {
        // Slow down time
        Time.timeScale = 0.2f;

        // Zoom in hard
        yield return StartCoroutine(ZoomToSize(rageBiteZoom, transitionDuration));

        // Wait in real time
        yield return new WaitForSecondsRealtime(1f);

        // Restore time
        Time.timeScale = 1f;

        // Transition to Rage Camera
        RageCamera();
    }

    public void RageCamera()
    {
        if (currentTransition != null) StopCoroutine(currentTransition);
        currentTransition = StartCoroutine(ZoomToSize(rageZoom, transitionDuration));
    }

    public void NormalCamera()
    {
        if (currentTransition != null) StopCoroutine(currentTransition);
        currentTransition = StartCoroutine(ZoomToSize(normalZoom, transitionDuration));
    }

    IEnumerator ZoomToSize(float targetSize, float duration)
    {
        float startSize = vcam.Lens.OrthographicSize;
        float time = 0f;

        while (time < duration)
        {
            float t = time / duration;
            vcam.Lens.OrthographicSize = Mathf.Lerp(startSize, targetSize, Mathf.SmoothStep(0f, 1f, t));
            time += Time.unscaledDeltaTime;
            yield return null;
        }

        vcam.Lens.OrthographicSize = targetSize;
    }

}
