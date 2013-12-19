# 基礎から始める型プロバイダー

原文： [Type Providers From the Ground Up][link01]

----

ブログを投稿するのは考証したり、記憶にとどめておきたいというのが常なわけだけれども、
今ちょうどコードベースに最初の [型プロバイダ][link02] を追加したところだ。
そこで a) 忘れてしまわないうちに、そして b) コードを修正しなければならなくなった
誰かのために詳細を記録しておくことにする。

さて、重要なものから始めよう。
現実的な問題に手をつける前に、まずは型を作ってみることにする。
型を作るには...

1) Visual StudioでF#のライブラリプロジェクトを作成する(2012以降であれば動作するはず)

2a) [F# TypeProvider Starter Pack][link03] をインストールする。あるいは

2b) プロジェクトの最初の方に [ProvidedTypes.fs][link04] と
    [ProvidedTypes.fsi][link05] のファイルを追加する。

どちらの場合でもプロジェクトのファイル一覧において、
.fsiファイルが.fsファイルよりも前になるようにして、
かつ型プロバイダー用のコードよりも前の位置になるようにする。
おそらくは手動で順序を並び替える必要があるだろう。

これらは型プロバイダーのアセンブリ内でDLLを参照する場合に
セキュリティやAppDomainといったやっかいな問題がある都合上、
コンパイル済みのDLLではなくソースコードとして公開されている。
なので単にこれらをプロジェクトに追加すればよい。
なおこれらのファイルを自前で再作成しようなどとはきっと思わないはずだ。

3) Library1.fsの内容をたとえば以下のように書き換える：

```fsharp
module Mavnn.Blog.TypeProvider

open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices

[<TypeProvider>]
type MavnnProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces ()

[<assembly:TypeProviderAssembly>]
do ()
```

さて、いい感じになったのでビルドする。
型プロバイダークラスが1つと、型を公開しているアセンブリであるということを
理解しているアセンブリが1つ用意できた。
ただし残念ながらまだ1つも型を公開していない。
では作ってみよう。

ソリューション内のLibrary1.fsを以下のような感じに変更してみて、
何が起きるのか、そしてどうやってテストするのかを見ていこう。

```fsharp
module Mavnn.Blog.TypeProvider

open ProviderImplementation.ProvidedTypes
open Microsoft.FSharp.Core.CompilerServices

[<TypeProvider>]
type MavnnProvider (config : TypeProviderConfig) as this =
    inherit TypeProviderForNamespaces ()

    let ns = "Mavnn.Blog.TypeProvider.Provided"
    let asm = Assembly.GetExecutingAssembly()

    let createTypes () =
        let myType = ProvidedTypeDefinition(asm, ns, "MyType", Some typeof<obj>)
        let myProp = ProvidedProperty("MyProperty", typeof<string>, IsStatic = true,
                                        GetterCode = (fun args -> <@@ "Hello world" @@>))
        myType.AddMember(myProp)
        [myType]

    do
        this.AddNamespace(ns, createTypes())

[<assembly:TypeProviderAssembly>]
do ()
```

まず重要なこと。
このコードはstaticプロパティを1つ持ったクラスを公開するように見えるが、
どうやってそれをテストしたらよいのだろうか？

これは見た目よりも難しいことがわかる。
出来たばかりの型プロバイダーをVisual Studioで参照してしまうと、
Visual StudioのインスタンスがDLLファイルをロックしてしまう。
つまり再コンパイルできなくなる。
そのため、開発用に使用しているVisual Studioのインスタンスで
DLLを参照する方法はうまくいかない。

2つ目のVisual Studioを起動して(そのくらいは余裕があるRAMを乗せた
ハードウェアを使ってるだろう？)、そこでF#プロジェクトを作成する。
そして以下のようなfsxファイルを追加する：

```fsharp
// パスは環境ごとに違うはず...
#r @"../../Mavnn.Blog.TypeProvider/Mavnn.Blog.TypeProvider/bin/Debug/Mavnn.Blog.TypeProvider.dll"

open Mavnn.Blog.TypeProvider.Provided

// この次の行で `MyType.MyProperty` と入力してみる
```

入力を始めると...よし！
Intellisenseに新しく作った型とstaticプロパティが一覧表示される。
かつてないほど長ったらしい"Hello World"プログラムを含んだ
スクリプトをF# Interactiveで評価してみよう。

**この新しいVisual Studioのインスタンスは
型プロバイダーを再コンパイルする時に
毎回終了させる必要がある。**

----

## 何が起きているのか？

