.assembly extern mscorlib { auto }
.assembly MyAsm { }
.module MyAsm.exe
.class public auto ansi Program extends [mscorlib]System.Object {
    .method public static void Main() cil managed {
        .entrypoint
        ldstr "Hello world!"
        call void [mscorlib]System.Console::WriteLine (string)
        ret
    }
}
