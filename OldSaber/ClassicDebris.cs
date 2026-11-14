using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json.Linq;
using UnityEngine;
using Object=UnityEngine.Object;

namespace OldSaber
{
    internal sealed class ClassicDebrisPatches : IDisposable
    {
        private object harmony;
        private MethodInfo unpatch;
        internal void Enable()
        {
            ClassicDebris.LoadProfile();
            // BSIPA already ships/loads its Harmony fork. Resolve that exact
            // assembly rather than bundling a competing 0Harmony.dll.
            var reference=typeof(IPA.PluginAttribute).Assembly.GetReferencedAssemblies().Single(n=>n.Name=="0Harmony");
            var assembly=Assembly.Load(reference);
            var type=assembly.GetType("HarmonyLib.Harmony",true);
            var methodType=assembly.GetType("HarmonyLib.HarmonyMethod",true);
            harmony=Activator.CreateInstance(type,new object[]{"OldSaber.ClassicDebris"});
            unpatch=type.GetMethod("UnpatchSelf",Type.EmptyTypes);
            if(unpatch==null)throw new MissingMethodException("Harmony.UnpatchSelf");
            var patch=type.GetMethods().Where(m=>m.Name=="Patch" && m.GetParameters().Length>=5 && m.GetParameters()[0].ParameterType==typeof(MethodBase))
                .OrderByDescending(m=>m.GetParameters().Length).First();
            var args=new object[patch.GetParameters().Length];
            args[0]=typeof(NoteDebris).GetMethod("Init");
            for(int i=1;i<args.Length;i++)
            {
                string name=patch.GetParameters()[i].Name;
                if(name=="prefix" || name=="postfix")
                {
                    var hook=typeof(ClassicDebris).GetMethod(name=="prefix"?"Prefix":"Postfix",BindingFlags.Static|BindingFlags.Public);
                    args[i]=Activator.CreateInstance(methodType,new object[]{hook});
                }
            }
            try
            {
                patch.Invoke(harmony,args);
                var saberArgs=new object[args.Length];
                saberArgs[0]=typeof(SaberModelController).GetMethod("Init");
                for(int i=1;i<saberArgs.Length;i++)
                    if(patch.GetParameters()[i].Name=="postfix")
                        saberArgs[i]=Activator.CreateInstance(methodType,new object[]{typeof(ClassicSabers).GetMethod("Postfix")});
                patch.Invoke(harmony,saberArgs);
                foreach(var target in new[]{typeof(ObstacleMaterialSetter).GetMethod("Init"),typeof(StretchableObstacle).GetMethod("SetAllProperties"),typeof(StretchableObstacle).GetMethod("SetSizeAndOffset")})
                {
                    var obstacleArgs=new object[args.Length];obstacleArgs[0]=target;
                    var hook=typeof(ClassicObstacles).GetMethod(target.DeclaringType==typeof(ObstacleMaterialSetter)?"GraphicsPostfix":"SizePostfix");
                    for(int i=1;i<obstacleArgs.Length;i++)if(patch.GetParameters()[i].Name=="postfix")obstacleArgs[i]=Activator.CreateInstance(methodType,new object[]{hook});
                    patch.Invoke(harmony,obstacleArgs);
                }

            }
            catch {Dispose();throw;}
            Plugin.Log.Info("Classic debris enabled: original separation/spin, 1-second lifetime, linear dissolve and Rigidbody settings.");
        }
        public void Dispose()
        {
            ClassicSabers.Restore();
            ClassicObstacles.Restore();
            if(harmony!=null && unpatch!=null) {unpatch.Invoke(harmony,null);harmony=null;}
            foreach(var state in Resources.FindObjectsOfTypeAll<ClassicDebrisState>())
            {
                if(!state.gameObject.scene.IsValid())continue;
                state.Restore();Object.Destroy(state);
            }
        }
    }
    internal static class ClassicDebris
    {
        private static readonly System.Random random=new System.Random();
        internal static DebrisVector[] Vertices;
        internal static float Lifetime,Mass,Drag,AngularDrag,ColliderRadius;
        internal static AnimationCurve Cutout;
        internal static void LoadProfile()
        {
            JObject data;
            using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("OldSaber.ClassicDebris.json"))
            using(var reader=new StreamReader(stream))data=JObject.Parse(reader.ReadToEnd());
            Lifetime=(float)data["lifetime"];Mass=(float)data["mass"];Drag=(float)data["drag"];AngularDrag=(float)data["angularDrag"];
            ColliderRadius=(float)data["colliderRadius"];
            Vertices=data["centroidVertices"].Select(v=>new DebrisVector((float)v[0],(float)v[1],(float)v[2])).ToArray();
            Cutout=new AnimationCurve(data["cutoutCurve"]["m_Curve"].Select(k=>new Keyframe((float)k["time"],(float)k["value"],(float)k["inSlope"],(float)k["outSlope"])).ToArray());
        }
        private static DebrisVector D(Vector3 v) {return new DebrisVector(v.x,v.y,v.z);}
        private static Vector3 V(DebrisVector v) {return new Vector3(v.X,v.Y,v.Z);}
        public static void Prefix(Vector3 cutNormal,Quaternion rotationOffset,ref Vector3 force,ref Vector3 torque,ref float lifeTime,out float __state)
        {
            __state=lifeTime;
            if(!Plugin.Active || !Plugin.Config.OldNotes)return;
            force=rotationOffset*V(ClassicDebrisModel.Velocity(D(cutNormal),ClassicDebrisModel.OnSphere(random)));
            torque=rotationOffset*V(ClassicDebrisModel.Spin(ClassicDebrisModel.InsideSphere(random)));
            lifeTime=Lifetime;
        }
        public static void Postfix(NoteDebris __instance,Vector3 notePos,Quaternion noteRot,Vector3 noteScale,Vector3 positionOffset,
            Quaternion rotationOffset,Vector3 cutPoint,Vector3 cutNormal,Vector3 force,Vector3 torque,bool forceOnlySimplePhysics,float __state)
        {
            if(!Plugin.Active || !Plugin.Config.OldNotes)return;
            var state=__instance.GetComponent<ClassicDebrisState>();
            if(!state) {state=__instance.gameObject.AddComponent<ClassicDebrisState>();state.Capture(__instance);}
            state.OriginalLifetime=__state;
            Fields.Set(__instance,"_cutoutCurve",Cutout);
            // Recreate the original cut-half pivot, including its scale-aware
            // inverse transform and projection/averaging rule (no modern clamp).
            if(Mathf.Abs(noteScale.x)>1e-6f && Mathf.Abs(noteScale.y)>1e-6f && Mathf.Abs(noteScale.z)>1e-6f)
            {
                Vector3 p=Quaternion.Inverse(noteRot)*(cutPoint-notePos),n=Quaternion.Inverse(noteRot)*cutNormal;
                p=new Vector3(p.x/noteScale.x,p.y/noteScale.y,p.z/noteScale.z);
                n=new Vector3(n.x/noteScale.x,n.y/noteScale.y,n.z/noteScale.z);
                var center=V(ClassicDebrisModel.Centroid(Vertices,D(p),D(n)));
                __instance.transform.SetPositionAndRotation(rotationOffset*(notePos+noteRot*Vector3.Scale(noteScale,center))+positionOffset,rotationOffset*noteRot);
                Fields.Get<Transform>(__instance,"_meshTransform").localPosition=-center;
                var block=Fields.Get<MaterialPropertyBlockController>(__instance,"_materialPropertyBlockController");
                block.materialPropertyBlock.SetVector("_CutPlane",new Vector4(n.x,n.y,n.z,-Vector3.Dot(n,p)));
                block.ApplyChanges();
            }
            state.Apply(force,torque,forceOnlySimplePhysics);
        }
    }
    public sealed class ClassicDebrisState : MonoBehaviour
    {
        private NoteDebris debris;
        private NoteDebrisPhysics physics;
        private Rigidbody body;
        private SphereCollider sphere;
        private Collider[] nativeColliders;
        private bool[] colliderEnabled;
        private AnimationCurve originalCurve;
        private bool addedBody,restored,originalGravity,originalCollisions;
        private float mass,drag,angularDrag,maxAngularVelocity;
        private RigidbodyInterpolation interpolation;
        private RigidbodyConstraints constraints;
        private CollisionDetectionMode collisionMode;
        internal float OriginalLifetime;
        private bool simpleOnly;
        internal void Capture(NoteDebris value)
        {
            debris=value;physics=Fields.Get<NoteDebrisPhysics>(value,"_physics");
            originalCurve=Fields.Get<AnimationCurve>(value,"_cutoutCurve");
            body=GetComponent<Rigidbody>();addedBody=!body;
            if(!body)body=gameObject.AddComponent<Rigidbody>();
            mass=body.mass;drag=body.drag;angularDrag=body.angularDrag;maxAngularVelocity=body.maxAngularVelocity;
            originalGravity=body.useGravity;originalCollisions=body.detectCollisions;
            interpolation=body.interpolation;constraints=body.constraints;collisionMode=body.collisionDetectionMode;
            nativeColliders=GetComponentsInChildren<Collider>(true);colliderEnabled=nativeColliders.Select(c=>c.enabled).ToArray();
            sphere=gameObject.AddComponent<SphereCollider>();sphere.radius=ClassicDebris.ColliderRadius;sphere.center=Vector3.zero;sphere.isTrigger=false;sphere.enabled=false;
        }
        internal void Apply(Vector3 force,Vector3 torque,bool forceSimple)
        {
            simpleOnly=forceSimple;
            if(forceSimple)
            {
                // Honor the game's deterministic/replay path, while retaining the
                // classic impulses and lifetime. Re-init after the pivot correction.
                sphere.enabled=false;RestoreColliders();body.isKinematic=true;
                if(physics is NoteDebrisSimplePhysics)physics.enabled=true;
                physics.Init(force,torque,true);return;
            }
            foreach(var component in GetComponents<NoteDebrisPhysics>())component.enabled=false;
            foreach(var collider in nativeColliders)if(collider)collider.enabled=false;
            sphere.enabled=true;
            body.mass=ClassicDebris.Mass;body.drag=ClassicDebris.Drag;body.angularDrag=ClassicDebris.AngularDrag;
            body.useGravity=true;body.detectCollisions=true;body.constraints=RigidbodyConstraints.None;
            body.collisionDetectionMode=CollisionDetectionMode.Discrete;
            body.interpolation=RigidbodyInterpolation.Interpolate;body.maxAngularVelocity=7f;
            body.isKinematic=false;body.position=transform.position;body.rotation=transform.rotation;
            body.velocity=Vector3.zero;body.angularVelocity=Vector3.zero;
            body.AddForce(force,ForceMode.VelocityChange);body.AddTorque(torque,ForceMode.VelocityChange);body.WakeUp();
        }
        private void RestoreColliders() {for(int i=0;i<nativeColliders.Length;i++)if(nativeColliders[i])nativeColliders[i].enabled=colliderEnabled[i];}
        internal void Restore()
        {
            if(restored)return;restored=true;
            if(sphere){sphere.enabled=false;Object.Destroy(sphere);}
            RestoreColliders();
            if(debris){Fields.Set(debris,"_cutoutCurve",originalCurve);Fields.Set(debris,"_lifeTime",OriginalLifetime);}
            if(body)
            {
                var velocity=body.velocity;var spin=body.angularVelocity;
                body.mass=mass;body.drag=drag;body.angularDrag=angularDrag;body.maxAngularVelocity=maxAngularVelocity;
                body.useGravity=originalGravity;body.detectCollisions=originalCollisions;body.constraints=constraints;
                body.collisionDetectionMode=collisionMode;body.interpolation=interpolation;
                body.isKinematic=true;
                if(physics && gameObject.activeInHierarchy)
                {
                    if(physics is NoteDebrisSimplePhysics)physics.enabled=true;
                    physics.Init(velocity,spin,simpleOnly);
                }
                if(addedBody)Object.Destroy(body);
            }
        }
    }
}
