let website = "/TryFSharp.Formatting"
let info =
  [ "project-name", "Tutorial: Functional Reactive Programming in F# and WPF"
    "project-author", "Stephen Elliott"
    "project-summary", "Trying to use FSharp.Formatting"
    "project-github", "https://github.com/yukitos/notes/Tutorial_Functional_Reactive_Programming_in_FSharp_and_WPF"]

#I "packages/FSharp.Formatting.2.4.1/lib/net40"
#I "packages/RazorEngine.3.3.0/lib/net40"
#I "packages/FSharp.Compiler.Service.0.0.36/lib/net40"

#r "RazorEngine.dll"
#r "FSharp.Compiler.Service.dll"
#r "FSharp.Literate.dll"
#r "FSharp.CodeFormat.dll"
#r "FSharp.MetadataFormat.dll"

open System.IO
open FSharp.Literate
open FSharp.MetadataFormat

let (@@) path1 path2 =
    Path.Combine(path1, path2)

#if RELEASE
let root = website
#else
let root = "file://" + (__SOURCE_DIRECTORY__ @@ "output")
#endif

let content     = __SOURCE_DIRECTORY__ @@ "docs"
let output      = __SOURCE_DIRECTORY__ @@ "output"
let templates   = __SOURCE_DIRECTORY__ @@ "templates"
let formatting  = __SOURCE_DIRECTORY__ @@ "packages/FSharp.Formatting.2.4.1/"
let docTemplate = formatting @@ "templates/docpage.cshtml"

let layoutRoots =
  [ templates
    formatting @@ "templates"
    formatting @@ "templates/reference" ]

let buildDocumentation () =
    let subdirs = Directory.EnumerateDirectories(content, "*", SearchOption.AllDirectories)
    for dir in Seq.append [content] subdirs do
        let sub = if dir.Length > content.Length then dir.Substring(content.Length + 1) else "."
        Literate.ProcessDirectory
            ( dir, docTemplate, output @@ sub, replacements = ("root", root)::info,
              layoutRoots = layoutRoots )

buildDocumentation()
