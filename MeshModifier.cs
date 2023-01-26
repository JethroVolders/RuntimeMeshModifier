using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UIElements;

namespace MyUtilities
{ 
    public class MeshModifier
    {
        public static List<Vector3> intersectionPoints { get; private set; } = new List<Vector3>();
        public static List<Vector3> newVertices1 { get; private set; } = new List<Vector3>();
        public static List<int> newTriangles1 { get; private set; } = new List<int>();
        public static List<Vector3> newVertices2 { get; private set; } = new List<Vector3>();
        public static List<int> newTriangles2 { get; private set; } = new List<int>();

        public static List<Vector3> NANVerts = new List<Vector3>();
        public static void Cut(Mesh mesh, MyPlane plane, out Mesh mesh1, out Mesh mesh2)
        {
            mesh1 = new Mesh();
            mesh2 = new Mesh();
            intersectionPoints = new List<Vector3>();
            newVertices1 = new List<Vector3>();
            newTriangles1 = new List<int>();
            newVertices2 = new List<Vector3>();
            newTriangles2 = new List<int>();
            NANVerts = new List<Vector3>();
            var triangles = mesh.triangles;
            // Step 1: divide the mesh vertices into 2 group, in front and behind the plane
            for (int i = 0; i < mesh.vertexCount; i++)
            {
                Vector3 v = mesh.vertices[i];
                Vector3 delta = plane.position - v;
                float dot = Vector3.Dot(delta.normalized, plane.normal.normalized);
                if (dot < 0)
                {
                    newVertices1.Add(v);
                }
                else if(dot > 0)
                {
                    newVertices2.Add(v);
                }
                else if(dot == 0)
                {
                    newVertices1.Add(v);
                    newVertices2.Add(v);
                }
            }
            if (newVertices1.Count + newVertices2.Count > mesh.vertexCount)
            {
                Debug.LogError("The total number of vertices in newVertices1 and newVertices2 exceeds the original vertex count!");
                return;
            }

            //Step 2: Iterate through the triangles and check if they are intersected by the plane
            for (int h = 0; h < triangles.Length; h += 3)
            {
                var p1 = mesh.vertices[triangles[h]];
                var p2 = mesh.vertices[triangles[h + 1]];
                var p3 = mesh.vertices[triangles[h + 2]];
                Vector3 interPoint1 = Vector3.zero;
                Vector3 interPoint2 = Vector3.zero;

                bool isOnOneSide = (newVertices1.Contains(p1) && newVertices1.Contains(p2) && newVertices1.Contains(p3))
                || (newVertices2.Contains(p1) && newVertices2.Contains(p2) && newVertices2.Contains(p3));

                if (!isOnOneSide)
                {
                    // This triangle is intersected by the plane
                    // Step 3: Calculate the intersection point
                    Vector3 oppositeVert = Vector3.zero;
                    Vector3 sameSideVert1 = Vector3.zero;
                    Vector3 sameSideVert2 = Vector3.zero;
                    Vector3 edge1 = Vector3.zero;
                    Vector3 edge2 = Vector3.zero;

                    //which edges are intersected
                    if (newVertices1.Contains(p1) && !newVertices1.Contains(p2) && !newVertices1.Contains(p3)
                        || newVertices2.Contains(p1) && !newVertices2.Contains(p2) && !newVertices2.Contains(p3))
                    {
                        edge1 = p2 - p1;
                        edge2 = p3 - p1;
                        oppositeVert = p1;
                        sameSideVert1 = p2;
                        sameSideVert2 = p3;
                    }
                    else if(newVertices1.Contains(p2) && !newVertices1.Contains(p1) && !newVertices1.Contains(p3)
                        || newVertices2.Contains(p2) && !newVertices2.Contains(p1) && !newVertices2.Contains(p3))
                    {
                        edge1 = p1 - p2;
                        edge2 = p3 - p2;
                        oppositeVert = p2;
                        sameSideVert1 = p1;
                        sameSideVert2 = p3;
                    }
                    else if (newVertices1.Contains(p3) && !newVertices1.Contains(p2) && !newVertices1.Contains(p1)
                        || newVertices2.Contains(p3) && !newVertices2.Contains(p2) && !newVertices2.Contains(p1))
                    {
                        edge1 = p2 - p3;
                        edge2 = p1 - p3;
                        oppositeVert = p3;
                        sameSideVert1 = p2;
                        sameSideVert2 = p1;
                    }
                    else
                    {
                        NANVerts.Add(p1);
                        NANVerts.Add(p2);
                        NANVerts.Add(p3);
                        continue;
                    }

                    Debug.DrawLine(oppositeVert, sameSideVert1, Color.cyan, Mathf.Infinity);
                    Debug.DrawLine(oppositeVert, sameSideVert2, Color.magenta, Mathf.Infinity);
                    bool isTriangleOnPositiveSide = newVertices1.Contains(oppositeVert);

                    float dot = Vector3.Dot(plane.normal, edge1);
                    float d = Vector3.Dot(plane.normal, oppositeVert);
                    float t = (plane.distance - d) / dot; 
                    interPoint1 = oppositeVert + (t * edge1);

                    dot = Vector3.Dot(plane.normal, edge2);
                    t = (plane.distance - d) / dot;
                    interPoint2 = oppositeVert + (t * edge2);

                    intersectionPoints.Add(interPoint1);
                    intersectionPoints.Add(interPoint2);
                    // Step 4: insert the new verts and new triangles
                    if(newVertices1.Contains(oppositeVert))
                    {
                        newVertices1.Insert(newVertices1.IndexOf(oppositeVert), interPoint1);
                        newVertices1.Insert(newVertices1.IndexOf(oppositeVert), interPoint2);
                        
                        newVertices2.Insert(newVertices2.IndexOf(sameSideVert1), interPoint1);
                        newVertices2.Insert(newVertices2.IndexOf(sameSideVert2), interPoint1);
                        newVertices2.Insert(newVertices2.IndexOf(sameSideVert2), interPoint2);
                        
                        newTriangles1.Add(newVertices1.IndexOf(oppositeVert));
                        newTriangles1.Add(newVertices1.IndexOf(interPoint1));
                        newTriangles1.Add(newVertices1.IndexOf(interPoint2));
                    }
                    else
                    {
                        newVertices2.Insert(newVertices2.IndexOf(oppositeVert), interPoint1);
                        newVertices2.Insert(newVertices2.IndexOf(oppositeVert), interPoint2);
                        
                        newVertices1.Insert(newVertices1.IndexOf(sameSideVert1), interPoint1);
                        newVertices1.Insert(newVertices1.IndexOf(sameSideVert2), interPoint1);
                        newVertices1.Insert(newVertices1.IndexOf(sameSideVert2), interPoint2);
                        
                        newTriangles2.Add(newVertices2.IndexOf(sameSideVert1));
                        newTriangles2.Add(newVertices2.IndexOf(interPoint1));
                        newTriangles2.Add(newVertices2.IndexOf(interPoint2));

                        newTriangles2.Add(newVertices2.IndexOf(sameSideVert2));
                        newTriangles2.Add(newVertices2.IndexOf(sameSideVert1));
                        newTriangles2.Add(newVertices2.IndexOf(interPoint2));
                    }
                }
                else
                {
                    if (newVertices1.Contains(p1))
                    {
                        // add indices of triangleVertices to newTriangles1
                        newTriangles1.Add(newVertices1.IndexOf(p1));
                        newTriangles1.Add(newVertices1.IndexOf(p2));
                        newTriangles1.Add(newVertices1.IndexOf(p3));
                    }
                    else
                    {
                        // add indices of triangleVertices to newTriangles2
                        newTriangles2.Add(newVertices2.IndexOf(p1));
                        newTriangles2.Add(newVertices2.IndexOf(p2));
                        newTriangles2.Add(newVertices2.IndexOf(p3));
                    }
                }
            }

            // Step 5: Create and set the new meshes
            mesh1.vertices = newVertices1.ToArray();
            mesh2.vertices = newVertices2.ToArray();
            mesh1.triangles = newTriangles1.ToArray();
            mesh2.triangles = newTriangles2.ToArray();

            mesh1.RecalculateNormals();
            mesh2.RecalculateNormals();
        }
    }
}

