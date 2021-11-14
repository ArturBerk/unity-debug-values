# unity-debug-values
Debug values window. Shows grouped named debug values

## Open window

**Window/Debug Values**

## Usage

``` C#
// Named
DebugValues.Show("value1", valueObject);
// Named formatted
DebugValues.Show("value2", 10.0f, "0.00");
// Named in group
DebugValues.ShowAt("group", "value3", valueObject);
```
