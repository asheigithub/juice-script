## 简介

juice-script 是我之前项目 **apple-juice-actionscript** 的 2.0 版延续与重构。

与之前项目相比，主要关注于解释器的效率
### 解释运行box2dlite，性能稳定
<img width="640" height="360" alt="box2dlite 2026-08-17 16-17-45" src="https://github.com/user-attachments/assets/49cb0d46-e513-4618-9554-2d09517bb2dc" />


- 运行时增加了一个轻量级的逃逸分析,很多小对象可以随意new,不会触发GC；
- 追加了 `struct` 结构体类型，用于更贴近底层的值类型建模；
- 支持增量编译，方便在较大工程中做局部重建；
- 后端加入了 SSA 优化管线，在保持实现可控的前提下做基础的代码优化。

语法方面：

- 新增了 `yield` 以及 `async` / `await` 语义，用于更自然地表达协程与异步流程；
- 基本类型扩展了 `float`、`byte`、`sbyte` 等，用来覆盖更细粒度的数值场景；
- 类的内存布局与 C 语言对齐规则保持一致，方便与原生世界进行数据结构互通。

总体来说，希望它能成为一个好用的嵌入式脚本。

## 为什么要做 2.0

动机其实很简单：在 apple-juice-actionscript 里有非常多的不足。

juice-script 是一次把当初没来得及做好的部分补齐、把能进化的地方继续进化的过程。运行时加入了轻量级逃逸分析、结构体值类型、增量编译、SSA 后端优化；语言层面补上了 yield、async/await、float、byte、sbyte 等更细粒度的类型与语义；类的内存布局也改成与 C 语言对齐一致。可以直接透传给C函数，例如glBuffer

还有一个更简单的理由：  
**有一个自己完全掌控的脚本系统很爽。**


## 快速使用说明

juice-script 的第一次使用需要完成两步初始化：**构建所有工程** 与 **编译基础库**。

### 1. 构建所有项目（第一次使用必做）
在根目录执行：
dotnet build juice-script-2.sln


所有项目成功编译后，会生成：

- 编译器（compiler）
- 运行时（runtime）


### 2. 编译基础库（global_swc）
juice-script 的运行时依赖一套基础库（包含 `trace`、`Math`、`Promise`、`IteratorResult` 等对象），源码位于：

fd_projs/dev_scripts/src

第一次使用时，需要手动使用 `asc.exe` 编译这部分代码，生成全局 SWC 包。

示例命令如下（路径请按你的本地环境调整）：

` asc.exe -r F:\GitHub\juice-script-2\juice-script-2\fd_projs\juice_global\src -w global_swc -o F:\GitHub\juice-script-2\juice-script-2\player\bin\Debug\net6.0\juice_global.swc -f `


完成后，`player` 需要加载 `juice_global.swc`，从而具备基础对象模型与内建函数。

### 3. VS 工程配置说明
解决方案中包含多套构建配置：

- **Debug / Release**  
  用于构建编译器（compiler）运行时 (player) 工具链（juice.exe）。

- **ProfilePlayer**  
  构建带有性能计数的Player,可统计指令的耗时和函数的耗时。

## Samples: Box2DLite

`samples/box2dlite` 是一个完整的示例，展示如何用 C# (OpenGL) 加载 JuiceScript 脚本并运行 Box2D-Lite 物理引擎模拟。

- .net10的运行性能比.net6快至少20%
- 为了最大化性能我展示了如何注入C#函数，同时启用了内置的Vector2,Matrix2x2。但是即使完全用脚本进行向量矩阵运算，同样可以支持金字塔堆叠35个以上刚体。

### 架构概览

- **AS3 脚本** (`fd_projs/dev_scripts/box2d-lite/src/`)：Box2D-Lite 物理引擎的核心逻辑（Body、Joint、World、碰撞检测等），用 JuiceScript 的 ActionScript 3 语法编写
- **C# 宿主** (`samples/box2dlite/Program.cs`)：通过 Silk.NET 创建 OpenGL 窗口，加载编译好的 SWC 脚本，驱动物理模拟并渲染结果
- **Native 函数桥接**：C# 端通过 `[NativeFunction]` 标记将性能关键的数学运算（向量叉乘、绝对值等）直接暴露给脚本层调用

### 编译步骤

#### Step 1: 构建所有项目

```bash
dotnet build juice-script-2.sln --configuration Debug
```

#### Step 2: 编译全局库 juice_global.swc

如果尚未编译过，需要先生成运行时基础库：

```bash
./asc/bin/Debug/net6.0/asc.exe -r fd_projs/juice_global/src -w player/bin/Debug/net6.0 -o player/bin/Debug/net6.0/juice_global.swc -f
```

#### Step 3: 编译 box2d-lite 脚本

```bash
./asc/bin/Debug/net6.0/asc.exe -r fd_projs/dev_scripts/box2d-lite/src -w fd_projs/dev_scripts/box2d-lite/obj -l player/bin/Debug/net6.0/juice_global.swc -o fd_projs/dev_scripts/box2d-lite/obj/o.swc -f
```

编译产物为 `fd_projs/dev_scripts/box2d-lite/obj/o.swc`，C# 宿主程序会从该路径加载。

#### Step 4: 构建并运行 box2dlite 示例

```bash
dotnet build samples/box2dlite/box2dlite.csproj --configuration Debug
dotnet run --project samples/box2dlite/box2dlite.csproj --configuration Debug
```

### 运行操作

| 按键 | 功能 |
|------|------|
| `1` - `9` | 切换不同 Demo 场景（单盒、单摆、摩擦系数、堆叠、金字塔、跷跷板、悬挂桥、多米诺、多摆） |
| `Space` | 发射随机物体（bomb） |
| `Esc` | 退出 |

### 依赖

- **Silk.NET 2.23.0**：窗口创建和 OpenGL 绑定（NuGet 自动恢复）








