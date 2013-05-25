# 鉄道指向プログラミング #
関数型アプリケーションのためのレシピ パート2
原文：[Railway oriented programming][link01]

- - - - - - - - - - - - - - - - - - - - - - - - - - - - - - 

前回はユースケースを処理単位に分解する方法と、発生したエラーを以下のようにエラー用の回路に逃がす必要があるという説明をしました：

![Combine to a single "failure" path][link02]

今回はこれらのステップ関数を様々な方法で1つのユニットとして組み立てる方法を紹介します。
関数の内部設計については別の記事で説明する予定です。

## 1つのステップを表す関数を設計する ##

まずはステップの詳細を確認していきましょう。
たとえば検証用の関数です。
これはどのように機能すべきでしょうか？
何かしらのデータを受け取って、何を出力すればいいのでしょうか？

おそらくは2つのケースが考えられます。
1つはデータが正常な場合(正常パス)、そしてもう1つは何か問題がある場合で、この場合には以下のように別の経路をたどるようにして、残る処理がスキップされるようにします：

![Validate function][link03]

しかし以前と同様に、この関数は適切な関数にはなり得ません。
関数は1つの出力しか行えないため、前回定義した``Result``を使うことになります。

```fsharp
type Result<'TSuccess, 'TFailure> =
    | Success of 'TSuccess
    | Failure of 'TFailure
```

そうするとダイアグラムも以下のようになります：

![Validate function with one output][link04]

これが実際にどのように動作するのかは、以下のような具体的な検証用関数の例で確認できるでしょう：

```fsharp
type Request = { name:string; email:string }

let validateInput input =
    if input.name = "" then Failure "名前を入力してください"
    else if input.email = "" then Failure "メールアドレスを入力してください"
    else Success input // 成功パス
```

この関数の型を確認すると、コンパイラは``Request``を受け取って成功時には``Request``を、失敗時には``string``をデータに持つような``Result``を返すものだと判断したことがわかります：

```fsharp
validateInput : Request -> Result<Request,string>
```

別の関数に対しても同じ方法で分析することができます。
いずれの関数も同じ「形」をしていることが確認できます。
つまり何らかの入力を受け取り、成功/失敗を出力するという形になっているはずです。

> 予防線：関数は2つの出力を持つことができないと説明したばかりですが、上のような関数を指して「2つの出力を行う」関数と呼んでしまうことがあるかもしれません。
> 当然ながらそれは2つのケースを出力にする関数のことを指しているつもりです。

## 鉄道指向プログラミング ##

さて、そうするとたくさんの「1入力 -> 成功/失敗を出力」という関数が揃いましたが、どうやってこれらを連結すればよいのでしょうか？

やりたいことは``Success``の出力を次の入力として渡し、ただし``Failure``出力の場合には次の関数をスキップするようにしたいということです。
この概念を表すダイアグラムは以下のようになります：

![Connect success output, or bypass next function][link05]

この状況を例えるには皆さんがおそらく見慣れている、うってつけのものがあります。
鉄道です！

鉄道にはスイッチ(イギリスではポイント)があり、電車の進路を別の路線へと切り替えることができるようになっています。
つまり「成功/失敗」関数を以下のような鉄道のスイッチと見なすことができるわけです：

!["Success/Failure" functions as railway switch][link06]

そして横につなげてみることができます。

![Two railways in a row][link07]

2つの失敗路線を連結するにはどうすればよいでしょうか？
もちろんこういう感じです！

![Connect two failure tracks][link08]

たくさんのスイッチがある場合でも、下の図のようにすればどこまでも2路線のシステムを延長できます：

![Two track system][link09]

上側の路線が成功パスで、下側が失敗パスです。

ここで少し話を戻して全体像を見てみると、2車線の線路を覆い隠すようなブラックボックス関数がいくつかあり、それぞれの関数はデータを処理した後に次の関数へとそれを受け渡していることがわかります：

![Functions straddling a two-track railway][link10]

しかし関数の中身を見ると、実際にはそれぞれの中にスイッチがあり、不正なデータであれば失敗用の路線に切り替えている形になっています：

![Inside the functions][link11]

なお一度失敗用の路線に入ってしまうと(本来であれば)成功パスには決して戻さないという点に注意してください。
終点にたどり着くまでは以降の処理をスキップさせるだけです。

## 基本的な合成 ##

それぞれの関数を「連結する(glue)」前に、合成がどのように機能するのか簡単に説明しましょう。

標準的な関数を線路上に置かれたブラックボックス(あるいはトンネル)だとします。
ここには1つの入力と1つの出力があります。

