using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditor.Animations;

// Run in the Editor through Pipeline script run, outside Assets to avoid a runtime dependency.
public static class GameplayDanceBuilder
{
    public const string Folder = "Assets/5.Animation/Gameplay";
    public const string ControllerPath = "Assets/Mibit/Animations/robo.controller";
    static readonly float[] Times = {0f,.10f,.26f,.37f,.52f,.74f,1f};
    static readonly float[] Accent = {0f,-.14f,.9f,1f,.65f,.18f,0f};
    static readonly string IdleSource = "Assets/PolyOne/Chibi Character/Animation/Idle.anim";

    public static string Build(bool refreshExisting=true)
    {
        if (Application.isPlaying) throw new InvalidOperationException("Stop Play Mode before building clips.");
        if (!AssetDatabase.IsValidFolder(Folder)) AssetDatabase.CreateFolder("Assets/5.Animation","Gameplay");
        var source=AssetDatabase.LoadAssetAtPath<AnimationClip>(IdleSource);
        var baseline=AnimationUtility.GetCurveBindings(source).Where(b=>b.type==typeof(Animator)&&!b.propertyName.Contains("T.")&&!b.propertyName.Contains("Q."))
            .ToDictionary(b=>b.propertyName,b=>AnimationUtility.GetEditorCurve(source,b).Evaluate(0f));
        // Symmetric, relaxed preparation pose. Finger curves retain the existing character's grip.
        Set(baseline,"Spine Front-Back",-.06f); Set(baseline,"Chest Front-Back",-.04f);
        foreach(var key in baseline.Keys.ToArray())
            if(key.Contains("Left-Right")||key.Contains("Eye")||key.Contains("Jaw")) baseline[key]=0f;
        foreach(var side in new[]{"Left","Right"})
        {
            Set(baseline,side+" Upper Leg Front-Back",.37f); Set(baseline,side+" Upper Leg In-Out",.02f);
            Set(baseline,side+" Upper Leg Twist In-Out",0f); Set(baseline,side+" Lower Leg Stretch",.74f);
            Set(baseline,side+" Lower Leg Twist In-Out",.24f); Set(baseline,side+" Foot Up-Down",-.13f);
            Set(baseline,side+" Foot Twist In-Out",0f); Set(baseline,side+" Toes Up-Down",0f);
            Set(baseline,side+" Shoulder Down-Up",.02f); Set(baseline,side+" Shoulder Front-Back",.12f);
            Set(baseline,side+" Arm Down-Up",-.49f); Set(baseline,side+" Arm Front-Back",.12f);
            Set(baseline,side+" Arm Twist In-Out",-.15f); Set(baseline,side+" Forearm Stretch",.38f);
            Set(baseline,side+" Forearm Twist In-Out",.13f); Set(baseline,side+" Hand Down-Up",0f);
            Set(baseline,side+" Hand In-Out",.1f);
        }
        Set(baseline,"Neck Nod Down-Up",.04f); Set(baseline,"Head Nod Down-Up",.04f);
        var clips=new Dictionary<string,AnimationClip>();
        foreach(var key in new[]{"Q","E","W","A","D","Idle"})
        {
            string clipName=key=="Idle"?"Dance_Idle_Groove":key=="Q"?"Dance_Q_LeftArmPop":key=="E"?"Dance_E_RightArmPop":key=="W"?"Dance_W_HeadChestIsolation":key=="A"?"Dance_A_LeftStep":"Dance_D_RightStep";
            var existing=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/"+clipName+".anim");
            if(!refreshExisting&&key!="W"&&existing!=null){clips[key]=existing;continue;}
            var frames=new List<Dictionary<string,float>>();
            for(int i=0;i<Times.Length;i++)
            {
                float t=Times[i],a=Accent[i],bounce=Mathf.Sin(t*Mathf.PI*2f);
                var pose=new Dictionary<string,float>(baseline);
                Set(pose,"RootT.x",0f); Set(pose,"RootT.y",.952f+.012f*bounce); Set(pose,"RootT.z",0f);
                Set(pose,"RootQ.x",0f); Set(pose,"RootQ.y",0f); Set(pose,"RootQ.z",0f); Set(pose,"RootQ.w",1f);
                foreach(var side in new[]{"Left","Right"})
                {
                    Add(pose,side+" Upper Leg Front-Back",-.025f*bounce);
                    Add(pose,side+" Lower Leg Stretch",.045f*bounce);
                    Add(pose,side+" Foot Up-Down",.018f*bounce);
                }
                Add(pose,"Spine Front-Back",.018f*bounce);
                if(key=="Q"||key=="E")
                {
                    string side=key=="Q"?"Left":"Right",other=key=="Q"?"Right":"Left";
                    float sign=key=="Q"?-1f:1f;
                    Add(pose,side+" Shoulder Down-Up",.2f*a);
                    Add(pose,side+" Arm Down-Up",.78f*a);
                    Add(pose,side+" Arm Front-Back",.18f*a);
                    Add(pose,side+" Forearm Stretch",.40f*a);
                    Add(pose,side+" Forearm Twist In-Out",-.22f*a);
                    Add(pose,side+" Hand Down-Up",.24f*a);
                    Add(pose,other+" Forearm Stretch",-.27f*a);
                    Add(pose,other+" Arm Front-Back",.18f*a);
                    Add(pose,"Chest Twist Left-Right",sign*.19f*a);
                    Add(pose,"Spine Left-Right",-sign*.14f*a);
                    Add(pose,"Head Tilt Left-Right",sign*.12f*a);
                }
                if(key=="A"||key=="D")
                {
                    string side=key=="A"?"Left":"Right",other=key=="A"?"Right":"Left";
                    float sign=key=="A"?-1f:1f;
                    Add(pose,side+" Upper Leg Front-Back",.19f*a);
                    Add(pose,side+" Upper Leg In-Out",.38f*a);
                    Add(pose,side+" Lower Leg Stretch",-.32f*a);
                    Add(pose,side+" Foot Up-Down",-.28f*a);
                    Add(pose,other+" Upper Leg Front-Back",.035f*a);
                    Add(pose,other+" Lower Leg Stretch",-.045f*a);
                    Add(pose,"Spine Left-Right",sign*.12f*a);
                    Add(pose,"Chest Twist Left-Right",-sign*.14f*a);
                    Add(pose,other+" Arm Front-Back",.3f*a);
                    Add(pose,other+" Forearm Stretch",-.25f*a);
                    Add(pose,side+" Arm Front-Back",-.18f*a);
                    Add(pose,side+" Arm Down-Up",.16f*a);
                }
                if(key=="W")
                {
                    // A clean upward pop: a single accent, stable head axis, feet planted.
                    Add(pose,"Spine Front-Back",-.18f*a);
                    Add(pose,"Chest Front-Back",.26f*a);
                    Add(pose,"Neck Nod Down-Up",.24f*a);
                    Add(pose,"Head Nod Down-Up",.20f*a);
                    foreach(var side in new[]{"Left","Right"})
                    {
                        Add(pose,side+" Shoulder Down-Up",.14f*a);
                        Add(pose,side+" Forearm Stretch",-.24f*a);
                        Add(pose,side+" Arm Down-Up",.10f*a);
                    }
                }
                frames.Add(pose);
            }
            clips[key]=Write(clipName,frames);
        }
        // Reuse the existing inversion, retaining a readable freeze while sharing the entry/exit pose.
        var existingFreeze=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/Dance_S_HandstandFreeze_Reused.anim");
        if(!refreshExisting&&existingFreeze!=null)clips["S"]=existingFreeze;
        else{
        var freeze=AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/5.Animation/DanceAnim/breakdance freeze var 1.anim");
        var ready=AnimationUtility.GetCurveBindings(clips["Idle"]).ToDictionary(b=>b.propertyName,b=>AnimationUtility.GetEditorCurve(clips["Idle"],b).Evaluate(0f));
        var freezeBindings=AnimationUtility.GetCurveBindings(freeze).Where(b=>b.type==typeof(Animator)&&(b.propertyName.StartsWith("Root")||ready.ContainsKey(b.propertyName))).ToArray();
        var freezeFrames=new List<Dictionary<string,float>>();
        float[] freezeTimes={0f,.85f,1.73f,2.04f,2.04f,3.25f,0f};
        float[] freezeWeights={0f,.7f,1f,1f,1f,.85f,0f};
        for(int i=0;i<Times.Length;i++)
        {
            var pose=new Dictionary<string,float>(ready);
            foreach(var b in freezeBindings)
            {
                float origin=ready.ContainsKey(b.propertyName)?ready[b.propertyName]:0f;
                pose[b.propertyName]=Mathf.LerpUnclamped(origin,AnimationUtility.GetEditorCurve(freeze,b).Evaluate(freezeTimes[i]),freezeWeights[i]);
            }
            freezeFrames.Add(pose);
        }
        clips["S"]=Write("Dance_S_HandstandFreeze_Reused",freezeFrames);
        }
        BuildFailures(clips,baseline);
        foreach(var pair in clips.Where(p=>refreshExisting||p.Key=="W"||p.Key.StartsWith("Fail_"))) Ground(pair.Value,pair.Key);
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        Undo.RecordObject(controller,"Assign gameplay dance clips");
        foreach(var child in controller.layers[0].stateMachine.states)
        {
            var state=child.state;
            string key=state.name=="Idle"?"Idle":state.name.StartsWith("DanceFail_")?"Fail_"+state.name.Substring(10):state.name.StartsWith("Dance_")?state.name.Substring(6):null;
            if(key==null||!clips.ContainsKey(key))continue;
            Undo.RecordObject(state,"Assign gameplay dance clip");state.motion=clips[key];state.speed=1f;EditorUtility.SetDirty(state);
        }
        foreach(var key in new[]{"Q","E","W","A","D","S"})
        {
            var machine=controller.layers[0].stateMachine;
            string name="DanceFail_"+key;
            var state=machine.states.Select(x=>x.state).FirstOrDefault(s=>s.name==name);
            if(state==null)state=machine.AddState(name,new Vector3(650,100+Array.IndexOf(new[]{"Q","E","W","A","D","S"},key)*65,0));
            state.motion=clips["Fail_"+key];state.speed=1f;
            var idle=machine.states.First(s=>s.state.name=="Idle").state;
            var transition=state.transitions.FirstOrDefault(t=>t.destinationState==idle);
            if(transition==null)transition=state.AddTransition(idle);
            transition.hasExitTime=true;transition.exitTime=.88f;transition.hasFixedDuration=false;transition.duration=.12f;
            transition.interruptionSource=TransitionInterruptionSource.SourceThenDestination;transition.orderedInterruption=false;
            EditorUtility.SetDirty(state);
        }
        EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
        return "Built 7 success/groove clips and 6 non-looping failure clips; wired Bad states with return to Idle.";
    }

