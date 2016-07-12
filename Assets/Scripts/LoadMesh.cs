﻿using UnityEngine;
using System.Collections;
using Ros_CSharp;
using System.Collections.Generic;
using System.Xml.Linq;
using System;
using System.IO;

public class LoadMesh : ROSMonoBehavior {

    public string RobotDescriptionParam = "";
    private string robotdescription;
    private Dictionary<string, Color?> materials = new Dictionary<string, Color?>();
    public XDocument RobotDescription { get; private set; }
    //private Dictionary<string, joint> joints = new Dictionary<string, joint>();
   // private Dictionary<string, link> links = new Dictionary<string, link>();

    private TfVisualizer tfviz
    {
        get
        {
            return transform.root.GetComponent<TfVisualizer>();
        }
    }


    //ros stuff
    protected string NameSpace = "";
    public void setNamespace(string _NameSpace)
    {
        NameSpace = _NameSpace;
    }

    internal NodeHandle nh;
    void Start () {
        rosmanager.StartROS(this, () => {
            nh = new NodeHandle();
        });
        Load();
    }
	
    //Written by Eric M.
    //Modified by Dalton C.
    public bool Load()
    {
        if (!NameSpace.Equals(string.Empty))
        {
            if (!RobotDescriptionParam.StartsWith("/"))
                RobotDescriptionParam = "/" + RobotDescriptionParam;
        }else
        {
            if (RobotDescriptionParam.StartsWith("/"))
                RobotDescriptionParam.TrimStart('/');
        }


        if (Param.get(NameSpace + RobotDescriptionParam, ref robotdescription))
        {
            return Parse();
        }
        return false;
    }

    private bool Parse(IEnumerable<XElement> elements = null)
    {

        //Written by Eric M. (Listener in ROS.NET samples)
        if (elements == null)
        {
            RobotDescription = XDocument.Parse(this.robotdescription);
            if (RobotDescription != null && RobotDescription.Root != null)
            {
                return Parse(RobotDescription.Elements());
            }
            return false;
        }
        elements = RobotDescription.Root.Elements();
       
        //written by Dalton C.
        //grab joints and links
        foreach (XElement element in elements)
        {
            if (element.Name == "material")
                handleMaterial(element); 

            if (element.Name == "link")
                handleLink(element);

            if (element.Name == "gazebo")
            {
                XElement link = element.Element("link");
                if(link != null)
                    handleLink(link);
            }

            /*
            if(element.Name == "joint")
                 handleJoint(element);
            */
        }
        /*
           due to how the code hase been changed its likely not necessary to maintaine joint and link obj's 
           It seems like maintaining a hierarchy is unecessary and just producing more work. This may be 
           brought back though if it is necessary for more complex urdfs. 
        */
        //handle hierarchy 
        /*
        foreach(KeyValuePair<string, link> curLink in links)
        {
            //curLink.Value.component.transform.parent = transform;
            
            if(!joints.ContainsKey(curLink.Key)) //if true, this is likely the root element
            {
                curLink.Value.component.transform.parent = transform;
                curLink.Value.component.transform.localPosition = new Vector3(0, 0, 0);
                curLink.Value.component.gameObject.SetActive(true);
                continue;
            }
            joint curJoint = joints[curLink.Key];
            link parentLink = links[curJoint.parent];
            Transform curTrans = curLink.Value.component.transform;
            //curTrans.parent = parentLink.component.transform;
            curTrans.parent = transform;
            float[] xyz;
            if ((xyz = curLink.Value.localPosToFloat()) == null)
            {
                if ((xyz = curJoint.localPosToFloat()) == null)
                {
                    xyz = new float[] { 0, 0, 0 };
                }
            }
            curTrans.localPosition = new Vector3(-xyz[0], xyz[2], xyz[1]);//+ curTrans.parent.transform.localPosition;
            curTrans.gameObject.SetActive(true);
            
    }
        */
        return true;
    }
    