先のコードでは新しい名前空間を宣言して、現在のアセンブリを見つけ出している。
つまり現在のアセンブリに何かしらを足し込むことができる。
型プロバイダーの初期化処理では、(今のところ)やや名前が不適切な
`createTypes` メソッドで作成した型1つを含む名前空間を
( `this.AddNamespace(...)` で)アセンブリに追加している。

`createTypes` ではまず、作成中の名前空間直下のメンバーとなる型( `MyType` )を作成し、
次にstaticプロパティを作成してからこの型に追加している。
`AddNamespace` は型のリストを引数にとるため、
この型を1つだけ要素とするリストを作成して、それを返している。

`MyType` に対するCLR上の表現は `obj` として定義する。
これはつまり非F#言語からアクセスした場合には、コンパイラ的には `object` として見えることになる。

なかなかいい感じだ。
ただしstaticプロパティのちょっと異様な `<@@ ... @>` シンタックスを除けば。
見ての通り、このコードは `"Hello world"` を返すようなプロパティのgetメソッドを
生成するためのものだけれども、ではどうやって？

この文法は [コードクォート][link06] を表すもので、
プログラム内にコンパイルされるのではなく、
1つの式を表すオブジェクトとしてコンパイルされる。

頭が痛くなった？
私もだよ...
ここではコードクォートの詳細は省略するけれども
(私も詳しいことはわかってないからね！)、
基本を押さえておく必要はあるだろう。

簡単な例として、クォート式 `<@@ 1 + 2 @@>` はコンパイルされると
`Quotations.Expr = Call (None, op_Addition, [Value (1), Value (2)])` になる。
まだ難しくないけど、これはどうだろう：

```fsharp
let addI i =
    <@@ 1 + (%%i) @@>
```

これは `Expr -> Expr` という関数で、 `let add2 = addI <@@ 2 @@>` としたり
(結果は `val add2 : Expr = Call (None, op_Addition, [Value (1), Value (2)])` )、
`let add2MultipliedByX x = addI <@@ 2 * x @@>` としたりできる
(結果は `val add2MultipliedByX : x:int -> Expr` )。
つまり最初のF#の式で表されるASTに2番目の式を組み込んだ形で
評価されるようなものが手に入るということだ。
したがって先の `GettterCode` の場合、型が生成される際には `get_MyPropertyMethod`
としてコンパイルされることになるASTを実際には指定しているということになる。

今のところクォート式について知っておくべきことの2つめとして、
式を評価するものが、作成したF#の式を扱えたり
扱えなかったりするかもしれないということだ。
これについてはもう少し後回しにしよう！

今のところ、作成中の型プロバイダーはあまりおもしろいものではない。
型のインスタンスさえ生成できない。
そこで `createTypes` を書き換えてこの機能を実装してみよう：

```fsharp
    let createTypes () =
        let myType = ProvidedTypeDefinition(asm, ns, "MyType", Some typeof<obj>)
        let myProp = ProvidedProperty("MyProperty", typeof<string>, IsStatic = true,
                                        GetterCode = (fun args -> <@@ "Hello world" @@>))
        myType.AddMember(myProp)

        let ctor = ProvidedConstructor([], InvokeCode = fun args -> <@@ "My internal state" :> obj @@>)
        myType.AddMember(ctor)

        let ctor2 = ProvidedConstructor(
                        [ProvidedParameter("InnerState", typeof<string>)],
                        InvokeCode = fun args -> <@@ (%%(args.[0]):string) :> obj @@>)
        myType.AddMember(ctor2)

        let innerState = ProvidedProperty("InnerState", typeof<string>,
                            GetterCode = fun args -> <@@ (%%(args.[0]) :> obj) :?> string @@>)
        myType.AddMember(innerState)

        [myType]

    do
        this.AddNamespace(ns, createTypes())
```

これで型のインスタンスを(2通りの方法で)生成できるようになった。
根底にあるCLR型は `object` なので、型のインスタンスに対する内部表現としては
任意のものを格納できる。
コンストラクタの `InvokeCode` 引数では、オブジェクトが評価された際に、
それに対する内部表現を返すようなクォート式を返すようにする必要がある。
ここでは1つの文字列を返すようにしていて(ただしobjにキャストする必要がある)、
スプライス構文を使うことによって(引数をとるコンストラクタのために)
コンストラクタの引数をクォート式内でも使えるようにしている。

