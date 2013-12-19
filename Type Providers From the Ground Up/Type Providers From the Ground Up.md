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
なおこれらのファイルを自前で再作成しようなどとはきっと思わないことだろう。

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
ハードウェアを使ってるよね？)、そこでF#プロジェクトを作成する。
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
これはつまり非F#言語からアクセスした場合には、コンパイラ的には `object` に見えることになる。

なかなかいい感じだ。
ただしstaticプロパティのちょっと異様な `<@@ ... @>` シンタックスを除けば。
見ての通り、このコードは `"Hello world"` を返すようなプロパティのgetメソッドを
生成するためのものだけれども、ではどうやって？

この文法は [コードクォート][link06] を表すもので、
プログラム内にコンパイルされるのではなく、
1つの式を表すオブジェクトとしてコンパイルされる。

頭が痛くなった？
僕もだよ...
ここではコードクォートの詳細は省略する(僕も詳しいことはわかってないし！)けれども、
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




[link01]: http://blog.mavnn.co.uk/type-providers-from-the-ground-up/ "Type Providers From the Ground Up"
[link02]: http://blogs.msdn.com/b/dsyme/archive/2013/01/30/twelve-type-providers-in-pictures.aspx "Twelve F# type providers in action"
[link03]: https://www.nuget.org/packages/FSharp.TypeProviders.StarterPack/ "FSharp.TypeProviders.StarterPack "
[link04]: https://raw.github.com/fsharp/FSharp.Data/master/src/CommonProviderImplementation/ProvidedTypes.fsi
[link05]: https://raw.github.com/fsharp/FSharp.Data/master/src/CommonProviderImplementation/ProvidedTypes.fs
[link06]: http://msdn.microsoft.com/ja-jp/library/dd233212.aspx "コード クォート"
