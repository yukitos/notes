# 鉄道指向プログラミング：カーボン化版(翻訳) #
FizzBuzzを実装する3通りの方法

原文：[Railway oriented programming: Carbonated edition][link00]

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 

[鉄道指向プログラミング][link01] [(原文)][link02] のフォローアップとして、同じテクニックを使って [FizzBuzz][link03] 問題を解いて、他の実装との違いを比較してみようと思います。

今回の記事のほとんどは、 [Dave Fayram氏のFizzBuzzに関する記事][link04] に [raganwald][link05] 氏のアイディアを加えたもの <del>のパクリ</del> にインスパイアされたものになっています。

## FizzBuzz: 命令型バージョン ##

一応補足しておくと、FizzBuzz問題とは以下のようなものです：

> 1から100までの数値に対して以下を満たすプログラムを作成せよ。
> * 3の倍数に対しては数値の代わりに Fizz と出力する
> * 5の倍数に対しては Buzz と出力する
> * 3の倍数かつ5の倍数に対しては FizzBuzz と出力する

基本的なF#プログラムとしては以下のようになるでしょう。

```fsharp
module FizzBuzz_Match =

    let fizzBuzz i =
        match i with
        | _ when i % 15 = 0 ->
            printf "FizzBuzz"
        | _ when i % 3 = 0 ->
            printf "Fizz"
        | _ when i % 5 = 0 ->
            printf "Buzz"
        | _ ->
            printf "%i" i

        printf "; "

    // fizzbuzzを実行
    [1..100] |> List.iter fizzBuzz
```

ここでは整数 ``i`` を引数にとり、 ``match`` と ``when`` 句で様々なテストを行いつつ、適切な数値を出力するような関数 ``fizzBuzz`` を定義しました。

単純かつ直感的で、改良も簡単ですが、いくつか問題も残されています。

まず、「15」用の特別なケースが必要になっています。
「3」と「5」のケース用のコードを再利用出来ていません。
また、たとえば「7」のようなケースを追加したい場合には(「21」と「35」と「105」という)さらに特別なケースを追加しなければいけません。
当然、もっと多くのケースを追加すると特別対応しなければいけないケースも爆発的に増加します。

次に、マッチの順序が重要になっています。
もしも「15」用のケースをパターンの最後に移動させたとすると、コード自体はエラーなく動作するでしょうが、実行結果は必要条件を満たさないでしょう。
また、新しいケースを追加する必要がある場合、正しい結果が得られるようにするためにはケースの数値が最も大きいものを最初に、最も小さいものを最後に記述しなければいけません。
これでは潜在的なバグが発生するのも当然です。

では「3」と「5」のケースを再利用して、「15」のケースを用意しなくても済むように実装してみましょう：

```fsharp
module FizzBuzz_IfPrime =

    let fizzBuzz i =
        let mutable printed = false

        if i % 3 = 0 then
            printed <- true
            printf "Fizz"

        if i % 5 = 0 then
            printed <- true
            printf "Buzz"

        if not printed then
            printf "%i" i

        printf "; "

    // fizzbuzzを実行
    [1..100] |> List.iter fizzBuzz
```

この実装では「3」と「5」の両方のコードが使われているため、「15」のケースも正しく出力されます。
さらに、ケースの順序も制限されません。
任意の順序で記述できます。

しかし各ケースの独立性はもはや失われてしまったわけなので、 ケースの **いずれか** に一致したかどうかを追跡して、デフォルトケースを処理できるようにしなければいけなくなりました。
さらに可変変数も追加されています。
F#で可変変数が必要になる場合は悪い兆候でしかないので、この実装は最適解とは言えません。

しかしこのバージョンは3と5だけではない因数 (factor)をサポートできるように簡単にリファクタリングできるという **利点があります**。

以下がそのバージョンです。
今回は ``fizzBuzz`` 関数に「ルール(rules)」を渡しています。
各ルールには因数と、関連して出力されるラベルが含まれます。
そのため、``fizzBuzz``関数では都度ルールを走査して処理を実行します。

