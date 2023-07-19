# DrawInstancedSystem

keywords: Unity Engine, Draw Mesh Instanced, Graphics.DrawMeshInstancedProcedural, Job System

自定义合批的实例化绘制包装，适用于 Unity 引擎的内置渲染管线，封装了 `UnityEngine.Graphics.DrawMeshInstancedProcedural` 方法。
仓库包含一个 Unity 工程，Packages 文件夹里是可以移植的包，用法样例见 Unity 工程的 `Main` 场景。

## 需求背景

`DrawMeshInstancedProcedural` 这个接口很高效，但如果有很多个提交批次、而每个批次的数量都不多的时候，“提交绘制命令”这个过程本身的开销就无法忽略，需要有某种手段再一次合并这些提交。

## 原理

Unity 内置渲染管线的实例化绘制要求**一对**网格和材质引用，这个组件要求在初始化阶段（至少是物体实例开始绘制之前）构建调度器，调度器持有特定的网格和材质。
然后任何要绘制一批实例的实体在初始化记录自身所属的调度器（按名称寻址），每个这样的实体自身记录要绘制批次的数量、偏移（按变换矩阵）和颜色，提交给调度器统一处理。
对应的着色器内开启了 `UNITY_PROCEDURAL_INSTANCING_ENABLED`，并使用颜色的数组替代颜色字段（通过 `unity_InstanceID` 寻址）。

## 之后做什么？

虽然有计划为 URP 这样的可编程渲染管线定制一个类似的功能，但 Unity 的 `批次渲染组`（`BatchRendererGroup`）似乎做了同样的事情，虽然程序接口对我来说有点难懂。
