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

入力を始めると...キタコレ！
Intellisenseに新しく作った型とstaticプロパティが一覧表示される。
かつてないほど長ったらしい"Hello World"プログラムを含んだ
スクリプトをF# Interactiveで評価してみよう。

**この新しいVisual Studioのインスタンスは
型プロバイダーを再コンパイルする時に
毎回終了させる必要がある。**

----

## 何が起きているのか？

[link01]: http://blog.mavnn.co.uk/type-providers-from-the-ground-up/ "Type Providers From the Ground Up"
[link02]: http://blogs.msdn.com/b/dsyme/archive/2013/01/30/twelve-type-providers-in-pictures.aspx "Twelve F# type providers in action"
[link03]: https://www.nuget.org/packages/FSharp.TypeProviders.StarterPack/ "FSharp.TypeProviders.StarterPack "
[link04]: https://raw.github.com/fsharp/FSharp.Data/master/src/CommonProviderImplementation/ProvidedTypes.fsi
[link05]: https://raw.github.com/fsharp/FSharp.Data/master/src/CommonProviderImplementation/ProvidedTypes.fs
