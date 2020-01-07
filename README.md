Allows you to compare what changes have been made between two values or objects.

# Install the package
`install-package FluffySpoon.ChangeDetection`

# Usage
Each `Change` returned looks like this:

```csharp
public struct Change
{
    /// <summary>
    /// The path of the property that changed.
    /// <example><see cref="string.Empty"/> for simple values.</example>
    /// <example>"MyComplexObject.MyComplexSubObject.MySimpleValue" when comparing two MyComplexObject instances.</example>
    /// </summary>
    public string PropertyPath
    {
        get;
    }

    /// <summary>
    /// The old value.
    /// </summary>
    public object OldValue
    {
        get; 
    }

    /// <summary>
    /// The new value.
    /// </summary>
    public object NewValue
    {
        get;
    }
}
```

## Comparing simple values
```csharp
ChangeDetector.GetChange("foo", "bar");

/*
{
    PropertyPath: "",
    OldValue: "foo",
    NewValue: "bar"
}
*/
```

## Comparing complex objects recursively
Consider the following complex class for the examples below:

```csharp
class ComplexObject
{
    public string StringValue
    {
        get; set;
    }

    public int MyIntValue
    {
        get; set;
    }

    public ComplexObject SubObject
    {
        get; set;
    }
}
```

### Example: Comparing object with sub-object
```csharp
var a = new ComplexObject() {
    StringValue = "foo",
    MyIntValue = 28,
    SubObject = new ComplexObject() {
        StringValue = "bar",
        MyIntValue = 123
    }
};

var b = new ComplexObject() {
    StringValue = "foo",
    MyIntValue = 1337,
    SubObject = new ComplexObject() {
        StringValue = "baz",
        MyIntValue = 123
    }
};

ChangeDetector.GetChanges(a, b);

/*
[
    {
        PropertyPath: "MyIntValue",
        OldValue: 28,
        NewValue: 1337
    },
    {
        PropertyPath: "SubObject.StringValue",
        OldValue: "bar",
        NewValue: "baz"
    }
]
*/
```

### Example: Comparing specific complex object property (SubObject)
```csharp
var a = new ComplexObject() {
    StringValue = "foo",
    MyIntValue = 28,
    SubObject = new ComplexObject() {
        StringValue = "bar",
        MyIntValue = 123
    }
};

var b = new ComplexObject() {
    StringValue = "foo",
    MyIntValue = 1337,
    SubObject = new ComplexObject() {
        StringValue = "baz",
        MyIntValue = 123
    }
};

ChangeDetector.GetChanges(a, b, x => x.SubObject);

/*
[
    {
        PropertyPath: "StringValue",
        OldValue: "bar",
        NewValue: "baz"
    }
]
*/
```

### Example: Comparing specific simple value on object (MyIntValue)
```csharp
var a = new ComplexObject() {
    StringValue = "foo",
    MyIntValue = 28,
    SubObject = new ComplexObject() {
        StringValue = "bar",
        MyIntValue = 123
    }
};

var b = new ComplexObject() {
    StringValue = "foo",
    MyIntValue = 1337,
    SubObject = new ComplexObject() {
        StringValue = "baz",
        MyIntValue = 123
    }
};

ChangeDetector.GetChanges(a, b, x => x.MyIntValue);

/*
[
    {
        PropertyPath: "MyIntValue",
        OldValue: 28,
        NewValue: 1337
    }
]
*/
```

## Checking if a change is present
Instead of `GetChange` and `GetChanges`, you can use `HasChanges` to get a boolean indicating if any change has been detected or not.

## Circular references won't break the comparison
If circular references are detected, the given property that causes the circular dependency is not evaluated.