1路線用の関数を複数接続したい場合、``>>``というシンボルで表される、左から右への合成演算子を使用します。

![left-to-right composition operator][link12]

この合成演算子は2路線用の関数にも適用できます：

![composition operator for two-track functions][link13]

合成における唯一の制限は、左辺の関数における出力の型が右辺の関数における入力の型と一致しなければいけないということだけです。

今回の鉄道の例であれば、1路線出力を1路線入力、あるいは2路線出力を2路線入力に接続することができますが、2路線出力を1路線入力に接続することはできないということです。

![invalid composition][link14]

## スイッチを2路線用の入力に変換する ##

さてここで問題があります。

各ステップ用の関数はそのままだと1路線入力のスイッチになります。
しかし処理全体としては両方の路線を覆うような2路線システムになっていなければいけません。
これはつまり各関数は単に1路線入力(``Request``)ではなく、2路線入力(``Result``)できるようになっていなければいけないということです。

どうすれば2路線システムにスイッチを導入できるのでしょうか？

答えは単純です。
以下の図にあるように、各関数の「穴」あるいは「スロット」を埋めて、適切な2路線用の関数へと変換するような「アダプター」関数を用意すればよいのです：

![adapter function][link15]

また、実際のコードは以下のようになります。
この関数をここでは``bind``と名付けましたが、この名前はこのような処理を表す標準的なものです。

```fsharp
let bind switchFunction =
    fun twoTrackInput ->
        match twoTrackInput with
        | Success s -> switchFunction s
        | Failure f -> Failure f
```

bind関数はスイッチ関数を引数にとり、新しい1つの関数を返します。
新しい関数は2路線入力(``Result``型)を取り、各ケースをチェックします。
入力が``Success``であればその値を引数にして``switchFunction``を呼びます。
しかし入力が``Failure``の場合にはスイッチ関数がスキップされます。

このコードをコンパイルしてみると以下のようなシグネチャになっていることが確認できます：

```fsharp
val bind : ('a -> Result<'b,'c>) -> Result<'a,'c> -> Result<'b,'c>
```

