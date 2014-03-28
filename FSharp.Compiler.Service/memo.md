# Memo about [FSharp.Compiler.Service](https://github.com/fsharp/FSharp.Compiler.Service)

# F# File Structure

* `ParsedInput`
  * `ImplFile`
    * `ParsedImplFileInput`
      * `string` : filename
      * `bool` : isScript
      * `QualifiedNameOfFile` : qualifiedNameOfFile
        * `Ident`
          * `idText`
          * `idRange`
      * `ScopedPragma list` : scopedPragmas
        * `WarningOff`
          * `range` : pragmaRange
          * `int` : warningNumFromPragma
      * `ParsedHashDirective list` : parsedHashDirectives
        * `string` : name
        * `string list` : parameters
        * `range` : declarationRange
      * `SynModuleOrNamespace list` : symbolsOfModuleOrNamespace
      * `bool` : isLastCompiland
  * `SigFile`
    * `ParsedSigFileInput`
      * `string` : filename
      * `QualifiedNameOfFile` : qualifiedNameOfFile
      * `ScopedPragma list` : scopedPragmas
      * `ParsedHashDirective list` : parsedHashDirectives
      * `SynModuleOrNamespaceSig list` : symbolsOfModuleOrNamespaceInSignagure