public struct MyPlane
{
    internal const int size = 16;
    
    private Vector3 m_Position;

    private Vector3 m_Normal;

    private float m_Distance;

    public Vector3 position
    {
        get
        {
            return m_Position;
        }
        set
        {
            m_Position = value;
        }
    }
    public Vector3 normal
    {
        get
        {
            return m_Normal;
        }
        set
        {
            m_Normal = value;
        }
    }
    public float distance
    {
        get
        {
            return m_Distance;
        }
        set
        {
            m_Distance = value;
        }
    }

    public MyPlane flipped => new MyPlane(m_Position, -m_Normal, 0f - m_Distance);

    public MyPlane(Vector3 inNormal, Vector3 inPoint)
    {
        m_Normal = Vector3.Normalize(inNormal);
        m_Distance = 0f - Vector3.Dot(m_Normal, inPoint);
        m_Position = inPoint;
    }
   
    public MyPlane(Vector3 inPosition, Vector3 inNormal, float d)
    {
        m_Position = inPosition;
        m_Normal = Vector3.Normalize(inNormal);
        m_Distance = d;
    }

    public MyPlane(Vector3 inPosition, Vector3 a, Vector3 b, Vector3 c)
    {
        m_Position = inPosition;
        m_Normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
        m_Distance = 0f - Vector3.Dot(m_Normal, a);
    }

