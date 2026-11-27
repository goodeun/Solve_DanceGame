using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

// Authoring only. Pipeline: script run --file AgentScripts/RivalDanceBuilder.cs --entry RivalDanceBuilder.Build
public static class RivalDanceBuilder
{
    public const string Folder="Assets/5.Animation/Rival";
    public const string Prefab="Assets/2.Model/Prefabs/Rival.prefab";
    public const float Bpm=128f, Beats=320f, Length=150f;
    public static readonly string[] Sections={"Low Groove","Step Scoop","Diagonal Reach","Chest Box","Overhead Sweep","Cross Open","Knee Drive","Quarter Pivot","Side Push","Wave Bridge"};
    static string Property(string muscle)
    {
        var parts=muscle.Split(' ');
        if(parts.Length>=3&&(parts[0]=="Left"||parts[0]=="Right")&&new[]{"Thumb","Index","Middle","Ring","Little"}.Contains(parts[1]))
            return parts[0]+"Hand."+parts[1]+"."+string.Join(" ",parts.Skip(2));
        return muscle;
    }
    static readonly string[] Properties=HumanTrait.MuscleName.Select(Property).ToArray();
    static readonly Dictionary<string,int> Indices=HumanTrait.MuscleName.Select((n,i)=>new{n,i}).ToDictionary(x=>x.n,x=>x.i);
    static float[] Baseline()
    {
        var pose=new float[HumanTrait.MuscleCount];
        Put(pose,"Spine Front-Back",-.06f);Put(pose,"Chest Front-Back",-.03f);
        Put(pose,"Neck Nod Down-Up",.04f);Put(pose,"Head Nod Down-Up",.04f);
        foreach(var side in new[]{"Left","Right"})
        {
            Put(pose,side+" Upper Leg Front-Back",.37f);Put(pose,side+" Lower Leg Stretch",.74f);
            Put(pose,side+" Lower Leg Twist In-Out",.24f);Put(pose,side+" Foot Up-Down",-.13f);
            Put(pose,side+" Shoulder Front-Back",-.05f);Put(pose,side+" Arm Down-Up",-.55f);
            Put(pose,side+" Arm Front-Back",.05f);Put(pose,side+" Arm Twist In-Out",-.05f);
            Put(pose,side+" Forearm Stretch",.65f);Put(pose,side+" Forearm Twist In-Out",.10f);
            Put(pose,side+" Hand In-Out",.12f);
            foreach(var finger in new[]{"Thumb","Index","Middle","Ring","Little"})
            {
                Put(pose,side+" "+finger+" 1 Stretched",.25f);Put(pose,side+" "+finger+" 2 Stretched",.35f);
                Put(pose,side+" "+finger+" 3 Stretched",.45f);Put(pose,side+" "+finger+" Spread",.05f);
            }
        }
        return pose;
    }
    static void Put(float[] p,string name,float v){if(Indices.TryGetValue(name,out var i))p[i]=v;}
    static void Add(float[] p,string name,float v){if(Indices.TryGetValue(name,out var i))p[i]+=v;}
    static float S(float x)=>Mathf.Sin(x*Mathf.PI*2f);
    static float C(float x)=>Mathf.Cos(x*Mathf.PI*2f);

