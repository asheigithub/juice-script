# 实现计划: NaNBoxing LocalString

## 概述

本实现计划将NaNBoxing LocalString功能分解为离散的编码步骤，每个任务都建立在前面的步骤之上，最终实现完整的LocalString功能。LocalString主要用于优化运行时动态生成的小字符串（如toString操作），而编译时的字符串字面量将继续使用现有的堆分配机制。实现将专注于核心NaNBoxing系统的扩展，并确保与现有字符串处理机制的无缝集成。

## 任务

- [x] 1. 扩展NaNBoxing类型系统以支持LocalString
  - 在NaNBoxing.cs中添加TAG_LOCAL_STRING常量定义
  - 在BoxType枚举中添加LocalString类型
  - 确保类型标签值不与现有类型冲突
  - _需求: 1.1, 1.2, 1.3_

- [ ]* 1.1 为类型系统扩展编写属性测试
  - 在compilerTests项目中添加LocalString类型系统测试
  - **属性 3: 类型标签唯一性**
  - **验证: 需求 1.3**

- [x] 2. 实现LocalString的核心存储和读取方法
  - [x] 2.1 实现SetLocalString方法
    - 接受ReadOnlySpan<byte>参数，避免异常抛出
    - 实现UTF-8字节到NaNBoxing位模式的转换
    - 处理6字节存储限制和零填充
    - _需求: 2.1, 2.3, 2.4_

  - [ ]* 2.2 为SetLocalString方法编写属性测试
    - 在compilerTests项目中添加SetLocalString方法测试
    - **属性 1: LocalString往返一致性**
    - **验证: 需求 2.3, 2.4, 3.1, 3.2, 3.4, 5.1**

  - [x] 2.3 实现LocalStringValue属性
    - 从NaNBoxing位模式提取UTF-8字节序列
    - 处理零字节终止和字符串长度检测
    - 实现UTF-8字节到字符串的解码
    - _需求: 3.1, 3.2, 3.3, 3.4_

  - [ ]* 2.4 为LocalStringValue属性编写单元测试
    - 在compilerTests项目中添加LocalStringValue属性测试
    - 测试空字符串处理
    - 测试边界情况和特殊字符
    - _需求: 3.3_

- [x] 3. 更新ValueType属性以识别LocalString
  - 在ValueType属性的switch语句中添加LocalString分支
  - 确保正确返回BoxType.LocalString
  - _需求: 4.5_

- [ ]* 3.1 为ValueType属性编写属性测试
  - 在compilerTests项目中添加ValueType属性测试
  - **属性 5: 类型识别一致性**
  - **验证: 需求 4.4, 4.5**

- [x] 4. 实现智能字符串创建策略（仅用于运行时动态字符串）
  - [x] 4.1 实现TryCreateLocalString辅助方法
    - 提供安全的LocalString创建接口
    - 返回bool指示创建是否成功
    - _需求: 7.3_

- [x] 5. 检查点 - 确保核心功能正常工作
  - 确保所有测试通过，如有问题请询问用户。

- [x] 6. 集成LocalString到现有字符串操作中
  - [x] 6.1 更新Extensions.GetPrimitiveValueToString方法
    - 在switch语句中添加BoxType.LocalString分支
    - 确保LocalString能正确转换为字符串表示
    - _需求: 4.4_

  - [ ]* 6.2 为GetPrimitiveValueToString集成编写属性测试
    - 在compilerTests项目中添加GetPrimitiveValueToString集成测试
    - **属性 5: 类型识别一致性**
    - **验证: 需求 4.4**

  - [x] 6.3 更新字符串比较逻辑
    - 在NaNBoxing.FastTestComp方法中添加LocalString支持
    - 在Player类的字符串比较方法中添加LocalString支持
    - 确保LocalString与RtPayloadString的正确比较
    - 确保LocalString之间的正确比较
    - _需求: 4.1, 4.2_

  - [ ]* 6.4 为字符串比较编写属性测试
    - 在compilerTests项目中添加字符串比较测试
    - **属性 4: 字符串比较等价性**
    - **验证: 需求 4.1, 4.2**

- [x] 7. 实现字符串连接支持
  - [x] 7.1 更新字符串连接逻辑
    - 在Player类的字符串连接方法中添加LocalString支持
    - 处理LocalString与其他字符串类型的连接
    - 优化连接结果的存储策略选择
    - _需求: 4.3_

  - [ ]* 7.2 为字符串连接编写属性测试
    - 在compilerTests项目中添加字符串连接测试
    - **属性 6: 字符串连接兼容性**
    - **验证: 需求 4.3**