同様にしてプロパティを1つ追加する(なお今回はstaticプロパティではない点に注意)。
このプロパティはstaticではないので、 `args` 配列の1番目の要素は
型のインスタンス自身である(拡張メソッドを定義する場合と同じ)。
そのため、この値をスプライス構文でクォート式に組み込むことによって
(ただし `obj` から `string` にキャストすることを忘れないようにすること)、
外の世界からもオブジェクトの内部状態が確認できるようにしている。

そうすると以下のようなことができる：

```fsharp
// パスは環境ごとに違うはず...
#r @"../../Mavnn.Blog.TypeProvider/Mavnn.Blog.TypeProvider/bin/Debug/Mavnn.Blog.TypeProvider.dll"

open Mavnn.Blog.TypeProvider.Provided

let thing = MyType()
let thingInnerState = thing.InnerState

let thing2 = MyType("Some other text")
let thing2InnerState = thing2.InnerState

// val thing : Mavnn.Blog.TypeProvider.Provided.MyType = "My internal state"
// val thingInnerState : string = "My internal state"
// val thing2 : Mavnn.Blog.TypeProvider.Provided.MyType = "Some other text"
// val thing2InnerState : string = "Some other text"
```
----

# それで要点は？

なかなかいい感じだ。
ただしこれまでは型を静的に生成する方法しか扱っていなかったことに気づいたことと思う。
それだけなら普通の文法を使っても宣言できることだ。

そこでもう一歩進んでみよう。
たとえばグラフのノードの種類を表すJson定義があり、
それぞれには入出力用の「ポート」が複数定義されているものとする。
グラフの要素はJson配列になっていて、各Nodeの種類とポートには
Guid識別子と表示名がある。

入力となるJSONデータは以下のようなものだ：

```javascript

   {
      "Id":{
         "Name":"Simple",
         "UniqueId":"0ab82262-0ad3-47d3-a026-615b84352822"
      },
      "Ports":[
         {
            "Id":{
               "Name":"Input",
               "UniqueId":"4b69408e-82d2-4c36-ab78-0d2327268622"
            },
            "Type":"input"
         },
         {
            "Id":{
               "Name":"Output",
               "UniqueId":"92ae5a96-6900-4d77-832f-d272329f8a90"
            },
            "Type":"output"
         }
      ]
   },
   {
      "Id":{
         "Name":"Join",
         "UniqueId":"162c0981-4370-4db3-8e3f-149f13c001da"
      },
      "Ports":[
         {
            "Id":{
               "Name":"Input1",
               "UniqueId":"c0fea7ff-456e-4d4e-b5a4-9539ca134344"
            },
            "Type":"input"
         },
         {
            "Id":{
               "Name":"Input2",
               "UniqueId":"4e93c3b1-11bc-422a-91b8-e53204368714"
            },
            "Type":"input"
         },
         {
            "Id":{
               "Name":"Output",
               "UniqueId":"fb54728b-9602-4220-ba08-ad160d92d5a4"
            },
            "Type":"output"
         }
      ]
   },
   {
      "Id":{
         "Name":"Split",
         "UniqueId":"c3e44941-9182-41c3-921c-863a82097ba8"
      },
      "Ports":[
         {
            "Id":{
               "Name":"Input",
               "UniqueId":"0ec2537c-3346-4503-9f5a-d0bb49e9e431"
            },
            "Type":"input"
         },
         {
            "Id":{
               "Name":"Output1",
               "UniqueId":"77b5a50c-3d11-4a67-b14d-52d6246e78c5"
            },
            "Type":"output"
         },
         {
            "Id":{
               "Name":"Output2",
               "UniqueId":"d4d1e928-5347-4d51-be54-8650bdfe9bac"
            },
            "Type":"output"
         }
      ]
   }
]
```

ここに来て事態がなかなか複雑になってきたので
コード全体を確認したいと思うかもしれないが、
コードは [GitHub] 上にあるのでそれぞれ好みの開発環境で参照しつつ
読み進めてもらいたい。

データのパースについては他に任せることにしよう。
型プロバイダーのプロジェクトで、NuGetマネージャから `Newtonsoft.Json` への参照を
追加して、3度 `createTypes` を見ていくことにする。

まずJsonのデシリアライズ先となるクラスをいくつか用意する必要がある。
出来合えのNewtonsoftは(更新途中であるとはいえ)F#のコアクラスを駆使するようには
なっていないため、今のところは古典的OOスタイルの可変型をいくつか作ることにする：

```fsharp
type Id () =
    member val UniqueId = Guid() with get, set
    member val Name = "" with get, set

type Port () =
    member val Id = Id() with get, set
    member val Type = "" with get, set

type Node () =
    member val Id = Id() with get, set
    member val Ports = Collections.Generic.List<Port>() with get, set
```