    public void SetNormalAndPosition(Vector3 inNormal, Vector3 inPoint)
    {
        m_Normal = Vector3.Normalize(inNormal);
        m_Distance = 0f - Vector3.Dot(m_Normal, inPoint);
    }

    public void Set3Points(Vector3 a, Vector3 b, Vector3 c)
    {
        m_Normal = Vector3.Normalize(Vector3.Cross(b - a, c - a));
        m_Distance = 0f - Vector3.Dot(m_Normal, a);
    }

    public void Flip()
    {
        m_Normal = -m_Normal;
        m_Distance = 0f - m_Distance;
    }

    public void Translate(Vector3 translation)
    {
        m_Distance += Vector3.Dot(m_Normal, translation);
    }

    public static MyPlane Translate(MyPlane plane, Vector3 translation)
    {
        return new MyPlane(plane.position, plane.m_Normal, plane.m_Distance += Vector3.Dot(plane.m_Normal, translation));
    }

    public Vector3 ClosestPointOnPlane(Vector3 point)
    {
        float num = Vector3.Dot(m_Normal, point) + m_Distance;
        return point - m_Normal * num;
    }

    public float GetDistanceToPoint(Vector3 point)
    {
        return Vector3.Dot(m_Normal, point) + m_Distance;
    }

    public bool GetSide(Vector3 point)
    {
        return Vector3.Dot(m_Normal, point) + m_Distance > 0f;
    }

    public bool SameSide(Vector3 inPt0, Vector3 inPt1)
    {
        float distanceToPoint = GetDistanceToPoint(inPt0);
        float distanceToPoint2 = GetDistanceToPoint(inPt1);
        return (distanceToPoint > 0f && distanceToPoint2 > 0f) || (distanceToPoint <= 0f && distanceToPoint2 <= 0f);
    }

    public bool Raycast(Ray ray, out float enter)
    {
        float num = Vector3.Dot(ray.direction, m_Normal);
        float num2 = 0f - Vector3.Dot(ray.origin, m_Normal) - m_Distance;
        if (Mathf.Approximately(num, 0f))
        {
            enter = 0f;
            return false;
        }

        enter = num2 / num;
        return enter > 0f;
    }

    public override string ToString()
    {
        return ToString(null, null);
    }

    public string ToString(string format)
    {
        return ToString(format, null);
    }

    public string ToString(string format, IFormatProvider formatProvider)
    {
        if (string.IsNullOrEmpty(format))
        {
            format = "F2";
        }

        if (formatProvider == null)
        {
            formatProvider = CultureInfo.InvariantCulture.NumberFormat;
        }

        return String.Format("(normal:{0}, distance:{1})", m_Normal.ToString(format, formatProvider), m_Distance.ToString(format, formatProvider));
    }
}


    
