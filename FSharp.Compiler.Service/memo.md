# Memo about [FSharp.Compiler.Service](https://github.com/fsharp/FSharp.Compiler.Service)

# F# File Structure (as of 2014/03)

* `ParsedInput`
  * `ImplFile`
    * `ParsedImplFileInput`
      * `string` : file name
      * `bool` : true if this is a script file
      * `QualifiedNameOfFile` : qualified name of file
        * `Ident`
          * `idText`
          * `idRange`
      * `ScopedPragma list` : pragmas that is available in the current scope
        * `WarningOff`
          * `range` : location of this pragma
          * `int` : number of warnings from this pragma (?)
      * `ParsedHashDirective list` : parsed hash directives, ex: `#directive XXX`
        * `string` : name of the hash directive
        * `string list` : parameters of the hash directive
        * `range` : location of this hash directive
      * `SynModuleOrNamespace list` : symbols that can be found in this module or namespace
        * `LongIdent` : id which might contains multiple Ident if nested, ex: `module Foo.Bar`
          * `Ident list`
        * `bool` : true if this is a module
        * `SynModuleDecls` : declarations in this module (or namespace)
        * `PreXmlDoc` : XmlDocument specified in front of this module or namespace declaration
        * `SynAttributes` : Attributes specified for this module (or namespace)
        * `SynAccess option` : access modifier for this module
        * `range` : whole location of this module or namespace
      * `bool` : true if this file is last compiland
  * `SigFile`
    * `ParsedSigFileInput`
      * `string` : filename
      * `QualifiedNameOfFile` : qualifiedNameOfFile
      * `ScopedPragma list` : scopedPragmas
      * `ParsedHashDirective list` : parsedHashDirectives
      * `SynModuleOrNamespaceSig list` : symbolsOfModuleOrNamespaceInSignagure