    static float[] Motif(int section,float beat)
    {
        var p=Baseline();float sway=S(beat/4f),counter=S(beat/4f-.09f),bounce=C(beat),open=.5f-.5f*C(beat/4f);
        float left=.5f+.5f*S(beat/4f),right=1f-left;
        float phrase=Mathf.Repeat(beat,32f)/32f,energy=.88f+.12f*Mathf.Sin(phrase*Mathf.PI);
        Add(p,"Spine Left-Right",.14f*sway);Add(p,"Chest Left-Right",-.09f*counter);
        Add(p,"Spine Twist Left-Right",.09f*counter);Add(p,"Chest Twist Left-Right",-.08f*counter);
        Add(p,"Head Tilt Left-Right",.055f*counter);Add(p,"Head Turn Left-Right",.12f*S(beat/8f));
        Add(p,"Neck Nod Down-Up",.035f*bounce);
        foreach(var side in new[]{"Left","Right"})
        {
            float sign=side=="Left"?-1f:1f,reach=side=="Left"?left:right,other=1f-reach;
            switch(section)
            {
                case 0:
                    Add(p,side+" Arm Down-Up",.12f*(.5f+.5f*sign*sway));
                    Add(p,side+" Forearm Stretch",-.20f+.10f*sign*counter);Add(p,side+" Hand In-Out",.18f*sign*counter);break;
                case 1:
                    Add(p,side+" Arm Down-Up",.30f*open);Add(p,side+" Arm Front-Back",-.35f*S(beat/4f));
                    Add(p,side+" Forearm Stretch",.20f*open);Add(p,side+" Hand Down-Up",.18f*S(beat/4f-.12f));break;
                case 2:
                    Add(p,side+" Arm Down-Up",1.40f*reach);Add(p,side+" Shoulder Down-Up",.20f*reach);
                    Add(p,side+" Arm Front-Back",-.20f*reach);Add(p,side+" Forearm Stretch",.60f*reach-.18f*other);break;
                case 3:
                    Add(p,side+" Arm Down-Up",.33f);Add(p,side+" Forearm Stretch",-.35f+.20f*sign*sway);
                    Add(p,side+" Arm Front-Back",-.26f*sign*sway);Add(p,side+" Hand Down-Up",.20f*sign*counter);break;
                case 4:
                    float lift=.64f+.30f*S(beat/8f+sign*.06f);
                    Add(p,side+" Arm Down-Up",1.65f*lift);Add(p,side+" Shoulder Down-Up",.30f*lift);
                    Add(p,side+" Forearm Stretch",.65f*lift);Add(p,side+" Arm Front-Back",-.15f*counter);break;
                case 5:
                    Add(p,side+" Arm Down-Up",.35f+.28f*open);Add(p,side+" Arm Front-Back",-.44f*(1f-open));
                    Add(p,side+" Forearm Stretch",-.30f+.55f*open);Add(p,side+" Hand In-Out",-.20f*(1f-open));break;
                case 6:
                    Add(p,side+" Arm Down-Up",.18f+.12f*reach);Add(p,side+" Arm Front-Back",-.38f*sign*S(beat/2f));
                    Add(p,side+" Forearm Stretch",-.35f+.20f*other);Add(p,side+" Shoulder Down-Up",.10f*reach);break;
                case 7:
                    Add(p,side+" Arm Down-Up",.36f*reach);Add(p,side+" Arm Front-Back",-.30f*counter);
                    Add(p,side+" Forearm Stretch",-.22f*other);Add(p,side+" Hand Down-Up",.25f*S(beat/4f+sign*.15f));break;
                case 8:
                    Add(p,side+" Arm Down-Up",.95f*reach);Add(p,side+" Arm Front-Back",-.28f*other);
                    Add(p,side+" Forearm Stretch",.60f*reach-.30f*other);Add(p,side+" Shoulder Front-Back",.15f*reach);break;
                case 9:
                    Add(p,side+" Arm Down-Up",.25f+.18f*S(beat/4f+sign*.08f));
                    Add(p,side+" Forearm Stretch",-.20f+.25f*S(beat/4f-.15f));
                    Add(p,side+" Hand Down-Up",.28f*S(beat/4f-.25f));break;
            }
        }
        if(section==3){Add(p,"Spine Front-Back",.10f*S(beat/2f));Add(p,"Chest Front-Back",.18f*C(beat/2f));}
        if(section==8)Add(p,"Chest Twist Left-Right",.20f*sway);
        if(section==9){Add(p,"Spine Front-Back",.14f*S(beat/4f));Add(p,"Chest Front-Back",.17f*S(beat/4f-.10f));Add(p,"Head Nod Down-Up",.10f*S(beat/4f-.20f));}
        var baseline=Baseline();for(int i=0;i<p.Length;i++)p[i]=Mathf.Clamp(Mathf.Lerp(baseline[i],p[i],energy),-.98f,.98f);
        return p;
    }
    static void Section(float beat,out int current,out int next,out float mix)
    {
        beat=Mathf.Repeat(beat,Beats);current=Mathf.FloorToInt(beat/32f);next=(current+1)%Sections.Length;
        mix=Mathf.SmoothStep(0,1,Mathf.InverseLerp(30f,32f,Mathf.Repeat(beat,32f)));
    }
    static void Pose(float beat,out float[] muscles,out Vector3 body,out Quaternion rotation)
    {
        Section(beat,out int section,out int next,out float blend);
        var p=Motif(section,beat);var q=Motif(next,beat);
        for(int i=0;i<p.Length;i++)p[i]=Mathf.Lerp(p[i],q[i],blend);
        float pivot=Mathf.Lerp(section==7?22f:7f,next==7?22f:7f,blend);
        float lower=Mathf.Lerp(section==2||section==4||section==8?.015f:0f,next==2||next==4||next==8?.015f:0f,blend);
        body=new Vector3(.035f*S(beat/8f),.906f-lower-.018f*C(beat),0f);
        rotation=Quaternion.Euler(0,pivot*S(beat/8f),2.5f*S(beat/4f));muscles=p;
    }

