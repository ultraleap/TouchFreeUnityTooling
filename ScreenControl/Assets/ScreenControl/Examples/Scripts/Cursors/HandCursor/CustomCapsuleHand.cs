/******************************************************************************
 * Based on the Leap Motion Capsule Hands                                     *
 ******************************************************************************/

using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Leap.Unity.Attributes;
using Leap.Unity;
using Leap;

/** A basic Leap hand model constructed dynamically vs. using pre-existing geometry*/
public class CustomCapsuleHand : HandModelBase
{
    private const int TOTAL_JOINT_COUNT = 4 * 5;
    private const float CYLINDER_MESH_RESOLUTION = 0.1f; //in centimeters, meshes within this resolution will be re-used
    private const int THUMB_BASE_INDEX = (int)Finger.FingerType.TYPE_THUMB * 4;
    private const int PINKY_BASE_INDEX = (int)Finger.FingerType.TYPE_PINKY * 4;

    public Color jointColour;
    public Color boneColour;


    [SerializeField]
    private Chirality handedness;

    [SerializeField]
    private bool _showArm = true;

    [SerializeField]
    private bool _castShadows = true;

    [SerializeField]
    private Material _material;

    [SerializeField]
    private Mesh _sphereMesh;

    [MinValue(3)]
    [SerializeField]
    private int _cylinderResolution = 12;

    [MinValue(0)]
    [SerializeField]
    private float _jointRadius = 0.008f;

    [MinValue(0)]
    [SerializeField]
    private float _cylinderRadius = 0.006f;

    [MinValue(0)]
    [SerializeField]
    private float _palmRadius = 0.015f;

    private Material _sphereMat;

    private Hand _hand;
    private Vector3[] _spherePositions;

    public float handScale = 1;
    private float _handScale = 1;

    public override ModelType HandModelType
    {
        get
        {
            return ModelType.Graphics;
        }
    }

    public override Chirality Handedness
    {
        get
        {
            return handedness;
        }
        set { }
    }

    public override bool SupportsEditorPersistence()
    {
        return true;
    }

    public override Hand GetLeapHand()
    {
        return _hand;
    }

    public override void SetLeapHand(Hand hand)
    {
        _hand = hand;
    }

    public override void InitHand()
    {
        if (_material != null)
        {
            _sphereMat = new Material(_material);
            _sphereMat.hideFlags = HideFlags.DontSaveInEditor;
        }
    }

    private void OnValidate()
    {
        _meshMap.Clear();
    }

    public override void BeginHand()
    {
        base.BeginHand();
        _sphereMat.color = jointColour;
        _material.color = boneColour;
    }

    public void SetBoneColour(Color colour)
    {
        boneColour = colour;
        if (_material != null) _material.color = colour;
    }

    public void SetJointColour(Color colour)
    {
        jointColour = colour;
        if (_sphereMat != null) _sphereMat.color = colour;
    }

