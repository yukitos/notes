# Memo about [FSharp.Compiler.Service](https://github.com/fsharp/FSharp.Compiler.Service)

# F# File Structure

* ParsedInput
  * ImplFile of ParsedImplFileInput
    * ParsedImplFileInput of (`filename`:string) * (`isScript`:bool) * (`qualifiedNameOfFile`:QualifiedNameOfFile) * (`scopedPragmas`:ScopedPragma list) * (`parsedHashDirectives`:ParsedHashDirective list) * (`symbolsOfModuleOrNamespace`:SynModuleOrNamespace list) * (`isLastCompiland`:bool)
  * SigFile of ParsedSigFileInput
    *ParsedSigFileInput of (`filename`:string) * (`qualifiedNameOfFile`:QualifiedNameOfFile) * (`scopedPragmas`:ScopedPragma list) * (`parsedHashDirectives`:ParsedHashDirective list) * (`symbolsOfModuleOrNamespaceInSignature`:SynModuleOrNamespaceSig list)
