## 简介

juice-script 是我之前项目 **apple-juice-actionscript** 的 2.0 版延续与重构。

相比上一代，这次的主要演进集中在运行时与语言层面：

- 运行时增加了一个轻量级的逃逸分析,很多小对象可以随意new,不会触发GC；
- 追加了 `struct` 结构体类型，用于更贴近底层的值类型建模；
- 支持增量编译，方便在较大工程中做局部重建；
- 后端加入了 SSA 优化管线，在保持实现可控的前提下做基础的代码优化。

语法方面：

- 新增了 `yield` 以及 `async` / `await` 语义，用于更自然地表达协程与异步流程；
- 基本类型扩展了 `float`、`byte`、`sbyte` 等，用来覆盖更细粒度的数值场景；
- 类的内存布局与 C 语言对齐规则保持一致，方便与原生世界进行数据结构互通。

总体来说，希望它能成为一个好用的嵌入式脚本。

### Demo

<video src="samples/box2dlite/box2dlite.mp4" controls width="640"></video>

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
  用于构建编译器（compiler）与工具链（juice.exe）。

- **Debug_Player / Release_Player**  
  用于构建运行时（runtime）与播放器（player）。  
  这两个配置不会生成编译器，只生成可执行的运行时环境。因为编译器其实也依赖于runtime,这两个配置删除了用于编译器部分的代码，所以生成的player是最优化的。








