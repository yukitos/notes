// #define USE_MAPPED_FIELD
// #define BLOW_UP
// Program header
.assembly extern mscorlib { auto }
.assembly OddOrEven { }
.module OddOrEven.exe
// Aliasing
.typedef [mscorlib]System.Console as TTY
.typedef method void TTY::WriteLine(string) as PrintLine
// Class declaration
.class public Odd.Or.Even {
    // Field declaration
    .field public static int32 val
    // Method declaration
    .method public static void check() {
        .entrypoint
        .locals init (int32 Retval)
    AskForNumber:
        ldstr "Enter a number"
        call PrintLine
        .try {
            call string TTY::ReadLine()
#ifdef BLOW_UP
            pop
            ldnull
#endif

#ifdef USE_MAPPED_FIELD
            ldsflda valuetype CharArray8 Format
            ldsflda int32 Odd.Or.Even::val
            call vararg int32 sscanf(string, int8*, ..., int32*)
#else
            ldstr "%d"
            ldsflda int32 Odd.Or.Even::val
            call vararg int32 sscanf(string, string, ..., int32*)
#endif
            stloc.0
            leave.s DidntBlowUp
        }
        catch [mscorlib]System.Exception
        { // Exception handler begins
            pop
            ldstr "KABOOM!"
            call PrintLine
            leave.s Return
        } // Exception handler ends
    DidntBlowUp:
        ldloc.0
        brfalse.s Error
        ldsfld int32 Odd.Or.Even::val
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
        call PrintLine
        ldloc.0
        brtrue.s AskForNumber
    Return:
        ret
    }
}
#ifdef USE_MAPPED_FIELD
// Global items
.field public static valuetype CharArray8 Format at FormatData
// Data declaration
.data FormatData = bytearray(25 64 00 00 00 00 00 00)
// Value type as placeholder
.class public explicit value CharArray8 { .size 8 }
// Calling unmanaged code
.method public static pinvokeimpl("msvcrt.dll" cdecl)
    vararg int32 sscanf(string, int8*) preservesig { }
#else
// Calling unmanaged code
.method public static pinvokeimpl("msvcrt.dll" cdecl)
    vararg int32 sscanf(string, string) preservesig { }
#endif
