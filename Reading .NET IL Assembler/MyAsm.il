// Program header
.assembly extern mscorlib { auto }
.assembly OddOrEven { }
.module OddOrEven.exe
// Class declaration
.namespace Odd.or {
    .class public auto ansi Even extends [mscorlib]System.Object {
        // Field declaration
        .field public static int32 val
        // Method declaration
        .method public static void check() cil managed {
            .entrypoint
            .locals init (int32 Retval)
        AskForNumber:
            ldstr "Enter a number"
            call void [mscorlib]System.Console::WriteLine(string)
            call string [mscorlib]System.Console::ReadLine()
            ldstr "%d"
            ldsflda int32 Odd.or.Even::val
            call vararg int32 sscanf(string, string, ..., int32*)
            stloc.0
            ldloc.0
            brfalse.s Error
            ldsfld int32 Odd.or.Even::val
            ldc.i4.1
            and
            brfalse.s ItsEven
            ldstr "odd!"
            br.s PrintAndReturn
        ItsEven:
            ldstr "even!"
            br.s PrintAndReturn
        Error:
            ldstr "How rude!"
        PrintAndReturn:
            call void [mscorlib]System.Console::WriteLine(string)
            ldloc.0
            brtrue.s AskForNumber
            ret
        }
    }
}
// Calling unmanaged code
.method public static pinvokeimpl("msvcrt.dll" cdecl)
    vararg int32 sscanf(string, string) cil managed preservesig { }