(ただし心配は無用。これらがそのままメインインターフェイスとして公開されるわけではない。)

Jsonからこの新しいCLR型への変換は簡単に実装できる：

```fsharp
let nodes =
    JsonConvert.DeserializeObject<seq<Node>>(IO.File.ReadAllText(@"C:\Temp\Graph.json"))
```

さてここが面白いところだ。
これらのノードからグラフを組み立てるにはいくつかの手順が必要になる。

まずノード型の特定のインスタンスを組み立てる必要がある。
しかしこれはどの `Split` ノードだろうか？

そこで、インスタンスに対して、基底をなす具象型を用意することにする：

```fsharp
type nodeInstance =
    {
        Node : Node
        InstanceId : Id
        Config : string
    }

module private NodeInstance =
    let create node name guid config =
        { Node = node; InstanceId = Id(Name = name, UniqueId = guid); Config = config }
```

そしてJsonから読み取ったそれぞれのノード型を受け付けるコンストラクタを持った、
より具体的な型を構築する：

```fsharp
let nodeType = ProvidedTypeDefinition(asm, ns, node.Id.Name, Some typeof<nodeInstance>)
let ctor = ProvidedConstructor(
            [
                ProvidedParameter("Name", typeof<string>)
                ProvidedParameter("UniqueId", typeof<Guid>)
                ProvidedParameter("Config", typeof<string>)
            ],
            InvokeCode = fun [name;unique;config] -> <@@ NodeInstance.create (GetNode id) (%%name:string) (%%unique:Guid) (%%config:string) @@>)
```

そうすると(Jsonデータを再確認してもらいたいが)
`let simple = Simple("simpleInstance", Guid.NewGuid(), "MyConfig")`
というコードで `Simple` ノードのインスタンスを生成することができる。
また、このインスタンスには基底型の `InstanceId` `Config` `Node`
というプロパティが既に備わっている。

なかなか順調だ。
しかし入出力を表すうまい方法がないものだろうか？
出力を互いに接続したりといった下らない処理を禁止するような、
何らかのコネクションビルダー関数を用意したい。
そこで入力と出力をそれぞれ別の型として用意することになる。

```fsharp
// F# for Fun and Profitのサイトにある、単一ケースを持った
// 判別共用体でデータをモデル化する方法についての
// すばらしい記事も要チェック：
// http://fsharpforfunandprofit.com/posts/designing-with-types-single-case-dus/

type InputPort = | InputPort of Port
type OutputPort = | OutputPort of Port
```

そして最後にノード生成用の関数を更新して、
`Inputs` と `Outputs` という2つの補助型を各ノード型に追加する。
また、それぞれのポートを表すオブジェクトに対応するプロパティを作成する。
ノードを作成する最終的なコードは以下のようになる：

```fsharp
let addInputPort (inputs : ProvidedTypeDefinition) (port : Port) =
    let port = ProvidedProperty(
                    port.Id.Name, 
                    typeof<InputPort>, 
                    GetterCode = fun args -> 
                        let id = port.Id.UniqueId.ToString()
                        <@@ GetPort id @@>)
    inputs.AddMember(port)
 
let addOutputPort (outputs : ProvidedTypeDefinition) (port : Port) =
    let port = ProvidedProperty(
                    port.Id.Name, 
                    typeof<OutputPort>, 
                    GetterCode = fun args -> 
                        let id = port.Id.UniqueId.ToString()
                        <@@ GetPort id @@>)
    outputs.AddMember(port)
 
let addPorts inputs outputs (portList : seq<Port>) =
    portList
    |> Seq.iter (fun port -> 
                    match port.Type with
                    | "input" -> addInputPort inputs port
                    | "output" -> addOutputPort outputs port
                    | _ -> failwithf "ポート %s/%s に対応する型が不明" port.Id.Name (port.Id.UniqueId.ToString()))
 
let createNodeType id (node : Node) =
    let nodeType = ProvidedTypeDefinition(asm, ns, node.Id.Name, Some typeof<nodeInstance>)
    let ctor = ProvidedConstructor(
                [
                    ProvidedParameter("Name", typeof<string>)
                    ProvidedParameter("UniqueId", typeof<Guid>)
                    ProvidedParameter("Config", typeof<string>)
                ],
                InvokeCode = fun [name;unique;config] -> <@@ NodeInstance.create (GetNode id) (%%name:string) (%%unique:Guid) (%%config:string) @@>)
    nodeType.AddMember(ctor)
 
    let outputs = ProvidedTypeDefinition("Outputs", Some typeof<obj>)
    let outputCtor = ProvidedConstructor([], InvokeCode = fun args -> <@@ obj() @@>)
    outputs.AddMember(outputCtor)
    outputs.HideObjectMethods <- true
 
    let inputs = ProvidedTypeDefinition("Inputs", Some typeof<obj>)
    let inputCtor = ProvidedConstructor([], InvokeCode = fun args -> <@@ obj() @@>)
    inputs.AddMember(inputCtor)
    inputs.HideObjectMethods <- true
    addPorts inputs outputs node.Ports
 
    // Node型の下に入力と出力を表すネスト型を追加する
    nodeType.AddMembers([inputs;outputs])
 
    // そしてノードのインスタンスでそれぞれにアクセスできるようにインスタンスプロパティをいくつか追加する
    let outputPorts = ProvidedProperty("OutputPorts", outputs, [],
                        GetterCode = fun args -> <@@ obj() @@>)
    let inputPorts = ProvidedProperty("InputPorts", inputs, [],
                        GetterCode = fun args -> <@@ obj() @@>)
 
    nodeType.AddMembers([inputPorts;outputPorts])
 
    nodeType
```