```fsharp
module FizzBuzz_UsingFactorRules =

    let fizzBuzz rules i =
        let mutable printed = false

        for factor,label in rules do
            if i % factor = 0 then
                printed <- true
                printf "%s" label

        if not printed then
            printf "%i" i

        printf "; "

    // fizzbuzzを実行
    let rules = [ (3,"Fizz"); (5,"Buzz") ]
    [1..100] |> List.iter (fizzBuzz rules)
```

数値を追加したい場合は以下のようにルールを追加するだけです：

```fsharp
module FizzBuzz_UsingFactorRules =

    // 上記と同じコード

    let rules = [ (3,"Fizz"); (5,"Buzz"); (7,"Baz") ]
    [1..100] |> List.iter (fizzBuzz rules)
```

## FizzBuzz: パイプラインバージョン ##

次のバージョンでは「パイプライン」モデルを使ってみることにしましょう。
このモデルではデータが一連の関数を通り抜けていって、最終的な結果が得られます。

今回は「3」を処理する関数、「5」を処理する関数など、複数のパイプライン関数を用意することになります。
また最終的なラベルを出力できるように、出力用の関数も用意します。

コンセプトを実現する疑似コードとしては以下のようになります：

```fsharp
data |> handleThreeCase |> handleFiveCase |> handleAllOtherCases |> printResult
```

なお追加の必須条件として、各パイプライン関数では副作用を起こさないものとします。
つまり途中の関数群では出力を **全く行わない** ものとします。
その代わりに何らかのラベルをパイプの終点まで伝搬させて、そのラベルが結果として出力されるようにします。

### パイプラインの設計 ###

まず始めに、パイプを通過するデータの形式を決めます。

先の擬似コードの一番目にある ``handleThreeCase`` 関数から始めましょう。
この入力と出力はどうなればよいでしょうか？

入力値としては、処理されるべき数値が必要なことは確実です。
しかし数値に一致した場合の出力は文字列「Fizz」になるでしょう。
あるいは一致しなければ元々の数値を返すべきかもしれません。

ではとりあえず2番目の ``handleFiveCase`` 関数を考えてみましょう。
この関数も数値が入力されるべきです。
しかし「15」に一致する場合は「Fizz」 **も** 出力して欲しいので、「Buzz」を追加すればよいはずです。

最後の ``handleAllOtherCases`` 関数は整数を文字列に変換するものですが、「Fizz」も「Buzz」も返されていなかった場合に **のみ** 処理を行います。

これらのことから、データ構造には処理対象となるデータ **および** 「以降に伝搬されるラベル」を含めるべきであることがわかります。

次は「上流の関数がラベルを作成したかどうか、どうやって知ることが出来るのだろうか」という問題です。
``handleAllOtherCases`` 関数はラベルが作成されたかどうかで処理を変える必要があります。

1つの方法としては空文字列(あるいはあのおぞましきnull文字)を使う方法がありますが、ここでは ``string option``を使うことにしましょう。

そうすると最終的に使用されるデータ型は以下のようになります：

```fsharp
type Data = { i:int; label:string option }
```

### パイプライン バージョン1 ###

このデータ構造を使用すれば ``handleThreeCase`` と ``handleFiveCase`` の挙動を定義できます。

* まず入力値 ``i`` が因数で割り切れるかチェックする。
* 割り切れる場合は ``label`` を確認する。ラベルが ``None`` の場合は ``Some "Fizz"`` または``Some "Buzz"`` に置き換える。
* ラベルが既に値を持っていた場合、「Buzz」(あるいは別の文字)を追加する。
* 入力が因数で割りきれない場合、入力データをそのまま次に渡す。

この仕様に対する実装は以下の通りです。
この関数は「Fizz」にも「Buzz」にも対応できるため、( [raganwald][link04] に倣って) ``carbonate`` という汎用的な名前にしています：

```fsharp
let carbonate factor label data =
    let { i=i; label=labelSoFar } = data
    if i % factor = 0 then
        // 新しいデータレコードを返す
        let newLabel =
            match labelSoFar with
            | Some s -> s + label
            | None -> label
        { data with label=Some newLabel }
    else
        // 未変更のデータを返す
        data
```

``handleAllOtherCases`` の実装は若干異なります：