- [x] 8. 更新快速运算方法以支持LocalString
  - [x] 8.1 更新FastTestComp方法
    - 在快速比较逻辑中添加LocalString支持
    - 确保LocalString的高效比较
    - _需求: 4.1, 4.2_

  - [x] 8.2 更新FastAdd和FastMinus方法
    - 在快速运算中添加LocalString到字符串的转换支持
    - 处理LocalString与其他类型的运算
    - _需求: 4.3_

- [x] 9. 实现UTF-8处理和错误处理
  - [x] 9.1 添加UTF-8长度计算优化
    - 确保多字节UTF-8字符的正确处理
    - 优化字节长度计算性能
    - _需求: 5.2_

  - [ ]* 9.2 为UTF-8处理编写属性测试
    - 在compilerTests项目中添加UTF-8处理测试
    - **属性 7: UTF-8长度计算正确性**
    - **验证: 需求 5.2**

  - [x] 9.3 实现错误处理机制
    - 处理无效UTF-8序列的情况
    - 实现适当的错误信息和异常处理
    - _需求: 7.1, 7.2, 7.4_

  - [ ]* 9.4 为错误处理编写属性测试
    - 在compilerTests项目中添加错误处理测试
    - **属性 8: 错误处理正确性**
    - **验证: 需求 7.1, 7.2, 7.4**

- [x] 10. 更新ActionScript类型操作以支持LocalString
  - [x] 10.1 更新Is()操作支持LocalString
    - 在Player.cs的Is()方法中添加LocalString支持
    - 确保LocalString被正确识别为String类型
    - _需求: 4.4, 4.5_

  - [x] 10.2 验证As()操作自动支持LocalString
    - cast_as指令内部使用Is()方法，因此自动支持LocalString
    - 确保LocalString可以正确进行类型转换
    - _需求: 4.4, 4.5_

  - [x] 10.3 优化ConvertValueType方法的String转换
    - 在ConvertValueType方法中添加LocalString支持
    - 对于数值类型转换为String时，优先尝试创建LocalString
    - 只有当LocalString无法容纳时才回退到堆分配
    - _需求: 6.1, 6.2_

  - [x] 10.4 优化Exec_Add方法的字符串连接
    - 优化Exec_Add方法中所有AllocString调用
    - 使用TryCreateStringValue辅助函数优先创建LocalString
    - 添加对LocalString + HeapPtr(String)连接的支持
    - _需求: 4.3, 6.1, 6.2_

- [x] 11. 最终集成和测试
  - [x] 11.1 运行现有测试套件验证兼容性
    - 确保所有现有字符串相关测试仍然通过
    - 验证向后兼容性
    - _需求: 8.2, 8.3_

  - [ ]* 11.2 编写综合集成测试
    - 在compilerTests项目中添加综合集成测试
    - 测试LocalString与现有系统的完整集成
    - 验证性能优化效果
    - _需求: 6.1, 6.2, 6.3_

  - [ ]* 11.3 为序列化兼容性编写属性测试
    - 在compilerTests项目中添加序列化兼容性测试
    - **属性 9: 序列化兼容性**
    - **验证: 需求 8.4**

- [x] 12. 处理编译时默认值的特殊情况
  - [x] 12.1 确保编译时默认值处理的兼容性
    - 当编译时求变量默认值或参数默认值时，如果获得LocalString，应转换为堆分配字符串
    - 确保字符串池的一致性
    - 维持编译器和运行时的分离
    - _需求: 8.1, 8.2_

  - [ ]* 12.2 为编译时默认值处理编写单元测试
    - 在compilerTests项目中添加编译时默认值处理测试
    - 测试LocalString到堆分配字符串的转换
    - 验证字符串池的正确性
    - _需求: 8.1, 8.2_

- [x] 13. 最终检查点 - 确保所有测试通过
  - 确保所有测试通过，如有问题请询问用户。

## 注意事项

- 标记为`*`的任务是可选的测试任务，可以跳过以加快MVP开发
- LocalString主要用于运行时动态生成的小字符串优化
- 编译时字符串字面量继续使用现有的堆分配机制
- 编译时默认值处理需要特殊考虑，确保LocalString转换为堆分配字符串
- 每个任务都引用了具体的需求，确保可追溯性
- 检查点任务确保增量验证
- 属性测试验证通用正确性属性
- 单元测试验证特定示例和边界情况
- 所有代码修改都应遵循JuiceScript的C#编码规范