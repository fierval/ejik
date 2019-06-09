using System;

using UnityEditor;
using UnityEngine;

namespace AdvancedInspector
{
    [CanEditMultipleObjects]
    [CustomEditor(typeof(Rigidbody2D), true)]
    public class Rigidbody2DEditor : InspectorEditor
    {
        protected override void RefreshFields()
        {
            Type type = typeof(Rigidbody2D);

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("mass"),
                new DescriptorAttribute("Mass", "The mass of the rigidbody", "https://docs.unity3d.com/Documentation/ScriptReference/Rigidbody-mass.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("drag"),
                new DescriptorAttribute("Linear Drag", "Coefficient of drag.", "http://docs.unity3d.com/ScriptReference/Rigidbody2D-drag.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("angularDrag"),
                new DescriptorAttribute("Angular Drag", "Coefficient of angular drag.", "http://docs.unity3d.com/ScriptReference/Rigidbody2D-angularDrag.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("gravityScale"),
                new DescriptorAttribute("Gravity Scale", "The degree to which this object is affected by gravity.", "http://docs.unity3d.com/ScriptReference/Rigidbody2D-gravityScale.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("isKinematic"),
                new DescriptorAttribute("Is Kinematic", "Should this rigidbody be taken out of physics control?", "http://docs.unity3d.com/ScriptReference/Rigidbody2D-isKinematic.html")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("interpolation"),
                new DescriptorAttribute("Interpolation", "Physics interpolation used between updates.", "http://docs.unity3d.com/ScriptReference/Rigidbody2D-interpolation.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("sleepMode"),
                new DescriptorAttribute("Sleep Mode", "The sleep state that the rigidbody will initially be in.", "http://docs.unity3d.com/ScriptReference/Rigidbody2D-sleepMode.html")));
            Fields.Add(new InspectorField(type, Instances, type.GetProperty("collisionDetectionMode"),
                new DescriptorAttribute("Collision Detection", "The method used by the physics engine to check if two objects have collided.", "http://docs.unity3d.com/ScriptReference/Rigidbody2D-collisionDetectionMode.html")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("constraints"),
                new DescriptorAttribute("Constraints", "Controls which degrees of freedom are allowed for the simulation of this Rigidbody2D.", "http://docs.unity3d.com/ScriptReference/Rigidbody2D-constraints.html")));

            Fields.Add(new InspectorField(type, Instances, type.GetProperty("velocity"), new InspectAttribute(InspectorLevel.Advanced),
                new DescriptorAttribute("Velocity", "Linear velocity of the rigidbody.", "http://docs.unity3d.com/ScriptReference/Rigidbody2D-velocity.html")));
        }
    }
}