* ラベルをチェックする。 ``None`` ではない場合には以前の関数でラベルが作成されているため何もしない。
* ラベルが ``None`` の場合は数値の文字列表現で置き換える。

コードは以下の通りです。
``labelOrDefault`` という名前をつけました：

```fsharp
let labelOrDefault data =
    let { i=i; label=labelSoFar } = data
    match labelSoFar with
    | Some s -> s
    | None -> sprintf "%i" i
```

コンポーネントが揃ったので、パイプラインとしてまとめます：

```fsharp
let fizzBuzz i =
    { i=i; label=None }
    |> carbonate 3 "Fizz"
    |> carbonate 5 "Buzz"
    |> labelOrDefault       // 文字列に変換
    |> printf "%s; "        // 出力
```

最初の関数( ``carbonate 3 "Fizz"`` )に渡す初期データを ``{ i=i; label=None }`` として作成しなければいけない点に注意してください。

最終的にまとめると以下のようになります：

```fsharp
module FizzBuzz_Pipeline_WithRecord =

    type Data = { i:int; label:string option }

    let carbonate factor label data =
        let { i=i; label=labelSoFar } = data
        if i % factor = 0 then
            // 新しいデータレコードを返す
            let newLabel =
                match labelSoFar with
                | Some s -> s + label
                | None -> label
            { data with label=Some newLabel }
        else
            // 未変更のデータを返す
            data

    let labelOrDefault data =
        let { i=i; label=labelSoFar } = data
        match labelSoFar with
        | Some s -> s
        | None -> sprintf "%i" i

    let fizzBuzz i =
        { i=i; label=None }
        |> carbonate 3 "Fizz"
        |> carbonate 5 "Buzz"
        |> labelOrDefault       // 文字列に変換
        |> printf "%s; "        // 出力

    [1..100] |> List.iter fizzBuzz
```

### パイプライン バージョン2 ###

新しいレコード型を作成したほうがドキュメント的に便利ではあるのですが、今回のように特別なデータ構造を用意せずとも単にタプル型を使用するだけで済ませたほうが簡単な場合もあります。

というわけで、タプルを使用したバージョンは以下のようになります。

```fsharp
module FizzBuzz_Pipeline_WithTuple =

    // type Data = int * string option

    let carbonate factor label data =
        let (i, labelSoFar) = data
        if i % factor = 0 then
            // 新しいデータレコードを返す
            let newLabel =
                match labelSoFar with
                | Some s -> s + label
                | None -> label
            (i, Some newLabel)
        else
            // 未変更のデータを返す
            data

    let labelOrDefault data =
        let (i, labelSoFar) = data
        match labelSoFar with
        | Some s -> s
        | None -> sprintf "%i" i

    let fizzBuzz i =
        (i, None)               // レコードの代わりにタプルを使用する
        |> carbonate 3 "Fizz"
        |> carbonate 5 "Buzz"
        |> labelOrDefault       // 文字列に変換
        |> printf "%s; "        // 出力

    [1..100] |> List.iter fizzBuzz
```

練習として、コード内の変更点を探してみてください。

### SomeとNoneの明示的なチェックを除去する ###

タプル版のコードにある ``match .. Some .. None`` という明示的にオプション型をチェックするコードは、オプション型の組み込み関数 ``map`` と ``defaultArg`` を使用するように書き換えることができます。

変更後の ``carbonate`` は以下の通りです：

```fsharp
// 変更前
let newLabel =
    match labelSoFar with
    | Some s -> s + label
    | None -> label

// 変更後
let newLabel =
    labelSoFar
    |> Option.map (fun s -> s + label)
    |> defaultArg <| label
```

また ``labelOrDefault`` は以下のようになります：

```fsharp
// 変更前
match labelSoFar with
| Some s -> s
| None -> sprintf "%i" i

// 変更後
labelSoFar
|> defaultArg <| sprintf "%i" i
```

上にある ``|> defaultArg <|`` という奇妙なイディオムが気になるかもしれません。