残された謎はあと1つ。
`GetPort` と `GetNode` は何者だろうか？
そして単に `<@@ node @@>` というようにはせず、
なぜこれらの関数をクォート式で使っているのだろうか？

クォート式の評価は、使用される評価器(evaluator)の実装によって
限定されるという話を覚えているだろうか。
最初の手順として追加した型プロバイダー用のファイルにはクォート式を
IL命令に変換するための評価器が同梱されている。
しかしカスタム型のリテラルについてはサポートされていない。
実際、 [ProvidedTypes.fsの該当箇所][link08]を確認すると
実に模範的な処理しか行われていないことがわかる。

したがって受付可能な型のうちの1つ(今回の場合は `string` )から
適切なポートやノードを見つけ出す方法を知るための
privateヘルパーメソッドをいくつか用意することになる：

```fsharp
let private nodes = JsonConvert.DeserializeObject<seq<Node>>(IO.File.ReadAllText(@"c:\Temp\Graph.json"))
                    |> Seq.map (fun n -> n.Id.UniqueId.ToString(), n)
                    |> Map.ofSeq
 
let GetNode id =
    nodes.[id]
 
let private ports =
    nodes
    |> Map.toSeq
    |> Seq.map (fun (_, node) -> node.Ports)
    |> Seq.concat
    |> Seq.map (fun p -> p.Id.UniqueId.ToString(), p)
    |> Map.ofSeq
 
let GetPort id =
    ports.[id]
```

さてこれですべてそろった。
Json形式で提供されたメタデータを使ってCLR型を生成するような
型プロバイダーが完成して動作するようになった。
ただし製品用のコードにするためには
(遅延読み込み、同名の複数ポートに対する処理、ファイル名をハードコードしない等、)
まだ数多くの処理を追加する必要があるだろう。

![Intellisenseによる補完][img01]

> 訳注：図は [原文のサイト][link01] に掲載されたものを転用しています。

疑問点や修正点があれば是非教えてほしい。
既に述べたとおり、私が型プロバイダーを使ったのは今回が本当に初めてのことだ。
しかしこのレベルであっても十分価値のある機能が利用できている。

[link01]: http://blog.mavnn.co.uk/type-providers-from-the-ground-up/ "Type Providers From the Ground Up"
[link02]: http://blogs.msdn.com/b/dsyme/archive/2013/01/30/twelve-type-providers-in-pictures.aspx "Twelve F# type providers in action"
[link03]: https://www.nuget.org/packages/FSharp.TypeProviders.StarterPack/ "FSharp.TypeProviders.StarterPack "
[link04]: https://raw.github.com/fsharp/FSharp.Data/master/src/CommonProviderImplementation/ProvidedTypes.fsi
[link05]: https://raw.github.com/fsharp/FSharp.Data/master/src/CommonProviderImplementation/ProvidedTypes.fs
[link06]: http://msdn.microsoft.com/ja-jp/library/dd233212.aspx "コード クォート"
[link07]: https://github.com/mavnn/Mavnn.Blog.TypeProvider 
[link08]: https://github.com/fsharp/FSharp.Data/blob/master/src/CommonProviderImplementation/ProvidedTypes.fs#L1876

[img01]: img/img01.png