    public override void UpdateHand()
    {
        if (_spherePositions == null || _spherePositions.Length != TOTAL_JOINT_COUNT)
        {
            _spherePositions = new Vector3[TOTAL_JOINT_COUNT];
        }

        if (_sphereMat == null)
        {
            _sphereMat = new Material(_material);
            _sphereMat.hideFlags = HideFlags.DontSaveInEditor;
        }

        _handScale = Mathf.Max(0, handScale);

        //Update all joint spheres in the fingers
        foreach (var finger in _hand.Fingers)
        {
            for (int j = 0; j < 4; j++)
            {
                int key = getFingerJointIndex((int)finger.Type, j);

                Vector3 position = finger.Bone((Bone.BoneType)j).NextJoint.ToVector3();
                _spherePositions[key] = position;

                drawSphere(position, _sphereMat);
            }
        }

        //Now we just have a few more spheres for the hands
        //PalmPos, WristPos, and mockThumbJointPos, which is derived and not taken from the frame obj

        Vector3 palmPosition = _hand.PalmPosition.ToVector3();
        drawSphere(palmPosition, _palmRadius * _handScale, _sphereMat);

        Vector3 thumbBaseToPalm = _spherePositions[THUMB_BASE_INDEX] - _hand.PalmPosition.ToVector3();
        Vector3 mockThumbJointPos = _hand.PalmPosition.ToVector3() + Vector3.Reflect(thumbBaseToPalm, _hand.Basis.xBasis.ToVector3());
        drawSphere(mockThumbJointPos, _sphereMat);

        //If we want to show the arm, do the calculations and display the meshes
        if (_showArm)
        {
            var arm = _hand.Arm;

            Vector3 right = arm.Basis.xBasis.ToVector3() * arm.Width * 0.7f * 0.5f;
            Vector3 wrist = arm.WristPosition.ToVector3();
            Vector3 elbow = arm.ElbowPosition.ToVector3();

            float armLength = Vector3.Distance(wrist, elbow);
            wrist -= arm.Direction.ToVector3() * armLength * 0.05f;

            Vector3 armFrontRight = wrist + right;
            Vector3 armFrontLeft = wrist - right;
            Vector3 armBackRight = elbow + right;
            Vector3 armBackLeft = elbow - right;

            drawSphere(armFrontRight, _sphereMat);
            drawSphere(armFrontLeft, _sphereMat);
            drawSphere(armBackLeft, _sphereMat);
            drawSphere(armBackRight, _sphereMat);

            drawCylinder(armFrontLeft, armFrontRight);
            drawCylinder(armBackLeft, armBackRight);
            drawCylinder(armFrontLeft, armBackLeft);
            drawCylinder(armFrontRight, armBackRight);
        }

        //Draw cylinders between finger joints
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                int keyA = getFingerJointIndex(i, j);
                int keyB = getFingerJointIndex(i, j + 1);

                Vector3 posA = _spherePositions[keyA];
                Vector3 posB = _spherePositions[keyB];

                drawCylinder(posA, posB);
            }
        }

        //Draw cylinders between finger knuckles
        for (int i = 0; i < 4; i++)
        {
            int keyA = getFingerJointIndex(i, 0);
            int keyB = getFingerJointIndex(i + 1, 0);

            Vector3 posA = _spherePositions[keyA];
            Vector3 posB = _spherePositions[keyB];

            drawCylinder(posA, posB);
        }

        //Draw the rest of the hand
        drawCylinder(mockThumbJointPos, THUMB_BASE_INDEX);
        drawCylinder(mockThumbJointPos, PINKY_BASE_INDEX);
    }

    private void drawSphere(Vector3 position, Material material)
    {
        drawSphere(position, _jointRadius * _handScale, material);
    }

    private void drawSphere(Vector3 position, float radius, Material material)
    {
        //multiply radius by 2 because the default unity sphere has a radius of 0.5 meters at scale 1.
        Graphics.DrawMesh(_sphereMesh,
                          Matrix4x4.TRS(position,
                                        Quaternion.identity,
                                        Vector3.one * radius * 2.0f),
                          material, 0,
                          null, 0, null, _castShadows);
    }

    private void drawCylinder(Vector3 a, Vector3 b)
    {
        float length = (a - b).magnitude;

        Graphics.DrawMesh(getCylinderMesh(length),
                          Matrix4x4.TRS(a,
                                        Quaternion.LookRotation(b - a),
                                        new Vector3(_handScale, _handScale, 1)),
                          _material,
                          gameObject.layer,
                          null, 0, null, _castShadows);
    }

    private void drawCylinder(int a, int b)
    {
        drawCylinder(_spherePositions[a], _spherePositions[b]);
    }

    private void drawCylinder(Vector3 a, int b)
    {
        drawCylinder(a, _spherePositions[b]);
    }

    private int getFingerJointIndex(int fingerIndex, int jointIndex)
    {
        return fingerIndex * 4 + jointIndex;
    }

    private Dictionary<int, Mesh> _meshMap = new Dictionary<int, Mesh>();
    private Mesh getCylinderMesh(float length)
    {
        int lengthKey = Mathf.RoundToInt(length * 100 / CYLINDER_MESH_RESOLUTION);

        Mesh mesh;
        if (_meshMap.TryGetValue(lengthKey, out mesh))
        {
            return mesh;
        }

        mesh = new Mesh();
        mesh.name = "GeneratedCylinder";
        mesh.hideFlags = HideFlags.DontSave;

        List<Vector3> verts = new List<Vector3>();
        List<Color> colors = new List<Color>();
        List<int> tris = new List<int>();

        Vector3 p0 = Vector3.zero;
        Vector3 p1 = Vector3.forward * length;
        for (int i = 0; i < _cylinderResolution; i++)
        {
            float angle = (Mathf.PI * 2.0f * i) / _cylinderResolution;
            float dx = _cylinderRadius * Mathf.Cos(angle);
            float dy = _cylinderRadius * Mathf.Sin(angle);

            Vector3 spoke = new Vector3(dx, dy, 0);

            verts.Add(p0 + spoke);
            verts.Add(p1 + spoke);

            colors.Add(Color.green);
            colors.Add(boneColour);

            int triStart = verts.Count;
            int triCap = _cylinderResolution * 2;

            tris.Add((triStart + 0) % triCap);
            tris.Add((triStart + 2) % triCap);
            tris.Add((triStart + 1) % triCap);

            tris.Add((triStart + 2) % triCap);
            tris.Add((triStart + 3) % triCap);
            tris.Add((triStart + 1) % triCap);
        }

        mesh.SetVertices(verts);
        mesh.SetIndices(tris.ToArray(), MeshTopology.Triangles, 0);
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
        mesh.UploadMeshData(true);

        _meshMap[lengthKey] = mesh;

        return mesh;
    }
}