``defaultArg`` の引数は1番目がオプション型です。
**2番目** ではありません。
そのため通常の部分適用ではうまく動作しません。
しかし「双方向」パイプ演算であれば動作するので、やや見た目がおかしなコードになっているというわけです。

つまり以下の通りです：

```fsharp
// OK：一般的な用法
defaultArg myOption defaultValue

// エラー：パイプ演算は機能しない
myOption |> defaultArg defaultValue

// OK：双方向パイプ演算は機能する
myOption |> defaultArg <| defaultValue
```

### パイプライン バージョン3 ###

作成した ``carbonate`` 関数は任意の因数を指定できるので、先の「命令型」バージョンと同様に簡単にコードを拡張できます。

しかし「3」や「5」といった値をパイプライン中にハードコードしなければいけないところがまだ問題になりそうです：

```fsharp
|> carbonate 3 "Fizz"
|> carbonate 5 "Buzz"
```

パイプラインに動的に関数を組み込むにはどうしたらよいでしょうか？

答えは極めて単純です。
各ルールに対応する関数を動的に作成して、1つの関数へと合成してしまえばよいのです。

具体的には以下のようなコードになります：

```fsharp
let allRules =
    rules
    |> List.mpa (fun (factor,label) -> carbonate factor label)
    |> List.reduce (>>)
```

各ルールは1つの関数にマッピングされます。
そして一連の関数は ``>>`` 演算を使用して1つの関数へと合成されます。

すべてをまとめた最終的なコードは以下の通りです：

```fsharp
module FizzBuzz_Pipeline_WithRules =

    // type Data = int * string option

    let carbonate factor label data =
        let (i, labelSoFar) = data
        if i % factor = 0 then
            // 新しいデータレコードを返す
            let newLabel =
                labelSoFar
                |> Option.map (fun s -> s  + label)
                |> defaultArg <| label
            (i, Some newLabel)
        else
            // 未変更のデータを返す
            data

    let labelOrDefault data =
        let (i, labelSoFar) = data
        labelSoFar
        |> defaultArg <| sprintf "%i" i

    let fizzBuzz rules i =

        // すべてのルールから1つの関数を作成
        let allRules =
            rules
            |> List.map (fun (factor,label) -> carbonate factor label)
            |> List.reduce (>>)

        (i, None)
        |> allRules
        |> labelOrDefault       // 文字列に変換
        |> printf "%s; "        // 出力

    let rules = [ (3,"Fizz"); (5,"Buzz"); (7,"Baz") ]
    [1..105] |> List.iter (fizzBuzz rules)
```

今回の「パイプライン」バージョンを先の命令型バージョンと比較すると、より関数的な設計になっていることがわかります。
可変変数も副作用もありません(ただし最後の ``printf`` ステートメントは除きます)。

