﻿using cakeslice;
using Pathfinding;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DentedPixel;

public class PlayerMovementManager : MonoBehaviour
{

    private static object _lock = new object();
    private static PlayerMovementManager _instance;
    public static PlayerMovementManager Instance
    {
        get
        {
            lock (_lock)
            {
                if (!_instance)
                {
                    var inst = FindObjectOfType(typeof(PlayerMovementManager)) as PlayerMovementManager;
                    _instance = inst ? inst : new GameObject().AddComponent<PlayerMovementManager>();
                }
            }
            return _instance;
        }
    }

    private struct BoolWrapper
    {
        public bool val { get; set; }
    }

    // Static pool of quads from which to pull for outline purposes.
    private static GameObject[] quads = new GameObject[100];
    private static int quadsInUse = 0;

    private Transform selected { get; set; }
    public ICharacterStats selectedCharacterStats;

    private int range;
    [SerializeField]
    private float cursorHeight = 3f;
    [SerializeField]
    private Material mat;
    [SerializeField]
    private int highlightedColor = 1; // This should correspond to 0, 1, or 2 in the camera's outlineEffect component.

    private List<GraphNode> nodes = null;

    private Mesh cursor;

    private Vector3 cursorPosition;

    private int[] cursorCoordinates = { 0, 0 };

    private Outline lastUpdateOutline;

    private int notHighlightedColor;

    private bool controlsEnabled = true; // Is the character moving? If so, lock controls and turn off quads.
    private ABPath path;
    private float moveSpeed = 5f;

    void Awake()
    {
        Vector3 quadRotation = new Vector3(90, 0, 0);
        for (int i = 0; i < 100; i++)
        {
            var quad = PrimitiveHelper.CreatePrimitive(PrimitiveType.Quad, false);
            var col = quad.AddComponent<BoxCollider>();
            col.size = new Vector3(1, .01f, 1);
            col.isTrigger = true;
            quad.layer = LayerMask.NameToLayer("Outline");
            var renderer = quad.GetComponent<Renderer>();
            renderer.enabled = false;
            renderer.material = mat ? mat : Resources.Load<Material>("Default");
            quad.AddComponent<Outline>().Renderer.sharedMaterials[0] = mat;
            quad.transform.localScale *= .9f;

            quad.SetActive(false);
            quad.transform.Rotate(quadRotation);
            quads[i] = quad;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (selected)
        {
            if (selected)
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (lastUpdateOutline) lastUpdateOutline.color = notHighlightedColor;
                if (Physics.Raycast(ray, out hit, Camera.main.farClipPlane, 1 << LayerMask.NameToLayer("Outline")))
                {
                    Outline outline = hit.transform.GetComponent<Outline>();
                    if (lastUpdateOutline) lastUpdateOutline.color = notHighlightedColor;
                    outline.color = highlightedColor;
                    lastUpdateOutline = outline;

                    if (controlsEnabled && Input.GetMouseButtonDown(0))
                    {
                        Vector3 hitPos = AstarData.active.GetNearest(hit.point).position;
                        if (!Physics.Raycast(new Ray(hitPos, Vector3.up), 1, 1 << LayerMask.NameToLayer("Player"))) // Check if occupied.
                        {
                            var path = PathManager.Instance.getPath(selected.transform.position, hitPos, PathManager.CharacterFaction.ALLY);
                            this.path = path;
                            StartCoroutine(MoveCharacter(path));
                        }
                        else
                        {
                            Debug.Log("Space occupied.");
                        }
                    }

                }
            }
        }
        #if DEBUG
        if (path != null)
        {
            for (int i = 0; i < path.vectorPath.Count - 1; i++)
            {
                Debug.DrawLine(path.vectorPath[i], path.vectorPath[i + 1], Color.red);
            }
        }
        #endif
    }

    private IEnumerator MoveCharacter(ABPath path)
    {
        #if DEBUG
        for (int i = 0; i < path.vectorPath.Count - 1; i++)
        {
            Debug.DrawLine(path.vectorPath[i], path.vectorPath[i + 1], Color.red);
        }
        #endif
        if (path.path.Count < 2)
        {
            throw new System.ArgumentException("Path must be at least length 2");
        }

        var finished = false;
        var positionEnumeration = (from node in path.path
                                  orderby path.path.IndexOf(node)
                                  select (Vector3)node.position).ToArray();
        var arr = new Vector3[positionEnumeration.Count() + 2];
        positionEnumeration.CopyTo(arr, 1);
        arr[0] = arr[1];
        arr[arr.Length - 1] = arr[arr.Length - 2];
        var spline = new LTSpline(arr);


        LeanTween.moveSpline(selected.gameObject, spline, spline.distance / moveSpeed).
            setOnComplete(() => finished = true). // May want to fiddle with animation states here.
            setEase(LeanTweenType.linear).
            setOrientToPath(true); 
        //.setOnStart()

        yield return new WaitUntil(() => finished);
    }

    public void Select(Transform t, ICharacterStats stats)
    {
        selectedCharacterStats = stats;
        range = selectedCharacterStats.GetMovementRange();

        Debug.Log("Character name is now:" + stats.getCharacterName());

        for (int i = 0; i < quads.Length; i++)
        {
            quads[i].SetActive(false);
        }
        cursorPosition = new Vector3(t.position.x, 3, t.position.z);
        cursorCoordinates = new int[] { 0, 0 };
        Debug.Log("select");
        // Get list of traversable nodes within range.
        nodes = PathUtilities.BFS(AstarData.active.GetNearest(t.position).node, range);
        // Shouldn't ever need too many quads.
        int count = 0;
        if (nodes.Count > quads.Length)
        {
            throw new System.Exception("Too many quads are required for the current range");
        }
        foreach (var node in nodes)
        {
            quads[count].SetActive(true);
            quads[count].transform.position = (Vector3)node.position + new Vector3(0, .01f, 0);
            count++;
        }
        selected = t;
    }
}
