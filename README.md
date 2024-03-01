# .NET Assembly Definition Generator

This application generates reference .NET assemblies by supplying it with an original .NET assembly. It will then
publicize it and strip the method bodies and resources from the file.

This project was made with the sole intent for generating reference assemblies for
the [LCVR](https://github.com/DaXcess/LCVR) mod, however since this publicizer is more resilient, it might also be used
for other purposes.

## Usage

> This tool does not output new DLL files. It replaces the original input file!

```sh
# Generate a reference assembly from a single file
$ AsmDefGenerator ./assembly.dll
```

```sh
# Generate reference assemblies from a directory containing DLL files
$ AsmDefGenerator ./folder/
```

## Difference between other publicizers

First and foremost, the output of this tool will generate reference assemblies, which do not contain code or resources.
This can significantly reduce the file size of assemblies.

This tool uses `dnlib` behind the scenes, instead of `Mono.Cecil`. This is generally considered to be faster, and I have
also noticed that other publicizers using `Mono.Cecil` sometimes require dependencies of the publicized assemblies to be
present, while this tool can work on standalone DLLs, without their dependencies having to be present.

This tool, like other publicizers correctly adjusts the backing fields of events to stay private, so as to not cause
ambiguity errors.

Last but not least, is that this tool attempts to see if a method on a type is an override (i.e. for an interface), and
if so it tries to detect whether or not another property with the same name is already present. If this is the case, it
will only publicize the method that is unique to the Type, and keeps the visibility of the overridden method as is. This
fixes some issues where JetBrains Rider starts showing ambiguity errors, even though the .NET compiler can still compile
the assembly no problem.