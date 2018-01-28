﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class ShadowSimulation : AttackSkill {

    public int duration;
    
    //影子动画字典
    Dictionary<Material, TweenInfo> tweenDic = new Dictionary<Material, TweenInfo>();
    
    public override void SetLevel(int level)
    {
        duration = level + 2;
    }
    
    public override void Effect()
    {
        //不飘字
        calculateDamage = false;
        
        //给自身施加禁止buff
        var buff = new BanBuff(duration);
        character.GetComponent<Unit>().Buffs.Add(buff);
        buff.Apply(character);
        

        base.Effect();

        animator.speed = 0;

        
        CreatePoint(character);
        float timeline = 0;
        for(int i = 0; i < other.Count; i++)
        {
            var o = other[i];
            //侧边
            if ((int)Vector3.Angle((o.position - character.position).normalized, character.forward) % 90 != 0)
            {
                timeline = timeline + 0.3f;
                //确定两边
                GameObject go = null;
                RoundManager.GetInstance().Invoke(() => { go = CreateMesh(character.position, o.position); }, timeline);
                if (DamageSystem.Miss(character, o, skillRate))
                {
                    RoundManager.GetInstance().Invoke(() => {
                        TweenSync.GetInstance().CreateTween(go.GetComponentInChildren<MeshRenderer>().material, 0f, new TweenInfo("_TilingY"));
                        DebugLogPanel.GetInstance().Log("Miss");
                        
                        RoundManager.GetInstance().Invoke(() => { GameObject.Destroy(go); }, 1f);
                        }, timeline + 1);
                }
                else
                {
                    RoundManager.GetInstance().Invoke(() => {
                        CreatePoint(o);
                        var banBuff = new BanBuff(duration);
                        o.GetComponent<Unit>().Buffs.Add(banBuff);
                        banBuff.Apply(o);
                    }, timeline + 1);
                    
                }
            }
            else
            {
                GameObject go = CreateLine(character.position, o.position);

                if (DamageSystem.Miss(character, o, skillRate))
                {
                    RoundManager.GetInstance().Invoke(() => {
                        TweenSync.GetInstance().CreateTween(go.GetComponentInChildren<MeshRenderer>().material, 0f, new TweenInfo("_TilingY"));
                        DebugLogPanel.GetInstance().Log("Miss");
                        RoundManager.GetInstance().Invoke(() => { GameObject.Destroy(go); }, 1f);
                    }, 1);
                }
                else
                {
                    RoundManager.GetInstance().Invoke(() => {
                        CreatePoint(o);
                        var banBuff = new BanBuff(duration);
                        o.GetComponent<Unit>().Buffs.Add(banBuff);
                        banBuff.Apply(o);
                    }, 1);
                    
                }
            }
        }

        RoundManager.GetInstance().Invoke(() => { animator.speed = 1; }, timeline + 2);

    }

    public override void GetHit()
    {
        
    }
    
    public override List<string> LogSkillEffect()
    {
        string title = "";
        string info = "";
        string durationInfo = duration.ToString();
        List<string> s = new List<string>
        {
            title,
            info,
            durationInfo
        };
        return s;
    }
    
    //创建脚下阴影
    void CreatePoint(Transform parent)
    {
        var p = Resources.Load("Prefabs/Point");
        var point = GameObject.Instantiate(p, parent) as GameObject;
        point.name = "阴影";
        
        var meshes = parent.GetComponentsInChildren<SkinnedMeshRenderer>();

        //关闭原本阴影
        foreach (var m in meshes)
        {
            m.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }

        var mat = point.GetComponentInChildren<MeshRenderer>().material;

        TweenSync.GetInstance().CreateTween(mat, 1f, new TweenInfo("_N_mask"));
    }

    //创建直线阴影
    GameObject CreateLine(Vector3 from,Vector3 to)
    {
        //适配末梢位置在脚下
        to = to + (to - from).normalized * 0.08f * (to - from).magnitude;

        var l = Resources.Load("Prefabs/Line");
        var line = GameObject.Instantiate(l, from, character.rotation) as GameObject;

        

        line.transform.forward = (to - from).normalized;

        line.transform.localScale = new Vector3(1, 1, (to - from).magnitude);

        var mat = line.GetComponentInChildren<MeshRenderer>().material;
        
        TweenSync.GetInstance().CreateTween(mat, 1f, new TweenInfo("_TilingY"));

        return line;
    }

    ////创建弯曲阴影
    ////一条贝塞尔
    //GameObject CreateMesh(Vector3 from, Vector3 to)
    //{
    //    var bezierMesh = new DrawMesh();
    //    var bezier = bezierMesh.DrawBezierMesh(from, to - character.right * (focus - character.position).magnitude / 3, to, 0.15f);

    //    tweenDic.Add(bezier.GetComponentInChildren<MeshRenderer>().material, new TweenInfo("_TilingY"));
    //    CreateTween(bezier.GetComponentInChildren<MeshRenderer>().material);

    //    return bezier;
    //}

    //创建弯曲阴影
    GameObject CreateMesh(Vector3 from, Vector3 to)
    {
        //适配末梢位置在脚下
        to = to + (to - from).normalized * 0.04f * (to - from).magnitude;

        var bezierMesh = new DrawMesh();
        var p11 = from + character.forward * (to - from).magnitude * 1 / 6;
        var p12 = to - character.forward * (to - from).magnitude * 1 / 6;
        var bezier = bezierMesh.DrawDoubleBezierMesh(from, p11, p12, to, 0.15f);

        //物理忽略
        bezier.layer = 2;

        var mat = bezier.GetComponentInChildren<MeshRenderer>().material;
        
        TweenSync.GetInstance().CreateTween(mat, 1f, new TweenInfo("_TilingY"));

        return bezier;
    }

}