# 设计文档

## 概述

LocalString是JuiceScript NaNBoxing系统的一个优化功能，旨在将短UTF-8字符串直接存储在NaNBoxing的6字节可用空间内，从而避免堆内存分配，提升字符串操作性能。该设计充分利用了NaNBoxing现有的类型标签系统，并与现有的字符串处理机制无缝集成。

## 架构

### NaNBoxing位布局分析

当前NaNBoxing使用64位存储结构：
```
位布局: [63-52: NaN标识] [51-40: 类型标签] [39-0: 数据区域]
- 位63-52: 固定为0xFFF8 (NaN标识)
- 位51-40: 4位类型标签，用于区分不同的装箱类型
- 位39-0: 40位数据区域，等效于5字节
- 实际可用: 低32位(4字节) + 部分高位，总计约6字节可用空间
```

### LocalString存储策略

LocalString将采用以下存储策略：
1. **固定长度**: 使用全部6字节空间存储UTF-8字节序列
2. **隐式终止**: 字符串在6字节边界处隐式结束
3. **零填充**: 不足6字节的字符串用零字节填充剩余空间

存储布局：
```
[63-52: 0xFFF8] [51-40: TAG_LOCAL_STRING] [39-0: UTF-8数据(6字节，零填充)]
```

## 组件和接口

### 1. 类型系统扩展

#### 新增常量定义
```csharp
// 在NaNBoxing类中添加
public const ulong TAG_LOCAL_STRING = 0xFFF80D0000000000;
```

#### BoxType枚举扩展
```csharp
public enum BoxType : uint
{
    // ... 现有类型 ...
    LocalString = (uint)(TAG_LOCAL_STRING >> 40) & 0xF,
    // ... 其他类型 ...
}
```

### 2. LocalString操作接口

#### 设置LocalString值
```csharp
public void SetLocalString(ReadOnlySpan<byte> utf8Bytes)
{
    // 调用者已经确保utf8Bytes.Length <= 6
    Debug.Assert(utf8Bytes.Length <= 6, "UTF-8 bytes length should not exceed 6");
    
    ulong data = TAG_LOCAL_STRING;
    
    // 存储UTF-8字节，从高位开始，剩余位置自动为0
    for (int i = 0; i < utf8Bytes.Length; i++)
    {
        data |= ((ulong)utf8Bytes[i]) << (32 - i * 8);
    }
    
    store = data;
}
```

#### 获取LocalString值
```csharp
public string LocalStringValue
{
    get
    {
        // 提取所有6字节，然后找到实际字符串结束位置
        Span<byte> utf8Bytes = stackalloc byte[6];
        for (int i = 0; i < 6; i++)
        {
            utf8Bytes[i] = (byte)((store >> (32 - i * 8)) & 0xFF);
        }
        
        // 找到第一个零字节的位置，或使用全部6字节
        int actualLength = 6;
        for (int i = 0; i < 6; i++)
        {
            if (utf8Bytes[i] == 0)
            {
                actualLength = i;
                break;
            }
        }
        
        if (actualLength == 0) return string.Empty;
        
        // 只使用实际长度的字节进行解码
        return Encoding.UTF8.GetString(utf8Bytes.Slice(0, actualLength));
    }
}
```

### 3. 字符串创建策略

#### 智能字符串分配
```csharp
public static NaNBoxing CreateString(Player player, string value)
{
    // 先尝试获取UTF-8字节
    int utf8ByteCount = Encoding.UTF8.GetByteCount(value);
    
    if (utf8ByteCount <= 6)
    {
        // 使用LocalString
        Span<byte> utf8Bytes = stackalloc byte[utf8ByteCount];
        Encoding.UTF8.GetBytes(value, utf8Bytes);
        
        NaNBoxing result = new NaNBoxing();
        result.SetLocalString(utf8Bytes);
        return result;
    }
    else
    {
        // 回退到堆分配
        int heapPtr = player.Context.GC.AllocString(value);
        if (heapPtr == 0)
        {
            throw new OutOfMemoryException("Failed to allocate string");
        }
        
        NaNBoxing result = new NaNBoxing();
        result.SetHeapPtr(heapPtr);
        return result;
    }
}
```

## 数据模型

### LocalString数据结构

```csharp
// LocalString在NaNBoxing中的表示
struct LocalStringLayout
{
    // 位63-52: 0xFFF8 (NaN标识)
    // 位51-40: 0xD (TAG_LOCAL_STRING标识)
    // 位39-32: UTF-8字节0
    // 位31-24: UTF-8字节1
    // 位23-16: UTF-8字节2
    // 位15-8:  UTF-8字节3
    // 位7-0:   UTF-8字节4
    // 位47-40: UTF-8字节5 (使用类型标签区域的低位)
    // 零填充: 不足6字节的字符串用零字节填充，字符串在第一个零字节处结束
}
```

### UTF-8编码处理

LocalString支持的字符类型：
- **ASCII字符**: 1字节，可存储最多5个字符
- **双字节UTF-8**: 2字节，可存储最多2个字符 + 1个ASCII
- **三字节UTF-8**: 3字节，可存储1个字符 + 最多2个ASCII
- **四字节UTF-8**: 4字节，可存储1个字符 + 最多1个ASCII
- **混合编码**: 根据总字节数限制组合存储

