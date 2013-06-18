# プロジェクト内のモジュールを整理する(翻訳) #
関数型アプリケーションのためのレシピ パート3

原文：[Organizing modules in a project][link01]

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 

レシピのコードを書き進める前に、F#プロジェクトの全体的な構造をまず確認することにしましょう。
具体的には(a) どのコードをどのモジュールに含めるべきか、(b) プロジェクト内のモジュールはどのように整理するべきか という2点です。

## すべきではないこと ##

F#を始めたばかりだと、コードをC#と同じようにクラスの内部にまとめたがるかもしれません。
1ファイルに1クラスで、アルファベット順。
どのみち、F#はC#と同じようにオブジェクト指向の機能をサポートしているわけでしょう？
だったらF#のコードもC#と同じ方法で整理すればいいはずですよね？

しばらくするとF#ではファイル(およびファイル内のコード)が依存する順序(dependency order)に並べられていなければいけないことに気付きます。
つまりF#では前方参照を使用して、コンパイラがまだ見たことのないコードを参照することはできないのです。[**](#note01)

[他にも面倒くさいこと][link02]や不満なことがあります。
どうしてF#はこんな馬鹿なことになってしまったんでしょう？
こんな調子では巨大なプロジェクトを開発することなんて無理に決まっています！

この記事ではそんなことになってしまうことが無いように、コードを整理する簡単な方法を説明していきます。

<a name="note01">**</a>
``and``キーワードを使用することで相互参照を解決できる場合もありますが、それでも冗長です。

## レイヤー設計に対する関数的アプローチ ##

コードを検討する標準的な方法の1つはレイヤー毎にグループ化することです。
つまり以下のようにドメインレイヤー、プレゼンテーションレイヤーといった具合にします：

![レイヤー設計][img01]

それぞれのレイヤーにはレイヤーに関連のあるコード **だけを** 配置します。

しかし実際にはこんなに単純では無く、レイヤーをまたがった依存があることもよくあります。
ドメインレイヤーはインフラレイヤーに依存し、プレゼンテーションレイヤーはドメインレイヤーに依存するといった具合です。