    static void BuildFailures(Dictionary<string,AnimationClip> clips,Dictionary<string,float> baseline)
    {
        foreach(var key in new[]{"Q","E","W","A","D","S"})
        {
            var frames=new List<Dictionary<string,float>>();
            for(int i=0;i<Times.Length;i++)
            {
                float t=Times[i],a=Accent[i];
                float wobble=Sample(new[]{0f,.08f,-.8f,.65f,-.4f,.12f,0f},t);
                var pose=new Dictionary<string,float>(baseline);
                Set(pose,"RootT.x",0f);Set(pose,"RootT.y",.952f+.012f*Mathf.Sin(t*Mathf.PI*2f));Set(pose,"RootT.z",0f);
                Set(pose,"RootQ.x",0f);Set(pose,"RootQ.y",0f);Set(pose,"RootQ.z",0f);Set(pose,"RootQ.w",1f);
                foreach(var side in new[]{"Left","Right"})
                {
                    Add(pose,side+" Upper Leg Front-Back",-.025f*Mathf.Sin(t*Mathf.PI*2f));
                    Add(pose,side+" Lower Leg Stretch",.045f*Mathf.Sin(t*Mathf.PI*2f));
                    Add(pose,side+" Foot Up-Down",.018f*Mathf.Sin(t*Mathf.PI*2f));
                }
                Add(pose,"Spine Front-Back",.018f*Mathf.Sin(t*Mathf.PI*2f));
                if(key=="W")
                {
                    // The former W success: an intentionally loose, side-to-side wobble.
                    float lateral=Sample(new[]{0f,-.1f,.8f,-.65f,.32f,-.12f,0f},t);
                    Add(pose,"Spine Front-Back",-.22f*a);Add(pose,"Chest Front-Back",.46f*a);
                    Add(pose,"Neck Nod Down-Up",-.22f*a);Add(pose,"Head Nod Down-Up",.30f*a);
                    Add(pose,"Neck Tilt Left-Right",.26f*lateral);Add(pose,"Head Tilt Left-Right",.32f*lateral);
                    Add(pose,"Chest Left-Right",-.10f*lateral);
                    foreach(var side in new[]{"Left","Right"}){Add(pose,side+" Forearm Stretch",-.2f*a);Add(pose,side+" Arm Down-Up",.07f*a);}
                }
                if(key=="Q"||key=="E")
                {
                    string side=key=="Q"?"Left":"Right",other=key=="Q"?"Right":"Left";float sign=key=="Q"?-1f:1f;
                    // The elbow folds before the pop lands; the wrist droops and the other arm catches balance.
                    Add(pose,side+" Arm Down-Up",.50f*a);Add(pose,side+" Arm Front-Back",-.35f*a);
                    Add(pose,side+" Arm Twist In-Out",.55f*a);Add(pose,side+" Forearm Stretch",-.95f*a);
                    Add(pose,side+" Hand Down-Up",-.65f*a);Add(pose,side+" Hand In-Out",.45f*a);
                    Add(pose,other+" Arm Down-Up",.40f*a);Add(pose,other+" Forearm Stretch",-.60f*a);
                    Add(pose,"Spine Left-Right",sign*.30f*a);Add(pose,"Chest Twist Left-Right",sign*.28f*wobble);
                    Add(pose,"Head Tilt Left-Right",-sign*.24f*wobble);Add(pose,"Neck Turn Left-Right",sign*.25f*wobble);
                }
                if(key=="A"||key=="D")
                {
                    string side=key=="A"?"Left":"Right",other=key=="A"?"Right":"Left";float sign=key=="A"?-1f:1f;
                    // A pigeon-toed, inward knee lift instead of the clean outward side tap.
                    Add(pose,side+" Upper Leg Front-Back",.50f*a);Add(pose,side+" Upper Leg In-Out",-.35f*a);
                    Add(pose,side+" Lower Leg Stretch",-.80f*a);Add(pose,side+" Foot Up-Down",.60f*a);
                    Add(pose,side+" Foot Twist In-Out",-.35f*a);Add(pose,other+" Lower Leg Stretch",-.10f*a);
                    foreach(var arm in new[]{"Left","Right"}){Add(pose,arm+" Arm Down-Up",.38f*a);Add(pose,arm+" Forearm Stretch",-.55f*a);}
                    Add(pose,other+" Shoulder Down-Up",.2f*a);Add(pose,"Spine Left-Right",sign*.38f*a);
                    Add(pose,"Chest Front-Back",.24f*a);Add(pose,"Head Tilt Left-Right",-sign*.28f*wobble);
                }
                if(key=="S")
                {
                    // An aborted handstand: crouch, try one kick, wobble and stand back up.
                    foreach(var side in new[]{"Left","Right"})
                    {
                        Add(pose,side+" Upper Leg Front-Back",.48f*a);Add(pose,side+" Lower Leg Stretch",-.95f*a);
                        Add(pose,side+" Foot Up-Down",-.25f*a);Add(pose,side+" Arm Down-Up",-.16f*a);
                        Add(pose,side+" Arm Front-Back",-.42f*a);Add(pose,side+" Forearm Stretch",.50f*a);
                        Add(pose,side+" Hand Down-Up",-.45f*a);
                    }
                    Add(pose,"Spine Front-Back",.60f*a);Add(pose,"Chest Front-Back",.40f*a);
                    Add(pose,"Head Nod Down-Up",-.35f*a);Add(pose,"Head Tilt Left-Right",.22f*wobble);
                    Add(pose,"Left Upper Leg In-Out",.24f*a);Add(pose,"Right Upper Leg Front-Back",.18f*wobble);
                    var q=Quaternion.Euler(24f*a,0f,-12f*wobble);
                    Set(pose,"RootQ.x",q.x);Set(pose,"RootQ.y",q.y);Set(pose,"RootQ.z",q.z);Set(pose,"RootQ.w",q.w);
                }
                frames.Add(pose);
            }
            string suffix=key=="Q"?"LeftArmFlop":key=="E"?"RightArmFlop":key=="W"?"HeadWobble":key=="A"?"LeftLegStumble":key=="D"?"RightLegStumble":"FreezeFumble";
            clips["Fail_"+key]=Write("DanceFail_"+key+"_"+suffix,frames,false);
        }
    }

