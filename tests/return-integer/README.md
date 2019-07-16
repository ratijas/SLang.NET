It only works under assumption that SLang `Integer` unit is represented as `Int32` or `UInt32` underlying native type. Using anything else, e.g. `Int64`, will fail with the following error under .NET:
```
Unhandled Exception: System.MethodAccessException: Entry point must have a return type of void, integer, or unsigned integer.
```
; and will be silently ignored as a return value under mono.