そして最も重要なこととして、ドメインレイヤーはパーシステンスレイヤーに依存しては **いけません** 。
これは [persistence agnostic"(「永続化の不可知論」)][link03] に従うべきだからです。

そういうわけで、先ほどのダイアグラムは以下のように書き換える必要があります(矢印が依存性を表しています)：

![変更後のレイヤーダイアグラム][img02]

また、理想的にはアプリケーションサービスやドメインサービスなどがある「サービスレイヤー」も含めて、より詳細な粒度で再構成すべきです。
そして再構成の作業が終われば、コアのドメインクラスは「純粋」で、かつドメインの外部には全く依存しないようになるでしょう。
この状態は ["hexagonal architecture"(「六角形アーキテクチャ」)][link04] あるいは ["onion architecture"(「玉葱アーキテクチャ」)][link05] と呼ばれます。
しかしこの記事のテーマはオブジェクト指向設計の機微ではないので、今のところはもっと単純なモデルを採用することにします。

## 型から挙動を分離する ##

**「10種のデータ構造を処理できる機能を10個用意するより、1種のデータ構造を処理できる機能を100個用意した方がよい」 アラン・パリス**

関数型デザインにおいて、 **データと挙動を分離しておく** ことは非常に重要です。
データ型とは単純で「無能」であるべきです。
そしてそれとは別に、これらデータ型を処理できるような多数の関数を用意します。

これはオブジェクト指向デザインにおいてデータと挙動が関連しているべきという方針とは真逆です。
結局のところ、それがクラスの役割なのです。
実際、真にオブジェクト指向のデザインでは挙動 **以外のもの** を持つべきではありません。
データはプライベートにしておき、メソッド経由でのみアクセスできるようになっているべきです。

事実、OODではデータ型に関連のある挙動が十分に揃えられていないことが悪とされ、["anemic domain model"(「無気力ドメインモデル」)][link06]という名前で呼ばれることもあります。

しかし関数型デザインの場合、透過的な「無能データ」の方が良しとされます。
一般的にはカプセル化もされていないようなデータで十分です。
データは不変であるので、間違った機能によって「傷つけられる」こともありません。
そしてデータを透過的にしておくことによって、より柔軟かつ汎用的なコードが作成できるようになります。

このアプローチによる利点について解説している [Rich Hickeyによる「The Value of Values」というタイトルのトーク][link07] をまだ見ていないのであれば是非見ておくことをおすすめします。

### 型レイヤーとデータレイヤー ###

さてではどうやって上記のレイヤー設計を組み込めばいいのでしょうか？

まずそれぞれのレイヤーを2つに分離します：

* **データ型** レイヤーによって使用されるデータ構造
* **ロジック** レイヤーで実装される機能

これらを分離するとダイアグラムは以下のようになります：

![レイヤーダイアグラムを分割][img03]

ただしここには(赤線で示したような)後方参照が含まれていることに注意してください。
たとえばドメインレイヤーの関数は``IRepository``のような永続性に関わるような型に依存することがあります。

OO設計の場合、この参照を解決するために(アプリケーションサービスのような)[レイヤーをさらに追加する][link07]ことになるでしょう。
しかし関数型の場合、その必要はありません。
永続性に関わるような型を単に階層の別の位置、たとえば下図のようにドメイン機能の下に移動するだけで済みます：

![永続化インターフェイスを移動][img04]

この設計であればレイヤー間におけるすべての循環参照が解消できます。
 **すべての矢印が下方しか指していません。**

また、特別なレイヤーを追加することもなく、オーバーヘッドもありません。

最後にこのレイヤー設計を上下逆転させた形でF#ファイルに変換します。

* プロジェクトの最初には他に全く依存しないファイルを置きます。
  このファイルは上のレイヤーダイアグラムにおける **一番下** の機能に該当します。
  一般的にはインフラストラクチャやドメインの型など、一連の型が含まれることになります。
* 次は1つめのファイルにしか依存しないファイルを置きます。
  レイヤーダイアグラムにおける下から2番目の機能に該当します。
* 以下同様。既存のファイルにしか依存しないようにファイルを追加していきます。

さて、[パート1][link08]で使用した例を振り返ってみましょう：

![リクエストのレイヤー][img05]

F#プロジェクト内で対応するコードを用意すると以下のようになります：

![F#プロジェクト内のファイル][img06]

リストの一番下には「main」あるいは「program」という名前で、プログラムのエントリポイントを含むようなファイルが置かれることになります。

そして下から2番目のコードにはアプリケーションのユースケースが来ます。
このファイル中にはその他のモジュールが「組み合わされて」、特定のユースケースやサービスリクエストを表すような関数を実装するコードがすべて定義されます。
(OOデザインで言うところの ["application services"(「アプリケーションサービス」)][link09] が最も近いものだと言えます。)

また、その上には「UIレイヤー」や「DBレイヤー」などが続きます。

このアプローチの利点としては、このコードを初めて目にした場合であっても、どこから手をつければいいのかがすぐにはっきりとわかるという点です。
上方にあるファイルは常に「低レイヤー」のコードで、下方にあるファイルは常に「高レイヤー」のコードになっています。
フォルダなどは必要ありません！

## コードをクラスではなくモジュールに配置する ##

F#を始めたばかりの頃は「クラスを使わないのならコードをどこに書けばいいのだろう？」とよく疑問に思うものです。

その答えは **モジュール** です。
既に説明した通り、オブジェクト指向プログラムではデータとそのデータに関連する関数が共に1つのクラス内に定義されます。
しかしF#のような関数型スタイルの場合、データ構造とデータ操作に関連する関数はモジュール内に定義します。

型と関数の組み合わせとしては以下の3パターンがあります：

* 関数と同じモジュール内で型を宣言する
* 同じファイル中で型と関数を別々に定義する
* 型と関数を別のファイルに定義しておき、型の宣言だけしか含まないようなファイルを用意する

1番目のアプローチの場合、型は関連する関数と共にモジュール **内** に定義されます。
主要な型が1つしかない場合、「T」のような単純な名前か、モジュールの名前にします。

たとえば以下のようになります：

```fsharp
namespace Example

module Person =
    
    type T = { First:string; Last:string }

    // コンストラクタ
    let create first last =
        { First=first; Last=last }

    // この型を操作するメソッド
    let fullName { First=first; Last=last } =
        first + " " + last
```

そうすると型自身は ``Person.T`` という名前でアクセスすることになりますが、それぞれの関数は ``Person.create`` あるいは ``Person.fullName`` という名前でアクセスできます。

2番目のアプローチの場合、型は同じファイル内で定義されますが、モジュールの外部で定義します：

```fsharp
namespace Example

// モジュールの外部で型を宣言する
type PersonType = { First:string; Last:string }

// この型を操作するメソッド用のモジュールを宣言する
module Person =

    // コンストラクタ
    let create first last =
        { First=first; Last=last }

    // この型を操作するメソッド
    let fullName { First=first; Last=last } =
        first + " " + last
```

この場合、型は ``PersonType`` という名前でアクセスできますが、それぞれの関数は先ほどと同じ名前( ``Person.create`` や ``Person.fullName`` )でアクセスできます。

最後に3番目のアプローチです。
型は(通常は別個のファイルとして用意される)特別な「型専用」のモジュールで定義します：

```fsharp
// ===============================
// ファイル名： DomainTypes.fs
// ===============================
namespace Example

// 「型専用」モジュール
[<AutoOpen>]
module DomainTypes =
    
    type Person = { First:string; Last:string }

    type OtherDomainType = ...

    type ThirdDomainType = ...
```

このアプローチの場合、 ``AutoOpen`` 属性を指定してプロジェクト内のすべてのモジュール内で型が自動的に利用できるように、つまり「グローバル」にしておく方法が一般的です。

そしてたとえば ``Person`` 型を操作する関数をすべて含むモジュールを別途用意します。

```fsharp
// ===============================
// ファイル名： Person.fs
// ===============================
namespace Example

// 特定の型に対する関数用のモジュールを宣言する
module Person =

    // コンストラクタ
    let create first last =
        { First=first; Last=last }

    // この型を操作するメソッド
    let fullName { First=first; Last=last } =
        first + " " + last
```

この例の場合、型とモジュールの名前がいずれも ``Person`` であることに注意してください。
実際にはこれが問題となることはなく、コンパイラが適宜必要な方を選択してくれます。

つまり以下のように記述したとします：

```fsharp
let f (p:Person) = p.First
```

そうするとコンパイラは ``Person`` 型を参照しているのだと認識します。

一方、以下のように記述したとしましょう：

```fsharp
let g () = Person.create "Alice" "Smith"
```

この場合にはコンパイラは ``Person`` モジュールを参照しているのだと認識します。

モジュールの詳細については [organizing functions(関数を整理する)][link10] を参照してください。

## モジュールの整理 ##

今回のレシピでは以下のガイドラインに則って、複数のアプローチを組み合わせて使用します：

### モジュールガイドライン ###

型が複数のモジュールから利用される場合、特別な型専用のモジュールに配置します。

* たとえば型がグローバルに使用される(あるいはDDD的に言えば「束縛されたドメイン」内で使用される)場合、 ``DomainTypes`` あるいは ``DomainModel`` という名前のモジュール内で、かつ早い段階でコンパイルされるファイル内で型を定義します。
* 型がたとえばいくつかのUIモジュールのようなサブシステムでのみ使用される場合、 ``UITypes`` といったモジュール内で、その他のUIモジュールの直前にコンパイルされるファイル内で型を定義します。

型が1つ(ないしは2つ)のモジュールでしか使用されないプライベートなものである場合、関連する機能と同一のモジュール内で定義します。

* たとえば検証機能においてのみ利用される型は ``Validation`` モジュール、データベースへのアクセスでのみ利用される型は ``Database`` モジュール内で定義するという具合です。

当然ながら型を整理する方法は他にもまだまだありますが、上記ガイドラインはなかなか良い標準ガイドラインになるのではないでしょうか。

### おいおい、フォルダはどこに行ってしまったんだ？ ###

F#プロジェクトの既知の制限として、フォルダ構造がサポートされておらず、そのために巨大なプロジェクトを整理できないのではないかということがよく話題に上ります。

純粋にオブジェクト指向デザインに従っているのであればこの苦情はもっともなものです。
しかしこれまでの説明からすれば、モジュールを直列的なリストにして依存性を正しく管理してやれば十分実用的であることがわかるはずです。
確かに理論上はファイルがバラバラになっていてもコンパイラが正しい順序を見つけ出すことが出来るはずではありますが、実際には正しい順序を見つけ出すのはそれほど簡単な話ではありません。

さらに重要なのは、 **人が** 正しい順序を見つけ出すことはさらに難しいわけで、必要以上に管理が大変になってしまうことでしょう。

巨大なプロジェクトの場合、現実的にはフォルダが無くても思うほどは問題になりません。
F#コンパイラ自身のように、この制限があるままでもきちんと統制されている巨大なF#プロジェクトもいくつかあります。
詳細については [cycles and modularity in the wild(現存プロジェクトの循環性とモジュール性)][link11] を参照してください。

### 型が相互依存している場合にはどうしたらいい？ ###

OOデザイン畑から移動してくると、以下のように相互依存している型が出てきてコンパイルできなくなることがあります：

```fsharp
type Location = { name:string; workers:Employee list }

type Employee = { name:string; worksAt:Location }
```

F#コンパイラが幸せになるためにはどう手直ししたらよいのでしょう？

それほど難しい話ではありませんが、もう少し説明が必要になるので別途 [循環参照の扱いに関する記事][link12] を参照してください。

## サンプルコード ##

前回までのパートで作成したコードに話を戻しますが、今回はモジュールとしてコードを整理します。

以下の各モジュールは基本的には個別のファイルに含まれます。

なお以下のコードはスケルトンになっていることに注意してください。
いくつかのモジュールが不足していたり、空のままのモジュールになっていたりします。

このような整理方法は小さなプロジェクトにとってはやり過ぎかもしれませんが、コードは大きく成長するものです！

```fsharp
/// ========================================================
/// 複数のプロジェクト間で共有される型および関数
/// ========================================================
module CommonLibrary =

    // 2路線型
    type Result<'TSuccess, 'TFailure> =
        | Success of 'TSuccess
        | Failure of 'TFailure

    // 1入力を2路線用の値に変換します
    let succeed x =
        Success x

    // 1入力を2路線用の値に変換します
    let fail x =
        Failure x

    // 成功用関数または失敗用関数のいずれかを適用します
    let either successFunc failureFunc twoTrackInput =
        match twoTrackInput with
        | Success s -> successFunc s
        | Failure f -> failureFunc f

    // スイッチ関数を2路線関数に変換します
    let bind f =
        either f fail

    // 2路線値をスイッチ関数に接続します
    let (>>=) x f =
        bind f x

    // 2つのスイッチを1つに連結します
    let (>=>) s1 s2 =
        s1 >> bind s2

    // 1路線関数をスイッチに変換します
    let switch f =
        f >> succeed

    // 1路線関数を2路線関数に変換します
    let map f =
        either (f >> succeed) fail

    // 行き止まり関数を1路線関数に変換します
    let tee f x =
        f x; x

    // 1路線関数を例外処理ありのスイッチに変換します
    let tryCatch f exnHandler x =
        try
            f x |> succeed
        with
        | ex -> exnHandler ex |> fail

    // 2つの1路線関数を1つの2路線関数に変換します
    let doubleMap successFunc failureFunc =
        either (successFunc >> succeed) (failureFunc >> fail)

    // 2つのスイッチを並列に加算します
    let plus addSuccess addFailure switch1 switch2 x =
        match (switch1 x),(switch2 x) with
        | Success s1,Success s2 -> Success (addSuccess s1 s2)
        | Failure f1,Success _  -> Failure f1
        | Success _ ,Failure f2 -> Failure f2
        | Failure f1,Failure f2 -> Failure (addFailure f1 f2)

/// ========================================================
/// 現在のプロジェクト内でグローバルな型
/// ========================================================
module DomainTypes =

    open CommonLibrary

    /// リクエストに対するDTO
    type Request = { name:string; email:string }

    // その他多くの型についてはまた後で！

/// ========================================================
/// ログ用関数
/// ========================================================
module Logger =

    open CommonLibrary
    open DomainTypes

    let log twoTrackInput =
        let success x = printfn "DEBUG. 今のところ問題なし: %A" x; x
        let failure x = printfn "ERROR. %A" x; x
        doubleMap success failure twoTrackInput

/// ========================================================
/// 検証用関数
/// ========================================================
module Validation =

    open CommonLibrary
    open DomainTypes

    let validate1 input =
        if input.name = "" then Failure "名前を入力してください"
        else Success input

    let validate2 input =
        if input.name.Length > 50 then Failure "名前は50文字以下で入力してください"
        else Success input

    let validate3 input =
        if input.email = "" then Failure "メールアドレスを入力してください"
        else Success input

    // 検証用関数を「加算する」関数
    let (&&&) v1 v2 =
        let addSuccess r1 r2 = r1 // 1つめを返します
        let addFailure s1 s2 = s1 + "; " + s2 // 連結します
        plus addSuccess addFailure v1 v2

    let combinedValidation =
        validate1
        &&& validate2
        &&& validate3

    let canonicalizeEmail input =
        { input with email = input.email.Trim().ToLower() }

/// ========================================================
/// データベース関数
/// ========================================================
module CustomerRepository =

    open CommonLibrary
    open DomainTypes

    let updateDatabase input =
        () // 今のところはダミーの行き止まり関数

    // 例外処理を行う新しい関数
    let updateDatabaseStep =
        tryCatch (tee updateDatabase) (fun ex -> ex.Message)

/// ========================================================
/// すべてのユースケースやサービスを一カ所に配置します
/// ========================================================
module UseCase =

    open CommonLibrary
    open DomainTypes

    let handleUpdateRequest =
        Validation.combinedValidation
        >> map Validation.canonicalizeEmail
        >> bind CustomerRepository.updateDatabaseStep
        >> Logger.log
```

## まとめ ##

今回の記事ではコードをモジュールとして整頓する方法を紹介しました。
次回はついに現実的なコーディングに取り組んでいきます！

それまでの間、相互依存に関する以下の記事に目を通しておくことをおすすめします：

* [Cyclic dependencies are evil(循環依存は悪手である)][link12]
* [Refactoring to remove cyclic dependencies.(循環依存をリファクタリングで取り除く)][link13]
* [Cycles and modularity in the wild(現存プロジェクトの循環性とモジュール性)][link11]
  この記事では実際のC#およびF#プロジェクトを比較しています

[link01]: http://fsharpforfunandprofit.com/posts/recipe-part3/ "Organizing modules in a project"
[link02]: http://www.sturmnet.org/blog/2008/05/20/f-compiler-considered-too-linear "F# compiler considered too linear"
[link03]: http://stackoverflow.com/questions/905498/what-are-the-benefits-of-persistence-ignorance "What are the benefits of Persistence Ignorance?"
[link04]: http://alistair.cockburn.us/Hexagonal+architecture "Hexagonal architecture"
[link05]: http://jeffreypalermo.com/blog/the-onion-architecture-part-1/ "Onion architecture"
[link06]: http://www.martinfowler.com/bliki/AnemicDomainModel.html "AnemicDomainModel"
[link07]: http://c2.com/cgi/wiki?OneMoreLevelOfIndirection "One More Level Of Indirection"
[link08]: How%20to%20design%20and%20code%20a%20complete%20program.md "How to design and code a complete program"
[link09]: http://stackoverflow.com/questions/2268699/domain-driven-design-domain-service-application-service "Domain Driven Design: Domain Service, Application Service"
[link10]: http://fsharpforfunandprofit.com/posts/organizing-functions/ "Organizing functions"
[link11]: http://fsharpforfunandprofit.com/posts/cycles-and-modularity-in-the-wild/ "Cycles and modularity in the wild"
[link12]: http://fsharpforfunandprofit.com/posts/cyclic-dependencies/ "Cyclic dependencies are evil"
[link13]: http://fsharpforfunandprofit.com/posts/removing-cyclic-dependencies/ "Refactoring to remove cyclic dependencies"

[img01]: img/03-01.png
[img02]: img/03-02.png
[img03]: img/03-03.png
[img04]: img/03-04.png
[img05]: img/01-01.png
[img06]: img/03-05.png
