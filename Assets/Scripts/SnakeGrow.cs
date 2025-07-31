using System.Collections.Generic;
using UnityEngine;

public class SnakeGrow : MonoBehaviour
{
    [SerializeField] GameObject meta;
    //Saves Position For Body
    [SerializeField] public List<SnakeHistoryEntry> history = new List<SnakeHistoryEntry>();

    //Gap Between Each Body Part
    [SerializeField] public float gap = 0.5f;
    //Total Size
    [SerializeField] float size = 3f;
    //Min Distance For Position Update
    [SerializeField] float minDist = 0.05f;
    [SerializeField] public float sampleInterval = 0.02f;
    float timeSinceLastSample = 0f;
    public int segmentCount;
    [SerializeField] public  bool raging;


    [Header("Body Parts")]
    [SerializeField] GameObject snakeParent;
    [SerializeField] GameObject bodySegmentPrefab;
    [SerializeField] public List<Transform> bodyParts = new List<Transform>();

    [SerializeField] GameObject tail;
    [SerializeField] bool nearTail;
    [SerializeField] bool attached;

    [SerializeField] int minLoopSize = 17; // Min size to maintain loop
    [SerializeField] int shrinkRate = 1; // Units per second to shrink

    float shrinkingLength = 3f;

    public bool shrinkActive;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        meta = GameObject.FindGameObjectWithTag("Meta");
        segmentCount = Mathf.FloorToInt(size / gap);
        Grow(0);
    }

    // Update is called once per frame
    void Update()
    {
        
        segmentCount = Mathf.FloorToInt(size / gap);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (!attached && nearTail) AttachTail();
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            if (attached) DetachTail();
            
        }

        if (attached)
        {
            // Smoothly decrease the snake's length
            if(shrinkActive)
            {
            shrinkingLength -= shrinkRate * Time.deltaTime;
            size = shrinkingLength; 
            }
            // If the loop gets too small, detach
            if (shrinkingLength < minLoopSize)
            {
                DetachTail();
            }
        }

        UpdatePositionHistory();
        UpdateBodyParts();
    }

    public void Grow(float amount)
    {
        // Calculate targetCount based on the NEW total size
        int targetCount = Mathf.FloorToInt((size + amount) / gap);

        // Update both size variables to keep them in sync
        size += amount;
        shrinkingLength += amount;

        // Add new segments to match the new size
        while (bodyParts.Count < targetCount)
        {
            GameObject segment = Instantiate(bodySegmentPrefab);
            segment.transform.parent = snakeParent.transform;
            bodyParts.Insert(bodyParts.Count - 1, segment.transform);
        }
    }

    void UpdatePositionHistory()
    {
        timeSinceLastSample += Time.deltaTime;

        if (timeSinceLastSample >= sampleInterval)
        {
            history.Add(new SnakeHistoryEntry(transform.position, transform.rotation));
            timeSinceLastSample = 0f;
        }

        // Keep enough history for all body segments
        float totalTrailTime = bodyParts.Count * gap / 5; // speed in units/sec
        int maxHistory = Mathf.CeilToInt(totalTrailTime / sampleInterval);

        if (history.Count > maxHistory + 1000)
        {
            history.RemoveAt(0);
        }
    }


    void UpdateBodyParts()
{
    // A key assumption here is that your 'minDist' variable represents the 
    // distance the snake moves between each history sample. Adjust if needed.
    if (minDist <= 0) minDist = 0.01f; // Avoid division by zero
    float samplesPerGap = gap / minDist;

    if (attached)
    {
        // --- Shrinking Loop Logic ---

        // Calculate the tail's smooth position based on the shrinking length
        float tailDistanceInGaps = shrinkingLength / gap;
        float tailFloatHistoryIndex = (history.Count - 1) - (tailDistanceInGaps * samplesPerGap);
        SnakeHistoryEntry tailTarget = GetInterpolatedHistoryEntry(tailFloatHistoryIndex);
        
        // Assumes the 'tail' is the LAST transform in the 'bodyParts' list
        Transform tailTransform = bodyParts[bodyParts.Count - 1];
        tailTransform.position = tailTarget.position;
        tailTransform.rotation = tailTarget.rotation;

        // Lock the head to the tail to close the loop
        transform.position = tailTransform.position;
        transform.rotation = tailTransform.rotation;
        
        // Position all body segments (everything except the tail)
        for (int i = 0; i < bodyParts.Count - 1; i++)
        {
            float segmentFloatHistoryIndex = (history.Count - 1) - ((i + 1) * samplesPerGap);
            bodyParts[i].position = GetInterpolatedHistoryEntry(segmentFloatHistoryIndex).position;
            bodyParts[i].rotation = GetInterpolatedHistoryEntry(segmentFloatHistoryIndex).rotation;
        }

        // Clean up segments that are now "behind" the tail
        int requiredSegmentCount = Mathf.FloorToInt(shrinkingLength / gap);
        while (bodyParts.Count - 1 > requiredSegmentCount && bodyParts.Count > 1)
        {
            int indexToRemove = bodyParts.Count - 2; // The segment right before the tail
            Transform segment = bodyParts[indexToRemove];
            bodyParts.RemoveAt(indexToRemove);
            Destroy(segment.gameObject);
        }
    }
    else 
    {
        // --- Normal Snake Movement Logic ---
        for (int i = 0; i < bodyParts.Count; i++)
        {
            float segmentFloatHistoryIndex = (history.Count - 1) - ((i + 1) * samplesPerGap);
            SnakeHistoryEntry target = GetInterpolatedHistoryEntry(segmentFloatHistoryIndex);
            bodyParts[i].position = target.position;
            bodyParts[i].rotation = target.rotation;
        }
    }
}

    public void AttachTail()
    {
        attached = true;
        // Set the shrinking length to the current body length
        shrinkingLength = (bodyParts.Count - 1) * gap;
        GetComponent<SnakePlayerFollow>().enabled = false;
        meta.GetComponent<GameStateHandler>().NormalCamera();
        raging = false;
    }

    public void DetachTail()
    {
        attached = false;
        // After detaching, update the size to match the remaining segments
        size = (bodyParts.Count - 1) * gap;
        GetComponent<SnakePlayerFollow>().enabled = true;
        if (!shrinkActive) shrinkActive = true;

        //Start the Game if not
        if (meta)
        {
            GameStateHandler gameStateHandler = meta.GetComponent<GameStateHandler>();
            if (!gameStateHandler.gameStarted)
            {
                gameStateHandler.gameStarted = true;
                gameStateHandler.AttachCamera(this.gameObject);
            }
            gameStateHandler.RageBiteCamera();
            raging = true;
        }
    }

    SnakeHistoryEntry GetInterpolatedHistoryEntry(float floatIndex)
    {
        if (history.Count == 0)
        {
            return new SnakeHistoryEntry(transform.position, transform.rotation);
        }

        // Clamp index to be within the bounds of the history list
        floatIndex = Mathf.Clamp(floatIndex, 0, history.Count - 1);

        // Get the two integer indices surrounding our float index
        int lowerIndex = Mathf.FloorToInt(floatIndex);
        int upperIndex = Mathf.CeilToInt(floatIndex);

        // If indices are out of bounds or the same, return the single point
        if (lowerIndex < 0) return history[0];
        if (upperIndex >= history.Count) return history[history.Count - 1];
        if (lowerIndex == upperIndex) return history[lowerIndex];

        // Find the interpolation factor (e.g., if floatIndex is 10.7, t is 0.7)
        float t = floatIndex - lowerIndex;

        // Interpolate position and rotation using Lerp and Slerp for smooth results
        Vector3 position = Vector3.Lerp(history[lowerIndex].position, history[upperIndex].position, t);
        Quaternion rotation = Quaternion.Slerp(history[lowerIndex].rotation, history[upperIndex].rotation, t);

        return new SnakeHistoryEntry(position, rotation);
    }

    private void OnCollisionEnter2D(Collision2D other)
    {
        if (other.gameObject.tag == "Enemy")
        {
            GameObject enemy = other.gameObject;
            Grow(enemy.GetComponent<Enemy>().growthValue);
            enemy.GetComponent<Enemy>().PlayDeath();
        }

    }

    void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject == tail)
        {
            nearTail = true;
        }
    }

    void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject == tail)
        {
            nearTail = false;
        }
    }


}





[System.Serializable]
public struct SnakeHistoryEntry
{
    public Vector3 position;
    public Quaternion rotation;

    public SnakeHistoryEntry(Vector3 pos, Quaternion rot)
    {
        position = pos;
        rotation = rot;
    }
}