しかし ``List.reduce`` には潜在的なバグがあります。
何か分かりますか？ [**](#note01)
この問題に対する議論および修正方法についてはページ下部にある追伸を参照してください。

<a name="note01">**</a>
ヒント：空のルールを渡してみましょう。

## FizzBuzz: 鉄道指向バージョン ##

先のパイプラインバージョンはFizzBuzzを完璧に関数型的に実装出来ていますが、試しに [鉄道指向プログラミング][link01] [(原文)][link02] で説明した2路線モデルを使って実装してみましょう。

簡単に復習しておくと、「鉄道指向プログラミング」(あるいは"Either"モナド)では「Success」と「Failure」のいずれかとなるユニオン型を定義して、それぞれが異なる路線を表していると説明しました。
そして一連の「2路線」関数を連結すると路線が作成できます。

実際に使用した関数の多くは「スイッチ」あるいは「ポイント」関数と呼ばれる、 **1** 路線入力に対して1つは成功ケース用、もう1つは失敗ケース用の2路線出力を返すような関数です。
これらのスイッチ関数は「bind」という関数を使用して2路線関数へと変換できます。

今回必要になる定義を含んだモジュールは以下の通りです：

```fsharp
module RailwayCombinatorModule =

    let (|Success|Failure|) =
        function
        | Choice1Of2 s -> Success s
        | Choice2Of2 f -> Failure f

    /// 1つの値を2路線の結果に変換します
    let succeed x = Choice1Of2 x

    /// 1つの値を2路線の結果に変換します
    let fail x = Choice2Of2 x

    // 成功用関数または失敗用関数のいずれかを実行します
    let either successFunc failureFunc twoTrackInput =
        match twoTrackInput with
        | Success s -> successFunc s
        | Failure f -> failureFunc f

    // スイッチ関数を2路線関数に変換します
    let bind f =
        either f fail
```

ここではF#コアライブラリの組み込み型である ``Choice`` 型を使用しています。
しかしこの型をSuccess/Failure型として扱うことができるよう、補助関数としてアクティブパターン1つと生成関数2つを用意しています。

さて、ここからどうやってFizzBuzzを実装していけばよいでしょう？

まずはわかりやすいところから始めましょう。
成功時は「カーボン化」されたもの (carbonation) を返し、失敗時はマッチしなかった整数値を返す関数を用意します。

別の言い方をすれば、成功路線にはラベルが、失敗路線には整数が含まれることになります。

「スイッチ」関数版の ``carbonate`` は以下のようになります：

```fsharp
let carbonate factor label i =
    if i % factor = 0 then
        succeed label
    else
        fail i
```

この実装は先のパイプライン版とほぼ同じものですが、入力がレコードやタプルではなく単なるintになっているのでこちらの方が簡潔です。

次にコンポーネントを連結する機能が必要です。
ロジックとしては以下のようになります：

* intが既にカーボン化されていた場合は無視する
* intがカーボン化されていない場合は次のスイッチ関数の入力に連結する

実装は以下の通りです：

```fsharp
let connect f =
    function
    | Success x -> succeed x
    | Failure i -> f i
```

別の実装方法としてはライブラリ内で既に定義済みの ``either`` 関数を使用できます：

```fsharp
let connect' f =
    either succeed f
```

いずれも機能としては全く同じものであることを理解しておいてください！

次に以下のような「2路線」関数を作成します：

```fsharp
    let fizzBuzz =
        carbonate 15 "FizzBuzz"     // 短絡評価用に 15-FizzBuzz ルールを定義
        >> connect (carbonate 3 "Fizz")
        >> connect (carbonate 5 "Buzz")
        >> either (printf "%s; ") (printf "%i; ")
```

このコードは一見すると「1路線」パイプライン関数と同じものに見えますが、実際には別のテクニックを使用しています。
ここではパイプ演算( ``|>`` )ではなく、関数合成( ``>>`` )を使用しています。

また、さらにいくつかの変更点があります：

* 再び「15」用のテストを追加する必要があります。
  これは(成功あるいは失敗という)2路線しかないためです。
  「3」の出力に「5」の結果を追加できるような「未完了の路線」はありません。
* 以前の例には存在していた ``labelOrDefault`` 関数は ``either`` 関数に置き換わりました。
  成功の場合には文字列が出力されます。
  失敗の場合にはintが出力されます。

実装コードの全容は以下の通りです：

```fsharp
module FizzBuzz_RailwayOriented_CarbonationIsSuccess =

    open RailwayCombinatorModule

    let carbonate factor label i =
        if i % factor = 0 then
            succeed label
        else
            fail i

    let connect f =
        function
        | Success x -> succeed x
        | Failure i -> f i

    let fizzBuzz =
        carbonate 15 "FizzBuzz"     // 短絡評価用に 15-FizzBuzz ルールを定義
        >> connect (carbonate 3 "Fizz")
        >> connect (carbonate 5 "Buzz")
        >> either (printf "%s; ") (printf "%i; ")

    // テスト
    [1..100] |> List.iter fizzBuzz
```

### 失敗時にカーボン化では？ ###

上記では「成功時」をカーボン化するよう定義しました。
確かにその方が自然に見えるはずです。
しかし鉄道指向プログラミングモデルでは、「成功」時にはデータを次の関数に渡し、「失敗」時には以降の関数をスキップして最後まで進めてしまうようになっていたことを思い出してください。

FizzBuzzの場合、「途中の関数をすべてスキップする」路線がカーボン化ラベルを返す路線で、「次の関数にデータを渡す」路線がintを返す路線になるはずです。

つまり本当はそれぞれの路線を逆にして、「失敗」時にはカーボン化を、「成功」時にはカーボン化しないようにするべきです。

そのためには、独自の ``connect`` 関数を作成するのをやめて、定義済みの ``bind`` 関数を再利用すればよいでしょう。

路線を逆転させた実装コードは以下の通りです：

```fsharp
module FizzBuzz_RailwayOriented_CarbonationIsFailure =

    open RailwayCombinatorModule

    // 値をカーボン化
    let carbonate factor label i =
        if i % factor = 0 then
            fail label
        else
            succeed i

    let fizzBuzz =
        carbonate 15 "FizzBuzz"
        >> bind (carbonate 3 "Fizz")
        >> bind (carbonate 5 "Buzz")
        >> either (printf "%i; ") (printf "%s; ")

    // テスト
    [1..100] |> List.iter fizzBuzz
```

### 果たして2路線とは何のこと？ ###

上記のように、路線を逆に実装してしまいやすいということは設計上の弱点があるということでしょう。
不適切なデザインを使っているのではないでしょうか？

ところで、何故一方の路線を「成功」、他方を「失敗」にしなければいけないのでしょう？
いずれにもそれほど違いが無いように見えます。

では2路線のアイディアは維持しつつ、「成功」と「失敗」というラベルを取り除いてしまうことにしましょう。

その代わりに1つを「カーボン化」、もう1つを「非カーボン化」という呼び方にします。

そこで、「成功/失敗」用に作成したものと同じようなアクティブパターンや生成関数を以下のように用意します：

```fsharp
let (|Uncarbonated|Carbonated|) =
    function
    | Choice1Of2 u -> Uncarbonated u
    | Choice2Of2 c -> Carbonated c

/// 1つの値を2路線の結果に変換します
let uncarbonated x = Choice1Of2 x
let carbonated x = Choice2Of2 x
```

もしも読者がドメイン駆動デザインを実践しているのであれば、コンテキストに適さないような言語ではなく、適切な [ユビキタス言語 (Ubiquitous Language)][link05] を使用してコードを作成してみるとよいでしょう。

今回の場合、FizzBuzzが対象ドメインだとすると「success」あるいは「failure」ではなく ``carbonated`` や ``uncarbonated`` という、ドメインにふさわしい名前の関数にするとよいでしょう。

```fsharp
let carbonate factor label i =
    if i % factor = 0 then
        carbonated label
    else
        uncarbonated i

let connect f =
    function
    | Uncarbonated i -> f i
    | Carbonated x -> carbonated x
```

ちなみに ``connect`` 関数は ``either`` 関数(あるいは先と同様に ``bind`` 関数)を使用して書き直すこともできます：

```fsharp
let connect' f =
    either f carbonated
```

モジュール全体としては以下のようになります：

```fsharp
module FizzBuzz_RailwayOriented_UsingCustomChoice =

    open RailwayCombinatorModule

    let (|Uncarbonated|Carbonated|) =
        function
        | Choice1Of2 u -> Uncarbonated u
        | Choice2Of2 c -> Carbonated c

    /// 1つの値を2路線の結果に変換します
    let uncarbonated x = Choice1Of2 x
    let carbonated x = Choice2Of2 x

    // 値をカーボン化
    let carbonate factor label i =
        if i % factor = 0 then
            carbonated label
        else
            uncarbonated i

    let connect f =
        function
        | Uncarbonated i -> f i
        | Carbonated x -> carbonated x

    let connect' f =
        either f carbonated

    let fizzBuzz =
        carbonate 15 "FizzBuzz"
        >> connect (carbonate 3 "Fizz")
        >> connect (carbonate 5 "Buzz")
        >> either (printf "%i; ") (printf "%s; ")

    // テスト
    [1..100] |> List.iter fizzBuzz
```

### ルールの追加 ###

上記の実装にはまだいくつかの問題が残されています：

* 「15」のテストが冗長です。これを除去して「3」と「5」のテストを再利用できないでしょうか？
* 「3」と「5」のケースがハードコーディングされています。もっと動的に定義できないでしょうか？

幸いにもこれらの問題を一石二鳥に解決する方法があります。

**一連の** 「スイッチ」関数を連結する代わりに、 **並列に** 関数を「追加」すればよいのです。
[鉄道指向プログラミング][link01] [(原文)][link02] の記事では検証関数に対してこのテクニックを使用しました。
FizzBuzzの場合にはすべての因数を一度に処理することになります。

トリックは2つの関数を「追加」あるいは「連結」する関数の定義にあります。
この方法で2つの関数が加算出来れば、後はいくつでも追加していくことが出来るようになります。

では2つのカーボン化関数があるときに、これらをどうやって連結すればよいのでしょうか？

考え得るケースとしては以下の通りです：

* 両方がカーボン化された出力を返した場合、ラベルを連結して新しくカーボン化されたラベルとします
* 一方だけがカーボン化された出力を返した場合、カーボン化された値を使用します
* 両方がカーボン化されていない出力を返した場合、カーボン化されていない値のいずれかを使用します(いずれも同じ値になっているはずです)

コードは以下の通りです：

```fsharp
// 2つのカーボン化関数を連結する
let (<+>) switch1 switch2 x =
    match (switch1 x),(switch2 x) with
    | Carbonated s1,Carbonated s2 -> carbonated (s1 + s2)
    | Uncarbonated f1,Carbonated s2 -> carbonated s2
    | Carbonated s1,Uncarbonated f2 -> carbonated s1
    | Uncarbonated f1,Uncarbonated f2 -> uncarbonated f1
```

ちなみにこのコードは ``uncarbonated`` を「ゼロ」とするような整数演算になっているとみなすことができます：

```
何か + 何か = 何かを足したもの
ゼロ + 何か = 何か
何か + ゼロ = 何か
ゼロ + ゼロ = ゼロ
```

これは偶然の一致ではありません！
このような挙動をする関数型コードを今後も目にすることがあるでしょう。
詳細については今後の記事で説明していく予定です。

ともかく、この「連結」関数を使用すると ``fizzBuzz`` を次のように書き換えることができるようになります：

```fsharp
let fizzBuzz =
    let carbonateAll =
        carbonate 3 "Fizz" <+> carbonate 5 "Buzz"

    carbonateAll
    >> either (printf "%i; ") (printf "%s; ")
```

2つの ``carbonated`` 関数を加算した後、以前と同じく ``either`` に渡しています。

全体的なコードは以下の通りです：

```fsharp
module FizzBuzz_RailwayOriented_UsingAppend =

    open RailwayCombinatorModule

    let (|Uncarbonated|Carbonated|) =
        function
        | Choice1Of2 u -> Uncarbonated u
        | Choice2Of2 c -> Carbonated c

    /// 1つの値を2路線の結果に変換します
    let uncarbonated x = Choice1Of2 x
    let carbonated x = Choice2Of2 x

    // 2つのカーボン化関数を連結する
    let (<+>) switch1 switch2 x =
        match (switch1 x),(switch2 x) with
        | Carbonated s1,Carbonated s2 -> carbonated (s1 + s2)
        | Uncarbonated f1,Carbonated s2 -> carbonated s2
        | Carbonated s1,Uncarbonated f2 -> carbonated s1
        | Uncarbonated f1,Uncarbonated f2 -> uncarbonated f1

    // 値をカーボン化
    let carbonate factor label i =
        if i % factor = 0 then
            carbonated label
        else
            uncarbonated i

    let fizzBuzz =
        let carbonateAll =
            carbonate 3 "Fizz" <+> carbonate 5 "Buzz"

        carbonateAll
        >> either (printf "%i; ") (printf "%s; ")

    // テスト
    [1..100] |> List.iter fizzBuzz
```

この新しいロジックを使用すれば、ルールを使用するようにリファクタリングすることも簡単です。
先の「パイプライン」版の実装と同じように、``reduce`` を使用してルール全体を1つの関数として加算していけばよいだけです。

ルールを使用するバージョンのコードは以下の通りです：

```fsharp
module FizzBuzz_RailwayOriented_UsingAddition =

    // 上記のコードと同じ

    let fizzBuzzPrimes rules =
        let carbonateAll =
            rules
            |> List.map (fun (factor,label) -> carbonate factor label)
            |> List.reduce (<+>)

        carbonateAll
        >> either (printf "%i; ") (printf "%s; ")

    // テスト
    let rules = [ (3,"Fizz"); (5,"Buzz"); (7,"Baz") ]
    [1..105] |> List.iter (fizzBuzzPrimes rules)
```

## まとめ ##

この記事では3種類の実装を紹介しました：

* 命令型バージョン：ロジック内では可変値や副作用を利用
* 「パイプライン」バージョン：一連の関数でデータ構造を渡していく
* 「鉄道指向」バージョン：2路線を使用して、関数を並列に「加算」して呼び出す

個人的には命令型バージョンは最悪です。
簡単に改良できるとはいえ、脆弱かつエラーを招きやすいという多くの問題があります。

残る2つでは、今回の問題であれば「鉄道指向」バージョンの方が明瞭だと思います。

タプルや特別なレコードではなく、 ``Choice`` 型を使用することでコード全体をきれいにまとめることができました。
パイプラインバージョンの ``carbonate`` と鉄道指向バージョンの ``carbonate`` を比較してみるとその違いが分かるのではないでしょうか。

当然ながら別のシナリオでは鉄道指向アプローチが採用できないこともあるでしょう。
また、パイプラインバージョンの方が適切な場合もあります。
この記事が両者の利点を判断する手助けとなれば幸いです。

> FizzBuzzに興味がある方は [Functional Reactive Programming][link06] のページを参照してみてください。
> このページでは様々なバリエーションの問題を紹介しています。

## 追伸：List.reduceを使用する場合の注意点 ##

``List.reduce`` には注意が必要です。
この関数は空のリストを渡すとエラーになります。
そのため、もしも空のルールセットを指定してしまうと ``System.ArgumentException`` 例外が発生します。

パイプラインの場合、モジュールに以下のようなコードを追加すれば上記の例外が確認できます：

```fsharp
module FizzBuzz_Pipeline_WithRules =

    // 先と同じコード

    // バグ
    let rules = []
    [1..105] |> List.iter (fizzBuzz rules)
```

この問題を修正するには ``List.reduce`` を ``List.fold`` に変更します。
``List.fold`` には初期値(あるいは「ゼロ」の値)を指定する必要があります。
今回の場合、初期値としては値を変更せずに返す関数 ``id`` を使用できます。

修正したコードは以下のようになります：

```fsharp
let allRules =
    rules
    |> List.map (fun (factor,label) -> carbonate factor label)
    |> List.fold (>>) id
```

同様に、鉄道指向バージョンの以下のコードは：

```fsharp
let allRules =
    rules
    |> List.map (fun (factor,label) -> carbonate factor label)
    |> List.reduce (<+>)
```

正しくは以下のようにすべきです：

```fsharp
let carbonateAll =
    rules
    |> List.map (fun (factor,label) -> carbonate factor label)
    |> List.fold (<+>) zero
```

この ``zero`` はリストが空だった場合に使用される「デフォルト」関数です。

課題として上記の ``zero`` 関数を作成してみてください。
(ヒント：既に別の名前で定義されたものがあります)

[link00]: http://fsharpforfunandprofit.com/posts/railway-oriented-programming-carbonated/ "Railway oriented programming: Carbonated edition"
[link01]: Railway%20oriented%20programming.md "鉄道指向プログラミング(翻訳)"
[link02]: http://fsharpforfunandprofit.com/posts/railway-oriented-programming-carbonated/ "Railway oriented programming: Carbonated edition"
[link03]: http://imranontech.com/2007/01/24/using-fizzbuzz-to-find-developers-who-grok-coding/ "FizzBuzz problem"
[link04]: http://weblog.raganwald.com/2007/01/dont-overthink-fizzbuzz.html "Don't Overthink FizzBuzz"
[link05]: http://martinfowler.com/bliki/UbiquitousLanguage.html "UbiquitousLanguage"
[link06]: http://fsharpforfunandprofit.com/posts/concurrency-reactive/ "Functional Reactive Programming"