## 正确性属性

*属性是一个特征或行为，应该在系统的所有有效执行中保持为真——本质上是关于系统应该做什么的正式陈述。属性作为人类可读规范和机器可验证正确性保证之间的桥梁。*

### 属性 1: LocalString往返一致性
*对于任何*UTF-8字符串，如果其字节长度不超过6字节，那么通过SetLocalString存储然后通过LocalStringValue读取应该产生与原始字符串相等的字符串值
**验证: 需求 2.3, 2.4, 3.1, 3.2, 3.4, 5.1**

### 属性 2: 字符串创建策略正确性
*对于任何*UTF-8字符串，CreateString方法应该在字符串字节长度不超过6字节时创建LocalString，超过6字节时回退到堆分配的RtPayloadString
**验证: 需求 2.1, 2.2, 5.3**

### 属性 3: 类型标签唯一性
*对于所有*定义的BoxType枚举值，每个类型标签应该具有唯一的数值，特别是TAG_LOCAL_STRING不应与现有类型标签冲突
**验证: 需求 1.3**

### 属性 4: 字符串比较等价性
*对于任何*两个表示相同字符串内容的值（无论是LocalString、RtPayloadString或其组合），字符串比较操作应该返回相等
**验证: 需求 4.1, 4.2**

### 属性 5: 类型识别一致性
*对于任何*LocalString值，其ValueType属性应该返回BoxType.LocalString，并且在GetPrimitiveValueToString等方法中应该被正确识别为字符串类型
**验证: 需求 4.4, 4.5**

### 属性 6: 字符串连接兼容性
*对于任何*LocalString和任何其他字符串类型的连接操作，结果应该等于将两个字符串的内容连接后的字符串
**验证: 需求 4.3**

### 属性 7: UTF-8长度计算正确性
*对于任何*包含多字节UTF-8字符的字符串，系统应该正确计算其UTF-8字节长度，并基于此长度（不超过6字节）决定存储策略
**验证: 需求 5.2**

### 属性 8: 错误处理正确性
*对于任何*无效的UTF-8序列或类型转换错误，系统应该抛出适当的异常并提供清晰的错误信息
**验证: 需求 7.1, 7.2, 7.4**

### 属性 9: 序列化兼容性
*对于任何*LocalString值，序列化和反序列化操作应该保持字符串内容的一致性
**验证: 需求 8.4**

## 错误处理

### 错误类型和处理策略

1. **UTF-8编码错误**
   - 检测无效的UTF-8字节序列
   - 抛出ArgumentException并提供详细错误信息
   - 在调试模式下提供额外的诊断信息

2. **长度超限错误**
   - 当字符串UTF-8编码超过5字节时，自动回退到堆分配
   - 不抛出异常，保持向后兼容性
   - 记录性能统计信息（可选）

3. **内存不足错误**
   - 当堆分配失败时，抛出OutOfMemoryException
   - 保持与现有错误处理机制的一致性
   - 提供清晰的错误上下文

4. **类型转换错误**
   - 当尝试将非字符串类型转换为LocalString时抛出InvalidOperationException
   - 提供类型安全的转换方法
   - 在调试模式下进行额外的类型检查

### 错误恢复机制

```csharp
public static bool TryCreateLocalString(ReadOnlySpan<byte> utf8Bytes, out NaNBoxing result)
{
    result = default;
    if (utf8Bytes.Length <= 6)
    {
        result.SetLocalString(utf8Bytes);
        return true;
    }
    return false;
}
```

## 测试策略

### 双重测试方法

本设计采用单元测试和基于属性的测试相结合的综合测试策略：

#### 单元测试重点
- **具体示例**: 测试特定的字符串值和边界情况
- **错误条件**: 测试无效输入和异常情况
- **集成点**: 测试与现有字符串系统的集成
- **边界情况**: 测试5字节边界、空字符串、特殊字符

#### 基于属性的测试重点
- **通用属性**: 验证所有输入的通用正确性属性
- **随机化覆盖**: 通过随机生成的输入实现全面覆盖
- **往返一致性**: 验证存储和读取的一致性
- **类型安全性**: 验证类型系统的正确性

#### 基于属性的测试配置
- **测试库**: 使用FsCheck.NUnit进行基于属性的测试
- **迭代次数**: 每个属性测试最少运行100次迭代
- **测试标签**: 每个测试使用格式 **Feature: nanboxing-localstring, Property {number}: {property_text}**
- **生成器**: 自定义字符串生成器，包括各种UTF-8字符组合

#### 测试覆盖范围
- **ASCII字符串**: 1-5字节的纯ASCII字符串
- **多字节UTF-8**: 包含各种Unicode字符的字符串
- **边界情况**: 恰好5字节的字符串
- **空字符串**: 零长度字符串
- **混合编码**: ASCII和多字节字符的组合
- **无效输入**: 超长字符串、无效UTF-8序列

### 性能测试

除了正确性测试外，还需要进行性能基准测试：
- **分配性能**: 比较LocalString与堆分配的性能
- **访问性能**: 比较字符串读取的性能差异
- **内存使用**: 验证内存使用的优化效果
- **GC压力**: 测试垃圾回收压力的减少