    public static string Build()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode before building");
        if(!AssetDatabase.IsValidFolder(Folder))AssetDatabase.CreateFolder("Assets/5.Animation","Rival");
        Directory.CreateDirectory("Temp/rival-dance");
        var raw=new AnimationClip{name="Rival construction pose",frameRate=30};
        const int sourceSteps=2560;var sourceCurves=Enumerable.Range(0,HumanTrait.MuscleCount+7).Select(_=>new AnimationCurve()).ToArray();
        for(int i=0;i<=sourceSteps;i++)
        {
            float beat=Beats*i/sourceSteps;Pose(beat,out var muscles,out var body,out var rotation);float t=beat*60f/Bpm;
            for(int m=0;m<muscles.Length;m++)sourceCurves[m].AddKey(t,muscles[m]);
            float[] root={body.x,body.y,body.z,rotation.x,rotation.y,rotation.z,rotation.w};
            for(int j=0;j<7;j++)sourceCurves[muscles.Length+j].AddKey(t,root[j]);
        }
        for(int i=0;i<sourceCurves.Length;i++)SetCurve(raw,Name(i),sourceCurves[i],true);
        Settings(raw,true,Length);
        var clip=BakeFeet(raw);
        var ready=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/Rival_Ready.anim");
        if(ready==null){ready=new AnimationClip();AssetDatabase.CreateAsset(ready,Folder+"/Rival_Ready.anim");}else ready.ClearCurves();
        ready.name="Rival_Ready";ready.frameRate=30;
        foreach(var binding in AnimationUtility.GetCurveBindings(clip))
        {
            float v=AnimationUtility.GetEditorCurve(clip,binding).Evaluate(0);
            AnimationUtility.SetEditorCurve(ready,binding,AnimationCurve.Constant(0,1,v));
        }
        Settings(ready,true,1f);EditorUtility.SetDirty(ready);
        string controllerPath=Folder+"/Rival_Dance.controller";
        var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>(controllerPath);
        if(controller==null)controller=AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine=controller.layers[0].stateMachine;
        var readyState=machine.states.Select(x=>x.state).FirstOrDefault(s=>s.name=="Rival_Ready")??machine.AddState("Rival_Ready",new Vector3(250,80));
        var loopState=machine.states.Select(x=>x.state).FirstOrDefault(s=>s.name=="Rival_DanceLoop")??machine.AddState("Rival_DanceLoop",new Vector3(520,80));
        readyState.motion=ready;loopState.motion=clip;machine.defaultState=readyState;
        EditorUtility.SetDirty(readyState);EditorUtility.SetDirty(loopState);EditorUtility.SetDirty(controller);AssetDatabase.SaveAssets();
        UnityEngine.Object.DestroyImmediate(raw);
        return "Built Rival 150-second / 320-beat / 80-bar loop, ten sections, offline support-foot IK, ready pose and controller.";
    }
    static string Name(int i)=>i<Properties.Length?Properties[i]:new[]{"RootT.x","RootT.y","RootT.z","RootQ.x","RootQ.y","RootQ.z","RootQ.w"}[i-Properties.Length];
    static void Settings(AnimationClip clip,bool loop,float length)
    {
        var s=AnimationUtility.GetAnimationClipSettings(clip);s.startTime=0;s.stopTime=length;s.loopTime=loop;s.loopBlend=false;
        s.keepOriginalOrientation=true;s.keepOriginalPositionXZ=true;s.keepOriginalPositionY=true;
        s.loopBlendOrientation=true;s.loopBlendPositionXZ=true;s.loopBlendPositionY=false;
        AnimationUtility.SetAnimationClipSettings(clip,s);clip.EnsureQuaternionContinuity();
    }
    static void SetCurve(AnimationClip clip,string name,AnimationCurve curve,bool smooth)
    {
        if(smooth)for(int i=0;i<curve.length;i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);
            AnimationUtility.SetKeyRightTangentMode(curve,i,AnimationUtility.TangentMode.ClampedAuto);
        }
        AnimationUtility.SetEditorCurve(clip,EditorCurveBinding.FloatCurve("",typeof(Animator),name),curve);
    }
    static AnimationClip BakeFeet(AnimationClip raw)
    {
        string path=Folder+"/Rival_ClubFusion_80Bars_128BPM.anim";
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if(clip==null){clip=new AnimationClip();AssetDatabase.CreateAsset(clip,path);}else clip.ClearCurves();
        clip.name="Rival_ClubFusion_80Bars_128BPM";clip.frameRate=30;
        var scene=EditorSceneManager.NewPreviewScene();var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);go.transform.localScale=Vector3.one;
        var animator=go.GetComponent<Animator>();animator.runtimeAnimatorController=null;animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var graph=PlayableGraph.Create("Bake Rival feet");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        var handler=new HumanPoseHandler(animator.avatar,animator.transform);
        try
        {
            var playable=AnimationClipPlayable.Create(graph,raw);playable.SetApplyFootIK(false);
            var output=AnimationPlayableOutput.Create(graph,"Pose",animator);output.SetSourcePlayable(playable);graph.Play();playable.SetTime(0);graph.Evaluate(0);
            var lf=animator.GetBoneTransform(HumanBodyBones.LeftFoot);var rf=animator.GetBoneTransform(HumanBodyBones.RightFoot);
            var originL=lf.position;var originR=rf.position;originL.x-=.012f;originR.x+=.012f;
            var rotationL=lf.rotation;var rotationR=rf.rotation;
            const int samples=4500;var values=Enumerable.Range(0,HumanTrait.MuscleCount+7).Select(_=>new float[samples+1]).ToArray();
            var pose=new HumanPose();float maxReach=0f;
            for(int i=0;i<=samples;i++)
            {
                float time=Length*i/samples,beat=time*Bpm/60f;
                playable.SetTime(time);graph.Evaluate(0);
                FootGoals(beat,originL,originR,out var l,out var r);
                maxReach=Mathf.Max(maxReach,SolveLeg(animator,true,l,rotationL),SolveLeg(animator,false,r,rotationR));
                handler.GetHumanPose(ref pose);
                for(int m=0;m<pose.muscles.Length;m++)values[m][i]=pose.muscles[m];
                var root=new[]{pose.bodyPosition.x,pose.bodyPosition.y,pose.bodyPosition.z,pose.bodyRotation.x,pose.bodyRotation.y,pose.bodyRotation.z,pose.bodyRotation.w};
                for(int j=0;j<7;j++)values[pose.muscles.Length+j][i]=root[j];
            }
            // The exact initial pose is the exact final pose; match endpoint slopes as well.
            for(int m=0;m<values.Length;m++)
            {
                values[m][samples]=values[m][0];
                float tolerance=m<Properties.Length?.0025f:.00035f;
                var keys=Reduce(values[m],Length,tolerance);
                var curve=new AnimationCurve(keys);
                for(int k=0;k<curve.length;k++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve,k,AnimationUtility.TangentMode.ClampedAuto);
                    AnimationUtility.SetKeyRightTangentMode(curve,k,AnimationUtility.TangentMode.ClampedAuto);
                }
                var first=curve.keys[0];var last=curve.keys[curve.length-1];
                float slope=((values[m][1]-values[m][0])+(values[m][samples]-values[m][samples-1]))*15f;
                first.inTangent=first.outTangent=last.inTangent=last.outTangent=slope;curve.MoveKey(0,first);curve.MoveKey(curve.length-1,last);
                SetCurve(clip,Name(m),curve,false);
            }
            Settings(clip,true,Length);EditorUtility.SetDirty(clip);
            File.WriteAllText("Temp/rival-dance/bake.json",Newtonsoft.Json.JsonConvert.SerializeObject(new{length=Length,beats=Beats,bpm=Bpm,sections=Sections,samples,maxUnreachableDistance=maxReach,keys=AnimationUtility.GetCurveBindings(clip).Sum(b=>AnimationUtility.GetEditorCurve(clip,b).length)}));
        }
        finally{handler.Dispose();graph.Destroy();UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
        return clip;
    }
    static void FootGoals(float beat,Vector3 originL,Vector3 originR,out Vector3 left,out Vector3 right)
    {
        Section(beat,out int section,out int next,out float mix);
        Goals(section,beat,originL,originR,out var a,out var b);Goals(next,beat,originL,originR,out var c,out var d);
        left=Vector3.Lerp(a,c,mix);right=Vector3.Lerp(b,d,mix);
    }
    static void Goals(int section,float beat,Vector3 l,Vector3 r,out Vector3 left,out Vector3 right)
    {
        float phase=Mathf.Repeat(section==4?beat*2f:beat,8f);bool mirror=phase>=4f;if(mirror)phase-=4f;
        int step=Mathf.FloorToInt(phase);float u=phase-step,ease=u*u*(3f-2f*u),lift=Mathf.Pow(Mathf.Sin(Mathf.PI*u),2f);
        float stride=section==0?.020f:section==4?.026f:section==6?.020f:section==8?.033f:.025f;
        float height=section==6?.048f:section==0?.012f:section==4?.012f:.020f;
        var leader=mirror?r:l;var follower=mirror?l:r;float direction=mirror?1f:-1f;
        float forward=section==1?.012f:section==2?.016f:section==3?.022f*(mirror?-1f:1f):section==5?.022f:section==7?.012f:0f;
        Vector3 shift=new Vector3(direction*stride,0,forward);
        var leaderGoal=leader;var followerGoal=follower;
        if(step==0){leaderGoal+=shift*ease+Vector3.up*height*lift;}
        if(step==1){leaderGoal+=shift;followerGoal+=shift*ease+Vector3.up*height*.75f*lift;}
        if(step==2){leaderGoal+=shift;followerGoal+=shift*(1-ease)+Vector3.up*height*lift;}
        if(step==3){leaderGoal+=shift*(1-ease)+Vector3.up*height*.75f*lift;}
        if(section==6){if(step%2==0)leaderGoal.z+=.018f*lift;else followerGoal.z+=.018f*lift;}
        left=mirror?followerGoal:leaderGoal;right=mirror?leaderGoal:followerGoal;
    }
    static float SolveLeg(Animator a,bool left,Vector3 target,Quaternion rotation)
    {
        var upper=a.GetBoneTransform(left?HumanBodyBones.LeftUpperLeg:HumanBodyBones.RightUpperLeg);
        var lower=a.GetBoneTransform(left?HumanBodyBones.LeftLowerLeg:HumanBodyBones.RightLowerLeg);
        var foot=a.GetBoneTransform(left?HumanBodyBones.LeftFoot:HumanBodyBones.RightFoot);
        var origin=upper.position;float l1=Vector3.Distance(origin,lower.position),l2=Vector3.Distance(lower.position,foot.position);
        float requested=Vector3.Distance(origin,target),distance=Mathf.Clamp(requested,.005f,l1+l2-.0001f);
        var dir=(target-origin).normalized;var bend=Vector3.ProjectOnPlane(Vector3.forward,dir).normalized;
        if(bend.sqrMagnitude<.1f)bend=Vector3.forward;
        float cos=Mathf.Clamp((l1*l1+distance*distance-l2*l2)/(2*l1*distance),-1,1);
        var knee=origin+dir*(cos*l1)+bend*(Mathf.Sqrt(Mathf.Max(0,1-cos*cos))*l1);
        upper.rotation=Quaternion.FromToRotation(lower.position-origin,knee-origin)*upper.rotation;
        lower.rotation=Quaternion.FromToRotation(foot.position-lower.position,target-lower.position)*lower.rotation;
        foot.rotation=rotation;
        return Mathf.Max(0,requested-distance);
    }
    static Keyframe[] Reduce(float[] values,float length,float tolerance)
    {
        var keep=new SortedSet<int>{0,values.Length-1};
        var stack=new Stack<Vector2Int>();stack.Push(new Vector2Int(0,values.Length-1));
        while(stack.Count>0)
        {
            var span=stack.Pop();float maximum=tolerance;int index=-1;
            for(int i=span.x+1;i<span.y;i++)
            {
                float expected=Mathf.Lerp(values[span.x],values[span.y],(i-span.x)/(float)(span.y-span.x));float error=Mathf.Abs(values[i]-expected);
                if(error>maximum){maximum=error;index=i;}
            }
            if(index>=0){keep.Add(index);stack.Push(new Vector2Int(span.x,index));stack.Push(new Vector2Int(index,span.y));}
        }
        return keep.Select(i=>new Keyframe(length*i/(values.Length-1),values[i])).ToArray();
    }
    public static string ApplyPrefab()
    {
        if(Application.isPlaying)throw new InvalidOperationException("Stop Play Mode");
        var prefab=PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var animator=prefab.GetComponent<Animator>();animator.runtimeAnimatorController=AssetDatabase.LoadAssetAtPath<AnimatorController>(Folder+"/Rival_Dance.controller");
            animator.applyRootMotion=false;
            if(prefab.GetComponent<RivalDanceAnimator>()==null)prefab.AddComponent<RivalDanceAnimator>();
            PrefabUtility.SaveAsPrefabAsset(prefab,Prefab);
        }
        finally{PrefabUtility.UnloadPrefabContents(prefab);}
        return "Applied Rival controller and music-aware playback to existing Rival prefab only; no scene was modified.";
    }

    public static string Validate()
    {
        var clip=AssetDatabase.LoadAssetAtPath<AnimationClip>(Folder+"/Rival_ClubFusion_80Bars_128BPM.anim");
        var bindings=AnimationUtility.GetCurveBindings(clip);
        float seam=bindings.Max(b=>{var c=AnimationUtility.GetEditorCurve(clip,b);return Mathf.Abs(c.Evaluate(0)-c.Evaluate(Length));});
        float slopeSeam=bindings.Max(b=>{var c=AnimationUtility.GetEditorCurve(clip,b);return Mathf.Abs(c.keys[0].outTangent-c.keys[c.length-1].inTangent);});
        if(!clip.humanMotion||!clip.isLooping||Mathf.Abs(clip.length-Length)>.001f||seam>.00001f||slopeSeam>.00001f)throw new InvalidOperationException("Invalid loop data");
        var scene=EditorSceneManager.NewPreviewScene();var go=UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(go,scene);go.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);go.transform.localScale=Vector3.one;
        var a=go.GetComponent<Animator>();a.runtimeAnimatorController=null;a.applyRootMotion=false;a.cullingMode=AnimatorCullingMode.AlwaysAnimate;
        var graph=PlayableGraph.Create("Validate Rival loop");graph.SetTimeUpdateMode(DirectorUpdateMode.Manual);
        try
        {
            var p=AnimationClipPlayable.Create(graph,clip);p.SetApplyFootIK(false);var o=AnimationPlayableOutput.Create(graph,"Pose",a);o.SetSourcePlayable(p);graph.Play();
            p.SetTime(0);graph.Evaluate(0);var bones=a.GetComponentsInChildren<Transform>();var initial=bones.Select(t=>t.localRotation).ToArray();
            float maxBoneAngle=0,maxGroundBelow=0;var lf=a.GetBoneTransform(HumanBodyBones.LeftFoot);var rf=a.GetBoneTransform(HumanBodyBones.RightFoot);
            float floor=Mathf.Min(lf.position.y,rf.position.y);int samples=4500;
            for(int i=0;i<=samples;i++)
            {
                p.SetTime(Length*i/samples);graph.Evaluate(0);
                maxGroundBelow=Mathf.Max(maxGroundBelow,floor-Mathf.Min(lf.position.y,rf.position.y));
                foreach(var t in bones){var v=t.position;if(float.IsNaN(v.x)||float.IsInfinity(v.y))throw new InvalidOperationException("Invalid bone pose");}
            }
            for(int i=0;i<bones.Length;i++)maxBoneAngle=Mathf.Max(maxBoneAngle,Quaternion.Angle(initial[i],bones[i].localRotation));
            if(maxBoneAngle>.05f||go.transform.position.sqrMagnitude>.00001f)throw new InvalidOperationException("Loop pose/root drift");
            var result=new{length=clip.length,bpm=Bpm,beats=Beats,bars=Beats/4f,loop=clip.isLooping,human=clip.humanMotion,curves=bindings.Length,keys=bindings.Sum(b=>AnimationUtility.GetEditorCurve(clip,b).length),curveEndpointError=seam,slopeEndpointError=slopeSeam,boneEndpointDegrees=maxBoneAngle,maxAnkleBelowInitialFloor=maxGroundBelow,samples,rootDrift=go.transform.position.magnitude,runtimeClipBytes=UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(clip)};
            File.WriteAllText("Temp/rival-dance/asset-verification.json",Newtonsoft.Json.JsonConvert.SerializeObject(result,Newtonsoft.Json.Formatting.Indented));
            return Newtonsoft.Json.JsonConvert.SerializeObject(result);
        }
        finally{graph.Destroy();UnityEngine.Object.DestroyImmediate(go);EditorSceneManager.ClosePreviewScene(scene);}
    }
}