    Color? handleMaterial(XElement material)
    {
        string colorName = material.Attribute("name").Value;
        Color? colorOut = null;

        if (materials.ContainsKey(colorName))
            return materials[colorName];

        XElement color = material.Element("color");
        if (color != null)
        {
            string colorVal = color.Attribute("rgba").Value;
            string[] colorValSplit = colorVal.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            float[] rbga = new float[colorValSplit.Length];
           

            for (int index = 0; index < colorValSplit.Length; ++index)
            {
                if (!float.TryParse(colorValSplit[index], out rbga[index]))
                {
                    rbga[index] = 0;
                }
            }
            if (rbga != null)
            {
                if (rbga.Length == 3)
                {
                    colorOut = new Color(rbga[0], rbga[1], rbga[2]);
                    materials.Add(colorName, colorOut);
                }

                if (rbga.Length == 4)
                {
                    colorOut = new Color(rbga[0], rbga[1], rbga[2], rbga[3]);
                    materials.Add(colorName, colorOut);
                }
            }
        }
        return colorOut;
    }
    
    //not being used at the moment, may be necessary for make more generic code
    bool handleJoint(XElement joint)
    {
        XElement origin = joint.Element("origin");
        XElement parent = joint.Element("parent");
        XElement child = joint.Element("child");
        string strOrigin = origin == null ? null : origin.Attribute("xyz") == null ? null : origin.Attribute("xyz").Value;
        /*
        if(parent != null && child != null)
        {
            joints.Add(child.Attribute("link").Value, new joint(parent.Attribute("link").Value, child.Attribute("link").Value, strOrigin));
        }
        */
        return true;
    }