このシグネチャは、``bind``関数が引数(``'a -> Result<..>``)を1つとり、2路線関数(``Result<..> -> Result<..>``)を出力とするというように解釈することもできます。

さらに具体的には以下の通りです：

* bindの引数(``switchFunction``)は任意の型``'a``をとり、``'b``(成功路線用)と``'c``(失敗路線用)を返します。
* リターンされた関数自体は、``'b``(成功用)と``'c``(失敗用)を型に持つ``Result``型の引数(``twoTrackInput``)をとります。
  型``'a``は``switchFunction``が期待する1路線の型と同じものです。
* リターンされた関数の出力は``'b``と``'c``という別の型を持った``Result``型です。
  この型はスイッチ関数の出力と同じ型になっています。

上記のように、この型シグネチャがまさに期待する通りのものになっていることが分かると思います。

なおこの関数は完全にジェネリックで、任意のスイッチ関数と任意の型に対応できます。
制限されるのは``switchFunction``の「形」だけで、具体的な型については特に制限されません。

### bind関数を別の方法で作成する ###

少し話がそれますが、bind関数は別の書き方をすることもできます。

1つは以下のように、内部で定義した``twoTrackInput``を2番目の引数として明示的に受け取るようにする方法です：

```fsharp
let bind switchFunction twoTrackInput =
    match twoTrackInput with
    | Success s -> switchFunction s
    | Failure f -> Failure f
```

これは最初の定義と全く同じものです。
2引数の関数が1引数の関数と全く同じだと言える理由が分からなければ、是非 [カリー化][link16] の記事を参照してください！

もう1つの書き方としては、以下のように``match..with``を``function``キーワードで書き換えてしまう方法です：

```fsharp
let bind switchFunction =
    function
    | Success s -> switchFunction s
    | Failure f -> Failure f
```

以上3つのコードスタイルを見かけることになると思いますが、引数が明確になっていたほうが非エキスパートであってもコードを読みやすいと思うため、筆者としては2番目のスタイル(``let bind switchFunction twoTrackInput =``)を推奨します。

## 例：複数の検証用関数を組み合わせる ##

コンセプトがうまくいっているかテストするために、ここで少しコードを書いてみましょう。

まずは既に定義済みのものがあります。
``Request``と``Result``、``bind``は以下の通りです：

```fsharp
type Result<'TSuccess, 'TFailure> =
    | Success of 'TSuccess
    | Failure of 'TFailure

type Request = {name:string; email:string}

let bind switchFunction twoTrackInput =
    match twoTrackInput with
    | Success s -> switchFunction s
    | Failure f -> Failure f
```

次に3つの関数を作成します。
それぞれは「スイッチ」関数で、最終的には1つの巨大な関数として組み合わせることになります：

```fsharp
let validate1 input =
    if input.name = "" then Failure "名前を入力してください"
    else Success input

let validate2 input =
    if input.name.Length > 50 then Failure "名前は50文字以下で入力してください"
    else Success input

let validate3 input =
    if input.email = "" then Failure "メールアドレスを入力してください"
    else Success input
```

次はこれらを連結できるように、それぞれの関数に対して``bind``関数を呼び出して2路線用の新しい関数を作成します。

その後、以下のように標準の関数合成演算子を使用して2路線関数を連結します：

```fsharp
/// 3つの検証用関数を1つにまとめます
let combinedValidation =
    // スイッチ関数を2路線関数に変換します
    let validate2' = bind validate2
    let validate3' = bind validate3
    // 2路線関数を連結します
    validate1 >> validate2' >> validate3'
```

``validate2'``と``validate3'``は2路線を入力とするような新しい関数です。
シグネチャを確認すると、``Result``を引数にとって``Result``をリターンするようになっていることがわかります。
しかし``validate1``は2路線入力できるように変換する必要はないという点に注意してください。
この入力は1路線のままになっていて、出力は既に2路線になっているため、合成に必要な条件を満たしています。

``validate1``(未bind)スイッチと``validate2``、``validate3``スイッチをそれぞれ``validate2'``、``validate3'``アダプターにして連結すると下図のようになります。

![combined validate functions][link16]

以下のように``bind``を「インライン化」することもできます：

```fsharp
let combinedValidation =
    // 2路線用関数を連結します
    validate1
    >> bind validate2
    >> bind validate3
```

不正な入力2パターンと正常入力1パターンをテストしてみましょう：

```fsharp
// テスト1
let input1 = {name=""; email=""}
combinedValidation input1
|> printfn "Result1=%A"

// ==> Result1=Failure "名前を入力してください"

// テスト2
let input2 = {name="Alice"; email=""}
combinedValidation input2
|> printfn "Result2=%A"

// ==> Result2=Failure "メールアドレスを入力してください"

let input3 = {name="Alice"; email="good"}
combinedValidation input3
|> printfn "Result3=%A"

// ==> Result3=Success {name = "Alice"; email = "good";}
```

是非上のコードを実際に試してみたり、違う値をテストしてみたりしてください。

> 上記3つの関数を直列ではなく並列に実行して、検証エラーを一度に取得できないだろうかと思うかもしれません。
> もちろん可能です。
> この記事で後ほど説明する予定です。

### パイプ化演算子としてのbind ###

``bind``関数の説明が続きますが、スイッチ関数をパイプ化するシンボルとしては``>>=``が一般的に使用されます。

定義は以下の通りで、左右に指定した関数を簡単に連結させることができるようになっています：

```fsharp
/// 中置演算子を作成します
let (>>=) twoTrackInput switchFunction =
    bind switchFunction twoTrackInput
```

> このシンボルは、合成演算子``>>``の後に線路のシンボル``=``を続けたものと覚えてください。

こういった演算子を使用すると、``>>=``演算子をスイッチ関数用のパイプ演算子(``|>``)とみなすことができます。

通常のパイプ演算では、左辺に1路線の値を指定して、右辺に通常の関数を指定します。
しかし「bindパイプ」演算子の場合、左辺には2路線の値を指定して右辺にスイッチ関数を指定します。



[link01]: http://fsharpforfunandprofit.com/posts/recipe-part2/ "Railway oriented programming"
[link02]: img/01-10.png "Figure 01-10.png"
[link03]: img/02-01.png "Figure 02-01.png"
[link04]: img/02-02.png "Figure 02-02.png"
[link05]: img/02-03.png "Figure 02-03.png"
[link06]: img/02-04.png "Figure 02-04.png"
[link07]: img/02-05.png "Figure 02-05.png"
[link08]: img/02-06.png "Figure 02-06.png"
[link09]: img/02-07.png "Figure 02-07.png"
[link10]: img/02-08.png "Figure 02-08.png"
[link11]: img/02-09.png "Figure 02-09.png"
[link12]: img/02-10.png "Figure 02-10.png"
[link13]: img/02-11.png "Figure 02-11.png"
[link14]: img/02-12.png "Figure 02-12.png"
[link15]: img/02-13.png "Figure 02-13.png"
[link16]: img/02-14.png "Figure 02-14.png"
