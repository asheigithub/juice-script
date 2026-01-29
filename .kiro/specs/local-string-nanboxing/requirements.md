# Requirements Document

## Introduction

This specification defines the LocalString feature for JuiceScript's NaNBoxing system. LocalString enables direct storage of short UTF-8 strings within the 64-bit NaNBoxing structure, eliminating heap allocation overhead for strings up to 6 bytes in length. This optimization reduces memory pressure and improves performance for common short string operations in ActionScript execution.

## Glossary

- **NaNBoxing**: The 64-bit value storage system using NaN bit patterns for type tagging
- **LocalString**: A string type stored directly within NaNBoxing's 48-bit payload area
- **HeapString**: The existing heap-allocated string type (RtPayloadString)
- **TAG_LOCAL_STRING**: New type identifier for LocalString in NaNBoxing
- **UTF-8_Payload**: The 6-byte area within NaNBoxing for storing UTF-8 string data
- **String_Interop**: Seamless conversion between LocalString and HeapString types
- **GC_System**: The garbage collection system managing heap-allocated objects

## Requirements

### Requirement 1: LocalString Storage

**User Story:** As a runtime system, I want to store short strings directly in NaNBoxing values, so that I can avoid heap allocation overhead for common string operations.

#### Acceptance Criteria

1. WHEN a string is 6 UTF-8 bytes or shorter, THE NaNBoxing_System SHALL store it as a LocalString
2. WHEN storing a LocalString, THE NaNBoxing_System SHALL use the TAG_LOCAL_STRING type identifier
3. WHEN accessing LocalString data, THE NaNBoxing_System SHALL extract the UTF-8 bytes from the 48-bit payload area
4. THE NaNBoxing_System SHALL preserve null termination for LocalString values
5. WHEN a LocalString is created, THE NaNBoxing_System SHALL validate UTF-8 encoding correctness

### Requirement 2: Type System Integration

**User Story:** As a type system, I want LocalString to integrate seamlessly with existing string operations, so that ActionScript code works transparently with both LocalString and HeapString.

#### Acceptance Criteria

1. WHEN comparing strings, THE String_Comparison_System SHALL treat LocalString and HeapString as equivalent when content matches
2. WHEN concatenating strings, THE String_Operations_System SHALL handle LocalString to HeapString promotion automatically
3. WHEN accessing string properties, THE Runtime_System SHALL provide identical behavior for LocalString and HeapString
4. THE BoxType_Enum SHALL include LocalString as a distinct type for debugging and introspection
5. WHEN converting to string representation, THE ToString_System SHALL produce identical output for equivalent LocalString and HeapString values

### Requirement 3: Automatic Length-Based Routing

**User Story:** As a string allocation system, I want to automatically choose between LocalString and HeapString based on length, so that optimal storage is used without developer intervention.

#### Acceptance Criteria

1. WHEN allocating a string 6 UTF-8 bytes or shorter, THE Allocation_System SHALL create a LocalString
2. WHEN allocating a string longer than 6 UTF-8 bytes, THE Allocation_System SHALL create a HeapString
3. WHEN a LocalString operation would exceed 6 bytes, THE System SHALL promote to HeapString automatically
4. THE Allocation_System SHALL maintain consistent behavior across all string creation paths
5. WHEN string length changes during operations, THE System SHALL handle LocalString/HeapString transitions transparently

### Requirement 4: UTF-8 Encoding Compliance

**User Story:** As a string processing system, I want LocalString to handle UTF-8 encoding correctly, so that multi-byte characters are stored and retrieved accurately.

#### Acceptance Criteria

1. WHEN storing multi-byte UTF-8 characters, THE LocalString_System SHALL validate complete character boundaries
2. WHEN a UTF-8 character would exceed the 6-byte limit, THE System SHALL use HeapString instead
3. WHEN extracting LocalString content, THE System SHALL preserve UTF-8 encoding integrity
4. THE LocalString_System SHALL handle ASCII characters (1 byte each) efficiently
5. WHEN processing UTF-8 sequences, THE System SHALL reject invalid or incomplete encodings

### Requirement 5: Performance Optimization

**User Story:** As a performance-critical runtime, I want LocalString operations to be faster than HeapString operations, so that short string handling provides measurable performance benefits.

#### Acceptance Criteria

1. WHEN creating LocalString values, THE System SHALL avoid heap allocation entirely
2. WHEN comparing LocalString values, THE System SHALL use direct bit comparison when possible
3. WHEN accessing LocalString content, THE System SHALL avoid memory indirection
4. THE LocalString_Operations SHALL integrate with existing NaNBoxing fast-path optimizations
5. WHEN garbage collection occurs, THE System SHALL skip LocalString values (no heap references)

### Requirement 6: Backward Compatibility

**User Story:** As an existing ActionScript codebase, I want LocalString to work transparently with existing string APIs, so that no code changes are required.

#### Acceptance Criteria

1. WHEN existing code calls string methods, THE System SHALL work identically with LocalString and HeapString
2. WHEN serializing strings, THE System SHALL produce equivalent output regardless of storage type
3. WHEN debugging strings, THE System SHALL display LocalString and HeapString values consistently
4. THE Existing_String_APIs SHALL continue to function without modification
5. WHEN migrating between LocalString and HeapString, THE System SHALL preserve all string semantics

### Requirement 7: Memory Safety

**User Story:** As a memory-safe runtime, I want LocalString operations to prevent buffer overflows and corruption, so that system stability is maintained.

#### Acceptance Criteria

1. WHEN writing to LocalString storage, THE System SHALL enforce the 6-byte boundary strictly
2. WHEN reading LocalString data, THE System SHALL prevent out-of-bounds access
3. WHEN handling malformed UTF-8, THE System SHALL reject invalid sequences safely
4. THE LocalString_System SHALL validate all input before storage
5. WHEN LocalString operations fail, THE System SHALL provide clear error indication without corruption

### Requirement 8: Debugging and Diagnostics

**User Story:** As a developer debugging ActionScript code, I want to distinguish between LocalString and HeapString in diagnostic output, so that I can understand memory usage patterns.

#### Acceptance Criteria

1. WHEN displaying NaNBoxing values, THE Debug_System SHALL indicate LocalString type clearly
2. WHEN showing string content, THE Debug_System SHALL display LocalString and HeapString equivalently
3. WHEN profiling memory usage, THE System SHALL report LocalString as zero heap allocation
4. THE ToString_Method SHALL include type information for LocalString values in debug builds
5. WHEN logging string operations, THE System SHALL distinguish LocalString and HeapString in traces