    bool handleLink(XElement link)
    {
        if (link.Attribute("name").Value == "left_gripper")
            Debug.Log("thing");

        XElement visual;
        //get pose outside of visual for gazebo
        XElement pose = link.Element("pose");
        if ((visual = link.Element("visual")) != null)
        {
            XElement origin = visual.Element("origin");
            XElement geometry = visual.Element("geometry");
            XElement material = visual.Element("material");
            string xyz = origin == null ? pose == null ? null : pose.Value : origin.Attribute("xyz").Value;
            //string materialName = material == null ? null : material.Attribute("name") == null ? null : material.Attribute("name").Value;


            //hackey shit
            float[] rpy_rot = null;
            string localRot = visual.Element("origin") == null ? null : visual.Element("origin").Attribute("rpy") == null ? null : visual.Element("origin").Attribute("rpy").Value;
            if (localRot != null)
            {
                string[] poses = localRot.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                rpy_rot = new float[poses.Length];
                for (int index = 0; index < poses.Length; ++index)
                {
                    if (!float.TryParse(poses[index], out rpy_rot[index]))
                    {
                        rpy_rot[index] = 0;
                    }
                }
            }

            float[] xyz_pos = null;
            string localPos = visual.Element("origin") == null ? null : visual.Element("origin").Attribute("xyz") == null ? null : visual.Element("origin").Attribute("xyz").Value;
            if (localPos != null)
            {
                string[] poses = localPos.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
                xyz_pos = new float[poses.Length];
                for (int index = 0; index < poses.Length; ++index)
                {
                    if (!float.TryParse(poses[index], out xyz_pos[index]))
                    {
                        xyz_pos[index] = 0;
                    }
                }
            }
            //hackey shit


            Color? color = null;
            if(material != null)
            {
                color = handleMaterial(material);
            }

            if ( geometry != null) 
            {
                //handle mesh
                XElement mesh;
                if ((mesh = geometry.Element("mesh")) != null)
                {
                    string path = mesh.Attribute("filename") == null ? mesh.Element("uri") == null ? null : mesh.Element("uri").Value : mesh.Attribute("filename").Value;

                    if (path != null)
                    {
                        if (path.StartsWith("package://"))
                            path = path.Remove(0, 10);
                      //  else
                       //     throw new Exception("piss");
                        Debug.Log("Trying to load " + path);
                        if(path.EndsWith(".dae"))
                            path = path.Substring(0, path.LastIndexOf("."));
                        if (path.EndsWith(".DAE"))
                            path = path.Substring(0, path.LastIndexOf("."));

                        try {
                            UnityEngine.Object foundMesh = Resources.Load(path) as GameObject;
                            if (foundMesh != null)
                            {
                                GameObject go = Instantiate(foundMesh as GameObject);
                                // = new GameObject();
                                //goParent.transform.parent = transform;
                                //goParent.name = link.Attribute("name").Value;
                                //go.transform.parent = goParent.transform;

                                if (link.Attribute("name").Value == "pedestal")
                                    Debug.Log("thin");

                                if (go.transform.childCount == 0)
                                {
                                    /*
                                    if (go.transform.localEulerAngles.x != 270)
                                    {
                                        go.transform.localRotation *= Quaternion.Euler(-90, 0, 90);
                                    }
                                    else
                                    {
                                        go.transform.localRotation *= Quaternion.Euler(0, 0, 90);
                                    }
                                    */
                                    //go.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0) + go.transform.localEulerAngles);

                                    if (xyz_pos != null)
                                        go.transform.localPosition +=  new Vector3(xyz_pos[0], xyz_pos[1] , xyz_pos[2] );

                                    if (rpy_rot == null)
                                        go.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0) + go.transform.localEulerAngles);
                                    else
                                        go.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0) + go.transform.localEulerAngles + new Vector3(-rpy_rot[1] * 57.3f, rpy_rot[2] * 57.3f, rpy_rot[0] * 57.3f));



                                    GameObject goParent = new GameObject();
                                    goParent.transform.parent = transform;
                                    goParent.name = link.Attribute("name").Value;
                                    go.transform.parent = goParent.transform;

                                    if (go.GetComponent<MeshRenderer>() != null && color != null)
                                        go.GetComponent<MeshRenderer>().material.color = color.Value;
                                }
                                else
                                {

                                    foreach (Transform tf in go.transform)
                                    {
                                        if (tf.name == "Lamp" || tf.name == "Camera")
                                        {
                                            Destroy(tf.gameObject);
                                            continue;
                                        }


                                        if (xyz_pos != null)
                                            tf.transform.localPosition += new Vector3(-xyz_pos[1], xyz_pos[2], xyz_pos[0]);

                                        //tf.localRotation = Quaternion.Euler(-90, 0, 90);
                                        if (rpy_rot == null)
                                            tf.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0) + tf.transform.localEulerAngles + go.transform.localEulerAngles);
                                        else
                                            tf.transform.localRotation = Quaternion.Euler(new Vector3(0, 90, 0) + tf.transform.localEulerAngles + go.transform.localEulerAngles + new Vector3(-rpy_rot[1] * 57.3f, rpy_rot[2] * 57.3f, rpy_rot[0] * 57.3f));

                                        if (tf.GetComponent<MeshRenderer>() != null && color != null)
                                            tf.GetComponent<MeshRenderer>().material.color = color.Value;
                                    }
                                    go.name = link.Attribute("name").Value;
                                    go.transform.parent = transform;

                                }
                            }
                        }
                        catch(Exception e)
                        {
                            Debug.LogWarning(e);
                        }
                       // links.Add(go.name, new link(go, xyz));
                    }
                }

                //handle shapes (Cubes, Cylinders)
                XElement shape;
                if ((shape = geometry.Element("box")) != null)
                {
                    string dimensions = shape.Attribute("size").Value;
                    string[] components = dimensions.Split(' ');
                    float x, y, z;
                    if(float.TryParse(components[0], out x) && float.TryParse(components[1], out y) && float.TryParse(components[2], out z) )
                    {
                        GameObject parent = new GameObject();
                        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);

                        parent.name = link.Attribute("name").Value;
                        parent.transform.parent = transform;
                        go.transform.parent = parent.transform;
                        go.transform.localScale = new Vector3(y, z, x);

                        if (xyz_pos != null)
                            go.transform.localPosition += new Vector3(-xyz_pos[1], xyz_pos[2], xyz_pos[0]);

                        if (rpy_rot != null)
                            go.transform.localRotation = Quaternion.Euler(new Vector3(-rpy_rot[1], rpy_rot[2], rpy_rot[0]));
                        
                        if (go.GetComponent<MeshRenderer>() != null && color != null)
                            go.GetComponent<MeshRenderer>().material.color = color.Value;
                        //links.Add(go.name, new link(go, xyz));

                    }

                }

                if ((shape = geometry.Element("cylinder")) != null)
                {
                    string length = shape.Attribute("length").Value;
                    string radius = shape.Attribute("radius").Value;
                    float fLength, fRadius;
                    if (float.TryParse(length, out fLength) && float.TryParse(radius, out fRadius))
                    {
                        GameObject parent = new GameObject();
                        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                        parent.name = link.Attribute("name").Value;
                        parent.transform.parent = transform;
                        go.transform.parent = parent.transform;
                        go.transform.localScale = new Vector3(fRadius * 2, fLength/2, fRadius * 2);

                        if (xyz_pos != null)
                            go.transform.localPosition += new Vector3(-xyz_pos[1], xyz_pos[2], xyz_pos[0]);

                        if (rpy_rot != null)
                            go.transform.localRotation = Quaternion.Euler(new Vector3(-rpy_rot[1], rpy_rot[2], rpy_rot[0]));


                        if (go.GetComponent<MeshRenderer>() != null && color != null)
                            go.GetComponent<MeshRenderer>().material.color = color.Value;
                        //links.Add(go.name, new link(go, xyz));
                    }

                }

            }

        }else
        {
            GameObject go = new GameObject();
            go.transform.parent = transform;
            go.name = link.Attribute("name").Value;
            //links.Add(go.name, new link(go, null));
        }
        return true;
    }


    // Update is called once per frame
    void Update()
    {

        foreach (Transform tf in transform)
        {
            Transform tff;
            if (tfviz.queryTransforms(NameSpace + "/" + tf.name, out tff))
            {
                tf.transform.position = tff.position;
                tf.transform.rotation = tff.rotation;
                // tff = transform;
            }

            //tf.transform.position = tff.position;
            //tf.transform.rotation = tff.rotation;
        }

    }


}

