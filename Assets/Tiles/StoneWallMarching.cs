﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu]
public class StoneWallMarching : RuleTile<StoneWallMarching.Neighbor> {

    public GameObject o0000;
    public GameObject o0001;
    public GameObject o0011;
    public GameObject o0111;
    public GameObject o1111;
    public GameObject o0101;

    public class Neighbor : RuleTile.TilingRule.Neighbor {
        public const int Null = 3;
        public const int NotNull = 4;
    }

    public static Dictionary<byte, int> neighbors = new Dictionary<byte, int>();
    static Dictionary<Vector3Int, byte> neighboring = new Dictionary<Vector3Int, byte>()
    {
        {new Vector3Int( 0, 1,0),0},
        {new Vector3Int( 1, 1,0),1},
        {new Vector3Int( 1, 0,0),2},
        {new Vector3Int( 1,-1,0),3},
        {new Vector3Int( 0,-1,0),4},
        {new Vector3Int(-1,-1,0),5},
        {new Vector3Int(-1, 0,0),6},
        {new Vector3Int(-1, 1,0),7},
    };

    static Dictionary<byte, GameObject> squarePatterns = new Dictionary<byte, GameObject>();
    static ITilemap CachedMap = null; 
    static Tilemap CachedBehaviour = null; 
    static Transform CachedTilemapLocation = null;
    static Vector3Int CachedTowardCamera;
    static byte[] corners = new byte[4];
    static Vector3[] cornerLocs = new Vector3[] {
        new Vector3Int(1,1,0),
        new Vector3Int(-1,1,0),
        new Vector3Int(-1,-1,0),
        new Vector3Int(1,-1,0),
    };
    static Vector3 CachedCellSize;
    static Vector3 center = new Vector3(.5f, .5f, 0);
    public override bool RuleMatches(TilingRule rule, Vector3Int position, ITilemap tilemap, ref Matrix4x4 transform)
    {
        Debug.Log($"I start {position}");
        if (tilemap != CachedMap)
            squarePatterns.Clear();
        if (!squarePatterns.Any())
        {
            CachedBehaviour = tilemap.GetComponent<Tilemap>();
            CachedTilemapLocation = CachedBehaviour.transform;
            CachedCellSize = CachedBehaviour.cellSize;
            CachedTowardCamera = new Vector3Int(0,0, CachedBehaviour.transform.position.z - Camera.main.transform.position.z < 0 ? 1:-1);
            Debug.Log($"I found things for this tilemap");
            squarePatterns.Add(0b_0000_0000, o0000);
            squarePatterns.Add(0b_0000_0001, o0001);
            squarePatterns.Add(0b_0000_0011, o0011);
            squarePatterns.Add(0b_0000_0111, o0111);
            squarePatterns.Add(0b_0000_0101 , o0101);
        }
        neighbors.Clear();
        var badNeighbors = rule.GetNeighbors();

        //bit arr of neighbors
        //7 0 1
        //6   2
        //5 4 3

        byte myBytes = 0;
        byte neighborByte = 1;
        
        var enume = badNeighbors.GetEnumerator();
        while (enume.MoveNext())
        {
            var pos = enume.Current.Key - position;
            if(Mathf.Abs(pos.x) <= 1 && Mathf.Abs(pos.y) <= 1)
                myBytes += (byte)(neighborByte << neighboring[pos]);
        }
        Debug.Log($"at {position} but really {CachedBehaviour.GetCellCenterLocal(position)}\n{myBytes} (should be anything a byte can be 0-255)");

        //made bit arr
        //now make four corners for marching squares shape resolution

        //7 0  0 1
        //6 -  - 2
        //
        //6 -  - 2
        //5 4  4 3
        for (int i = 0, b = 0; i < 4; i++, b += 2)//8,6,4,2
        {
            corners[i] = (byte)(((myBytes << b) | (byte)((uint)myBytes >> (8 - b))) & 0b_0000_0111); //get surrounding //looping left shift... cast to uint so that right shift fills with 0.
            corners[i] |= 0b_000_1000;//include self
            corners[i] = (byte)(((corners[i] << i) | (byte)((uint)corners[i] >> (4 - i))) & 0b_0000_1111); //rotate 
            Debug.Log($"corner {i} is {corners[i]} (should be 0-15)");
        }

        //now each corner is rotated to the same orientation relative to the whole block.
        //every square spatially is represented as binary values with bit indexes as below
        //3 0  3 0
        //2 1  2 1
        //
        //3 0  3 0
        //2 1  2 1

        //the order of squares in the corners arr is currently 
        //1 0
        //2 3
        //corners = {0,1,2,3}


        //alright, time for the fun part. figure out which game object each square relates to.
        byte check = 0;
        GameObject go;
        for (int i = 0; i < 4; i++)
        {
            int r = 0;
            while (!squarePatterns.ContainsKey(check))
                check = (byte)(((corners[i] << r) | (byte)((uint)corners[i] >> (4 - r++))) & 0b_0000_1111);

            go = squarePatterns[check];
            //instantiate the game object at tile position plus the right transform to center on the correct portion of the square, rotate by r*90
            go = Instantiate<GameObject>(go, CachedBehaviour.transform);
            go.transform.localPosition = CachedBehaviour.GetCellCenterLocal(position) + Vector3.Scale(CachedCellSize, (center - CachedBehaviour.tileAnchor + .25f * cornerLocs[i]));
            go.transform.localRotation = Quaternion.AngleAxis(-r * 90, CachedTowardCamera);
            go.transform.localScale = Vector3.Scale(Vector3.one / 2, CachedCellSize); //assume GameObject is 1 unit scale. because standards. depending on usage, cachedCellSize may shift to a vec3 of the smallest aspect of the CachedCellSize // just aspect ratio things
            go.name = "3d" + position.ToString() + "(" + i + ")";
        }
        Debug.Log($"I finish {position}");

        //i think the math is all there so lets see what happens.
        //if this works on my first try, i'll shit my pants.
        return base.RuleMatches(rule, position, tilemap, ref transform);
    }

}