    static AnimationClip Write(string name,List<Dictionary<string,float>> poses,bool loop=true)
    {
        string path=Folder+"/"+name+".anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}else clip.ClearCurves();
        clip.name=name;clip.frameRate=60f;clip.legacy=false;
        foreach(var property in poses.SelectMany(x=>x.Keys).Distinct())
        {
            var curve=new AnimationCurve(Times.Select((time,i)=>new Keyframe(time,poses[i][property])).ToArray());
            for(int i=0;i<curve.length;i++)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);
                AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);
            }
            // Equal endpoint slopes preserve the shared groove's velocity at a loop boundary.
            if(property=="RootT.y"||property.Contains("Leg Front-Back")||property.Contains("Lower Leg Stretch")||property.Contains("Foot Up-Down")||property=="Spine Front-Back")
            {
                var first=curve.keys[0];var last=curve.keys[curve.length-1];
                float slope=((curve.keys[1].value-first.value)/Times[1]+(last.value-curve.keys[curve.length-2].value)/(1f-Times[Times.Length-2]))*.5f;
                first.inTangent=first.outTangent=last.inTangent=last.outTangent=slope;
                curve.MoveKey(0,first);curve.MoveKey(curve.length-1,last);
            }
            AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),property),curve);
        }
        var settings=AnimationUtility.GetAnimationClipSettings(clip);
        settings.startTime=0f;settings.stopTime=1f;settings.loopTime=loop;settings.loopBlend=false;
        settings.keepOriginalOrientation=true;settings.keepOriginalPositionY=true;settings.keepOriginalPositionXZ=true;
        settings.loopBlendOrientation=true;settings.loopBlendPositionY=false;settings.loopBlendPositionXZ=true;
        AnimationUtility.SetAnimationClipSettings(clip,settings);clip.EnsureQuaternionContinuity();EditorUtility.SetDirty(clip);
        return clip;
    }
    static void Set(Dictionary<string,float> p,string n,float v){p[n]=v;}
    static void Add(Dictionary<string,float> p,string n,float v){p[n]+=v;}
    static float Sample(float[] values,float t)
    {
        for(int i=1;i<Times.Length;i++)if(t<=Times[i])return Mathf.Lerp(values[i-1],values[i],Mathf.InverseLerp(Times[i-1],Times[i],t));
        return values[values.Length-1];
    }

    // Bake support-foot/hand compensation into the clips; no runtime IK component is needed.
    static void Ground(AnimationClip clip,string key)
    {
        if(key.StartsWith("Fail_"))key=key.Substring(5);
        var scene=UnityEditor.SceneManagement.EditorSceneManager.NewPreviewScene();
        var instance=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2.Model/Prefabs/Player.prefab"));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(instance,scene);
        instance.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);instance.transform.localScale=Vector3.one;
        var a=instance.GetComponent<Animator>();a.runtimeAnimatorController=null;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var graph=PlayableGraph.Create("Bake dance support");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        try
        {
            var p=AnimationClipPlayable.Create(graph,clip);p.SetApplyFootIK(false);
            var output=AnimationPlayableOutput.Create(graph,"Pose",a);output.SetSourcePlayable(p);graph.Play();
            p.SetTime(0);graph.Evaluate(0);
            Vector3 left=a.GetBoneTransform(HumanBodyBones.LeftFoot).position,right=a.GetBoneTransform(HumanBodyBones.RightFoot).position;
            float ground=Mathf.Min(left.y,right.y);
            var bindings=new[]{"RootT.x","RootT.y","RootT.z"}.Select(n=>EditorCurveBinding.FloatCurve("",typeof(Animator),n)).ToArray();
            var original=bindings.Select(b=>AnimationUtility.GetEditorCurve(clip,b)).ToArray();
            var curves=new[]{new AnimationCurve(),new AnimationCurve(),new AnimationCurve()};
            for(int i=0;i<=120;i++)
            {
                float t=i/120f;p.SetTime(t);graph.Evaluate(0);
                var l=a.GetBoneTransform(HumanBodyBones.LeftFoot).position;var r=a.GetBoneTransform(HumanBodyBones.RightFoot).position;
                var lh=a.GetBoneTransform(HumanBodyBones.LeftHand).position;var rh=a.GetBoneTransform(HumanBodyBones.RightHand).position;
                Vector3 support=key=="A"?r:key=="D"?l:(l+r)*.5f;
                Vector3 reference=key=="A"?right:key=="D"?left:(left+right)*.5f;
                float contact=Mathf.Min(l.y,r.y)-ground;
                if(key=="S")
                {
                    Vector3 hand=lh.y<rh.y?lh:rh;
                    float weight=Mathf.InverseLerp(.08f,-.06f,hand.y-Mathf.Min(l.y,r.y));
                    support=Vector3.Lerp(support,hand,weight);
                    reference=Vector3.Lerp(reference,new Vector3(0,0,.08f),weight);
                    contact=Mathf.Min(contact,hand.y-.035f);
                }
                var correction=new Vector3(reference.x-support.x,-contact,reference.z-support.z)/a.humanScale;
                for(int c=0;c<3;c++)curves[c].AddKey(new Keyframe(t,original[c].Evaluate(t)+correction[c]));
            }
            graph.Destroy();
            for(int c=0;c<3;c++)
            {
                for(int i=0;i<curves[c].length;i++)curves[c].SmoothTangents(i,0f);
                var first=curves[c].keys[0];var last=curves[c].keys[curves[c].length-1];last.value=first.value;
                float slope=(curves[c].keys[1].value-curves[c].keys[curves[c].length-2].value)*60f;
                first.inTangent=first.outTangent=last.inTangent=last.outTangent=slope;
                curves[c].MoveKey(0,first);curves[c].MoveKey(curves[c].length-1,last);
                AnimationUtility.SetEditorCurve(clip,bindings[c],curves[c]);
            }
            EditorUtility.SetDirty(clip);
        }
        finally
        {
            if(graph.IsValid())graph.Destroy();UnityEngine.Object.DestroyImmediate(instance);
            UnityEditor.SceneManagement.EditorSceneManager.ClosePreviewScene(scene);
        }
    }

    public static string Render(bool motion=false,bool revision=false)
    {
        string root=revision?"Temp/dance-failure":"Temp/dance-authoring";
        string renderFolder=root+(motion?"/previews-motion":"/previews");
        Directory.CreateDirectory(renderFolder);
        var prefab=AssetDatabase.LoadAssetAtPath<GameObject>("Assets/2.Model/Prefabs/Player.prefab");
        var preview=new PreviewRenderUtility();
        var instance=UnityEngine.Object.Instantiate(prefab);
        preview.AddSingleGO(instance);
        instance.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);instance.transform.localScale=Vector3.one;
        var animator=instance.GetComponent<Animator>();animator.runtimeAnimatorController=null;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var skins=instance.GetComponentsInChildren<SkinnedMeshRenderer>();
        var baked=skins.Select(s=>new Mesh()).ToArray();
        var staticRenderers=skins.Select(s=>{
            var o=new GameObject("Sampled skin");o.transform.SetParent(s.transform,false);
            var scale=s.transform.lossyScale;o.transform.localScale=new Vector3(1f/scale.x,1f/scale.y,1f/scale.z);
            o.AddComponent<MeshFilter>();var r=o.AddComponent<MeshRenderer>();r.sharedMaterials=s.sharedMaterials;
            s.enabled=false;s.updateWhenOffscreen=true;return r;
        }).ToArray();
        preview.camera.backgroundColor=new Color(.075f,.085f,.11f,1f);preview.camera.clearFlags=CameraClearFlags.SolidColor;
        preview.camera.transform.position=new Vector3(1.0f,1.12f,2.8f);preview.camera.transform.LookAt(new Vector3(0,.7f,0));
        preview.camera.orthographic=true;preview.camera.orthographicSize=1.08f;preview.camera.nearClipPlane=.01f;preview.camera.farClipPlane=20f;
        preview.lights[0].intensity=1.25f;preview.lights[0].transform.rotation=Quaternion.Euler(35,210,0);
        preview.lights[1].intensity=.9f;preview.lights[1].transform.rotation=Quaternion.Euler(30,35,0);preview.ambientColor=Color.gray;
        var paths=AssetDatabase.FindAssets("t:AnimationClip",new[]{Folder}).Select(AssetDatabase.GUIDToAssetPath).Where(x=>(!motion||!x.Contains("Idle"))&&(!revision||x.Contains("DanceFail_")||x.Contains("Dance_W_"))).OrderBy(x=>x).ToArray();
        var report=new List<object>();
        try
        {
            foreach(var path in paths)
            {
                var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
                var graph=PlayableGraph.Create("DancePreview");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
                var rows=new List<object>();
                try
                {
                    var playable=AnimationClipPlayable.Create(graph,clip);playable.SetApplyFootIK(false);
                    var output=AnimationPlayableOutput.Create(graph,"Pose",animator);output.SetSourcePlayable(playable);graph.Play();
                    foreach(float t in motion?Enumerable.Range(0,25).Select(i=>i/25f).ToArray():new[]{0f,.10f,.26f,.37f,.52f,.74f,.99f})
                    {
                        playable.SetTime(t);graph.Evaluate(0f);
                        for(int s=0;s<skins.Length;s++){skins[s].BakeMesh(baked[s]);staticRenderers[s].GetComponent<MeshFilter>().sharedMesh=baked[s];}
                        preview.BeginStaticPreview(new Rect(0,0,384,384));preview.Render(true);
                        var image=preview.EndStaticPreview();
                        File.WriteAllBytes(renderFolder+"/"+clip.name+"_"+t.ToString("0.00",System.Globalization.CultureInfo.InvariantCulture)+".png",image.EncodeToPNG());UnityEngine.Object.DestroyImmediate(image);
                        var bones=new[]{HumanBodyBones.Head,HumanBodyBones.LeftFoot,HumanBodyBones.RightFoot,HumanBodyBones.LeftHand,HumanBodyBones.RightHand};
                        rows.Add(new{t,bones=bones.ToDictionary(b=>b.ToString(),b=>{var v=animator.GetBoneTransform(b).position;return new[]{v.x,v.y,v.z};})});
                    }
                }finally{graph.Destroy();}
                report.Add(new{path,poses=rows});
            }
            File.WriteAllText(root+(motion?"/motion-poses.json":"/preview-poses.json"),Newtonsoft.Json.JsonConvert.SerializeObject(report));
        }finally{preview.Cleanup();foreach(var mesh in baked)UnityEngine.Object.DestroyImmediate(mesh);}
        return "Rendered "+paths.Length+" clips to "+renderFolder;
    }
}