class link
{
    public GameObject component { set; get; }
    public string localPos { set; get; }
    public link(GameObject component, string localPos)
    {
        this.component = component;
        this.localPos = localPos;
    }
    public float[] localPosToFloat()
    {
        float[] xyz;
        if (localPos != null)
        {
            string[] poses = localPos.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            xyz = new float[poses.Length];
            for (int index = 0; index < poses.Length; ++index)
            {
                if (!float.TryParse(poses[index], out xyz[index]))
                {
                    xyz[index] = 0;
                }
            }
        }else
        {
            return null;
        }
        return xyz;

    }

}

class joint
{
    public string parent { set; get; }
    public string child  { set; get; }
    public string childLocalPos  { set; get; }
    public joint ( string parent, string child, string childLocalPos)
    {
        this.child = child;
        this.parent = parent;
        this.childLocalPos = childLocalPos;
    }
    public float[] localPosToFloat()
    {
        float[] xyz;
        if (childLocalPos != null)
        {
            string[] poses = childLocalPos.Split(new char[0], StringSplitOptions.RemoveEmptyEntries);
            xyz = new float[poses.Length];
            for (int index = 0; index < poses.Length; ++index)
            {
                if (!float.TryParse(poses[index], out xyz[index]))
                {
                    xyz[index] = 0;
                }
            }
        }
        else
        {
            return null;
        }
        return xyz;

    }
    
}


